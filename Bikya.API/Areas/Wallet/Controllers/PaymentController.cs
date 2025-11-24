using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.DTOs.PaymentDTOs;
using Bikya.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stripe;
using Stripe.Checkout;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic; // Added for List

namespace Bikya.API.Areas.Wallet.Controllers
{
    [ApiController]
    [Route("api/[area]/[controller]")]
    [Area("Wallet")]
    [Authorize]

    public class PaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;
        private readonly IStripeService _stripeService;
        private readonly IConfiguration _configuration;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IOrderRepository _orderRepository;

        public PaymentController(
            IPaymentService paymentService,
            IStripeService stripeService,
            IConfiguration configuration,
            IPaymentRepository paymentRepository,
            ITransactionRepository transactionRepository,
            IOrderRepository orderRepository)
        {
            _paymentService = paymentService;
            _stripeService = stripeService;
            _configuration = configuration;
            _paymentRepository = paymentRepository;
            _transactionRepository = transactionRepository;
            _orderRepository = orderRepository;
        }

        /// <summary>
        /// Get all payments for a specific user
        /// </summary>
        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            if (userId <= 0)
                return BadRequest(new { message = "Invalid user ID" });

            var payments = await _paymentService.GetPaymentsByUserIdAsync(userId);
            return Ok(payments);
        }

        /// <summary>
        /// Get all payments for a specific order
        /// </summary>
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetByOrderId(int orderId)
        {
            if (orderId <= 0)
                return BadRequest(new { message = "Invalid order ID" });

            var payments = await _paymentService.GetPaymentsByOrderIdAsync(orderId);
            return Ok(payments);
        }

        /// <summary>
        /// Get payment summary by payment ID
        /// </summary>
        [HttpGet("summary/{paymentId}")]
        public async Task<IActionResult> GetPaymentSummary(int paymentId)
        {
            if (paymentId <= 0)
                return BadRequest(new { message = "Invalid payment ID" });

            var response = await _paymentService.GetPaymentSummaryAsync(paymentId);
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Stripe webhook endpoint for payment processing
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous] // Stripe webhooks don't use authentication
        public async Task<IActionResult> StripeWebhook()
        {
            try
            {
                var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
                Console.WriteLine($"[WEBHOOK] Received webhook: {json}");

                var stripeSignature = Request.Headers["Stripe-Signature"].FirstOrDefault();
                if (string.IsNullOrEmpty(stripeSignature))
                {
                    Console.WriteLine("[WEBHOOK] Error: Missing Stripe-Signature header");
                    return BadRequest(new { error = "Missing Stripe-Signature header" });
                }

                Console.WriteLine($"[WEBHOOK] Stripe-Signature: {stripeSignature}");

                var webhookSecret = _configuration["Stripe:WebhookSecret"];
                if (string.IsNullOrEmpty(webhookSecret))
                {
                    Console.WriteLine("[WEBHOOK] Error: Webhook secret not configured");
                    return StatusCode(500, new { error = "Webhook secret not configured." });
                }

                Console.WriteLine($"[WEBHOOK] Webhook secret configured: {!string.IsNullOrEmpty(webhookSecret)}");

                Event stripeEvent;

                try
                {
                    stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
                    Console.WriteLine($"[WEBHOOK] Event constructed successfully. Type: {stripeEvent.Type}");
                }
                catch (StripeException ex)
                {
                    Console.WriteLine($"[WEBHOOK] Stripe signature validation failed: {ex.Message}");
                    return BadRequest(new { error = "Stripe signature validation failed: " + ex.Message });
                }

                Console.WriteLine($"[WEBHOOK] Processing event type: {stripeEvent.Type}");

                if (stripeEvent.Type == "checkout.session.completed")
                {
                    Console.WriteLine("[WEBHOOK] Processing checkout.session.completed event");
                    
                    var session = stripeEvent.Data.Object as Session;

                    if (session == null || string.IsNullOrEmpty(session.Id))
                    {
                        Console.WriteLine("[WEBHOOK] Error: Invalid session object");
                        return BadRequest(new { error = "Invalid session object" });
                    }

                    Console.WriteLine($"[WEBHOOK] Session ID: {session.Id}");
                    Console.WriteLine($"[WEBHOOK] Payment Status: {session.PaymentStatus}");
                    Console.WriteLine($"[WEBHOOK] Session Status: {session.Status}");

                    var payment = await _paymentRepository.GetByStripeSessionIdAsync(session.Id);
                    if (payment == null)
                    {
                        Console.WriteLine($"[WEBHOOK] Error: Payment not found for session ID: {session.Id}");
                        return NotFound(new { error = $"Payment not found for session ID: {session.Id}" });
                    }

                    Console.WriteLine($"[WEBHOOK] Found payment ID: {payment.Id}, Current status: {payment.Status}");

                    // Update payment status
                    payment.Status = PaymentStatus.Paid;
                    await _paymentRepository.SaveChangesAsync();
                    Console.WriteLine($"[WEBHOOK] Updated payment {payment.Id} status to Paid");

                    // Update order status if payment is successful
                    if (payment.OrderId.HasValue)
                    {
                        var order = await _orderRepository.GetByIdAsync(payment.OrderId.Value);
                        if (order != null)
                        {
                            order.Status = OrderStatus.Paid;
                            order.PaidAt = DateTime.UtcNow;
                            await _orderRepository.SaveChangesAsync();
                            Console.WriteLine($"[WEBHOOK] Updated order {order.Id} status to Paid");
                        }

                        // Create transaction record
                        var transaction = new Transaction
                        {
                            Amount = payment.Amount,
                            Type = TransactionType.Payment,
                            Status = TransactionStatus.Completed,
                            PaymentId = payment.Id,
                            RelatedOrderId = payment.OrderId,
                            Description = $"Stripe Payment for Order #{payment.OrderId}"
                        };

                        await _transactionRepository.AddAsync(transaction);
                        Console.WriteLine($"[WEBHOOK] Created transaction record for payment {payment.Id}");
                    }

                    Console.WriteLine("[WEBHOOK] Payment processed successfully");
                    return Ok(new { message = "Payment processed successfully" });
                }
                else if (stripeEvent.Type == "checkout.session.expired")
                {
                    Console.WriteLine("[WEBHOOK] Processing checkout.session.expired event");
                    
                    var session = stripeEvent.Data.Object as Session;
                    if (session != null && !string.IsNullOrEmpty(session.Id))
                    {
                        var payment = await _paymentRepository.GetByStripeSessionIdAsync(session.Id);
                        if (payment != null)
                        {
                            payment.Status = PaymentStatus.Failed;
                            await _paymentRepository.SaveChangesAsync();
                            Console.WriteLine($"[WEBHOOK] Updated payment {payment.Id} status to Failed (expired)");
                        }
                    }
                    return Ok(new { message = "Payment session expired" });
                }
                else
                {
                    Console.WriteLine($"[WEBHOOK] Unhandled event type: {stripeEvent.Type}");
                    return BadRequest(new { error = $"Unhandled event type: {stripeEvent.Type}" });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WEBHOOK] Unexpected error: {ex.Message}");
                Console.WriteLine($"[WEBHOOK] Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { error = $"Internal server error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Get payment by ID with detailed information
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            if (id <= 0)
                return BadRequest(new { message = "Invalid payment ID" });

            var response = await _paymentService.GetPaymentStatusAsync(id);
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Create a new payment with comprehensive validation
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] PaymentRequestDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _paymentService.CreatePaymentAsync(dto);
            return StatusCode(response.StatusCode, response);
        }

        /// <summary>
        /// Manually check and update payment status from Stripe
        /// This endpoint can be used to debug webhook issues
        /// </summary>
        [HttpPost("check-status/{paymentId}")]
        public async Task<IActionResult> CheckPaymentStatus(int paymentId)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return NotFound(new { message = "Payment not found" });
                }

                if (string.IsNullOrEmpty(payment.StripeSessionId))
                {
                    return BadRequest(new { message = "Payment has no Stripe session ID" });
                }

                // Check session status from Stripe
                var sessionService = new Stripe.Checkout.SessionService();
                var session = await sessionService.GetAsync(payment.StripeSessionId);

                if (session.PaymentStatus == "paid")
                {
                    // Update payment status
                    payment.Status = PaymentStatus.Paid;
                    await _paymentRepository.SaveChangesAsync();

                    // Update order status if payment is successful
                    if (payment.OrderId.HasValue)
                    {
                        var order = await _orderRepository.GetByIdAsync(payment.OrderId.Value);
                        if (order != null)
                        {
                            order.Status = OrderStatus.Paid;
                            order.PaidAt = DateTime.UtcNow;
                            await _orderRepository.SaveChangesAsync();
                        }

                        // Create transaction record
                        var transaction = new Transaction
                        {
                            Amount = payment.Amount,
                            Type = TransactionType.Payment,
                            Status = TransactionStatus.Completed,
                            PaymentId = payment.Id,
                            RelatedOrderId = payment.OrderId,
                            Description = $"Stripe Payment for Order #{payment.OrderId}"
                        };

                        await _transactionRepository.AddAsync(transaction);
                    }

                    return Ok(new { 
                        message = "Payment status updated successfully", 
                        paymentStatus = "Paid",
                        stripeSessionStatus = session.PaymentStatus
                    });
                }
                else if (session.Status == "expired")
                {
                    payment.Status = PaymentStatus.Failed;
                    await _paymentRepository.SaveChangesAsync();

                    return Ok(new { 
                        message = "Payment session expired", 
                        paymentStatus = "Failed",
                        stripeSessionStatus = session.Status
                    });
                }
                else
                {
                    return Ok(new { 
                        message = "Payment is still pending", 
                        paymentStatus = payment.Status.ToString(),
                        stripeSessionStatus = session.PaymentStatus,
                        stripeSessionStatus2 = session.Status
                    });
                }
            }
            catch (Stripe.StripeException ex)
            {
                return BadRequest(new { error = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Check all pending payments for a user and their Stripe status
        /// </summary>
        [HttpGet("check-pending/{userId}")]
        public async Task<IActionResult> CheckPendingPayments(int userId)
        {
            try
            {
                var payments = await _paymentService.GetPaymentsByUserIdAsync(userId);
                var pendingPayments = payments.Where(p => p.Status == "Pending").ToList();
                
                var results = new List<object>();
                var sessionService = new Stripe.Checkout.SessionService();

                foreach (var payment in pendingPayments)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(payment.StripeUrl))
                        {
                            // Extract session ID from Stripe URL
                            var sessionId = payment.StripeUrl.Split('/').Last();
                            var session = await sessionService.GetAsync(sessionId);

                            results.Add(new
                            {
                                paymentId = payment.Id,
                                amount = payment.Amount,
                                orderId = payment.OrderId,
                                stripeSessionId = sessionId,
                                stripePaymentStatus = session.PaymentStatus,
                                stripeSessionStatus = session.Status,
                                createdAt = payment.CreatedAt
                            });
                        }
                    }
                    catch (Stripe.StripeException ex)
                    {
                        results.Add(new
                        {
                            paymentId = payment.Id,
                            amount = payment.Amount,
                            orderId = payment.OrderId,
                            error = $"Stripe error: {ex.Message}"
                        });
                    }
                }

                return Ok(new
                {
                    totalPayments = payments.Count(),
                    pendingPayments = pendingPayments.Count,
                    results = results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Refresh all pending payments for a user and update their status
        /// </summary>
        [HttpPost("refresh-pending/{userId}")]
        public async Task<IActionResult> RefreshPendingPayments(int userId)
        {
            try
            {
                var payments = await _paymentService.GetPaymentsByUserIdAsync(userId);
                var pendingPayments = payments.Where(p => p.Status == "Pending").ToList();
                
                var results = new List<object>();
                var updatedCount = 0;
                var sessionService = new Stripe.Checkout.SessionService();

                foreach (var payment in pendingPayments)
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(payment.StripeUrl))
                        {
                            // Extract session ID from Stripe URL
                            var sessionId = payment.StripeUrl.Split('/').Last();
                            var session = await sessionService.GetAsync(sessionId);

                            var dbPayment = await _paymentRepository.GetByIdAsync(payment.Id);
                            if (dbPayment != null)
                            {
                                if (session.PaymentStatus == "paid")
                                {
                                    // Update payment status
                                    dbPayment.Status = PaymentStatus.Paid;
                                    await _paymentRepository.SaveChangesAsync();

                                    // Update order status if payment is successful
                                    if (dbPayment.OrderId.HasValue)
                                    {
                                        var order = await _orderRepository.GetByIdAsync(dbPayment.OrderId.Value);
                                        if (order != null)
                                        {
                                            order.Status = OrderStatus.Paid;
                                            order.PaidAt = DateTime.UtcNow;
                                            await _orderRepository.SaveChangesAsync();
                                        }

                                        // Create transaction record
                                        var transaction = new Transaction
                                        {
                                            Amount = dbPayment.Amount,
                                            Type = TransactionType.Payment,
                                            Status = TransactionStatus.Completed,
                                            PaymentId = dbPayment.Id,
                                            RelatedOrderId = dbPayment.OrderId,
                                            Description = $"Stripe Payment for Order #{dbPayment.OrderId}"
                                        };

                                        await _transactionRepository.AddAsync(transaction);
                                    }

                                    updatedCount++;
                                    results.Add(new
                                    {
                                        paymentId = payment.Id,
                                        action = "Updated to Paid",
                                        stripePaymentStatus = session.PaymentStatus
                                    });
                                }
                                else if (session.Status == "expired")
                                {
                                    dbPayment.Status = PaymentStatus.Failed;
                                    await _paymentRepository.SaveChangesAsync();

                                    results.Add(new
                                    {
                                        paymentId = payment.Id,
                                        action = "Updated to Failed",
                                        stripeSessionStatus = session.Status
                                    });
                                }
                                else
                                {
                                    results.Add(new
                                    {
                                        paymentId = payment.Id,
                                        action = "Still Pending",
                                        stripePaymentStatus = session.PaymentStatus,
                                        stripeSessionStatus = session.Status
                                    });
                                }
                            }
                        }
                    }
                    catch (Stripe.StripeException ex)
                    {
                        results.Add(new
                        {
                            paymentId = payment.Id,
                            action = "Error",
                            error = $"Stripe error: {ex.Message}"
                        });
                    }
                }

                return Ok(new
                {
                    message = $"Processed {pendingPayments.Count} pending payments",
                    totalPending = pendingPayments.Count,
                    updatedCount = updatedCount,
                    results = results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }

        /// <summary>
        /// Test endpoint to simulate webhook events (for debugging only)
        /// </summary>
        [HttpPost("test-webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> TestWebhook([FromBody] TestWebhookRequest request)
        {
            try
            {
                Console.WriteLine($"[TEST-WEBHOOK] Testing webhook for session ID: {request.SessionId}");
                
                var sessionService = new Stripe.Checkout.SessionService();
                var session = await sessionService.GetAsync(request.SessionId);
                
                Console.WriteLine($"[TEST-WEBHOOK] Session found - Payment Status: {session.PaymentStatus}, Session Status: {session.Status}");

                var payment = await _paymentRepository.GetByStripeSessionIdAsync(request.SessionId);
                if (payment == null)
                {
                    Console.WriteLine($"[TEST-WEBHOOK] Payment not found for session ID: {request.SessionId}");
                    return NotFound(new { error = $"Payment not found for session ID: {request.SessionId}" });
                }

                Console.WriteLine($"[TEST-WEBHOOK] Found payment ID: {payment.Id}, Current status: {payment.Status}");

                if (session.PaymentStatus == "paid")
                {
                    // Update payment status
                    payment.Status = PaymentStatus.Paid;
                    await _paymentRepository.SaveChangesAsync();
                    Console.WriteLine($"[TEST-WEBHOOK] Updated payment {payment.Id} status to Paid");

                    // Update order status if payment is successful
                    if (payment.OrderId.HasValue)
                    {
                        var order = await _orderRepository.GetByIdAsync(payment.OrderId.Value);
                        if (order != null)
                        {
                            order.Status = OrderStatus.Paid;
                            order.PaidAt = DateTime.UtcNow;
                            await _orderRepository.SaveChangesAsync();
                            Console.WriteLine($"[TEST-WEBHOOK] Updated order {order.Id} status to Paid");
                        }

                        // Create transaction record
                        var transaction = new Transaction
                        {
                            Amount = payment.Amount,
                            Type = TransactionType.Payment,
                            Status = TransactionStatus.Completed,
                            PaymentId = payment.Id,
                            RelatedOrderId = payment.OrderId,
                            Description = $"Stripe Payment for Order #{payment.OrderId}"
                        };

                        await _transactionRepository.AddAsync(transaction);
                        Console.WriteLine($"[TEST-WEBHOOK] Created transaction record for payment {payment.Id}");
                    }

                    return Ok(new { 
                        message = "Payment processed successfully via test webhook",
                        paymentId = payment.Id,
                        newStatus = "Paid",
                        stripePaymentStatus = session.PaymentStatus
                    });
                }
                else
                {
                    return Ok(new { 
                        message = "Payment is not completed in Stripe",
                        paymentId = payment.Id,
                        currentStatus = payment.Status.ToString(),
                        stripePaymentStatus = session.PaymentStatus,
                        stripeSessionStatus = session.Status
                    });
                }
            }
            catch (Stripe.StripeException ex)
            {
                Console.WriteLine($"[TEST-WEBHOOK] Stripe error: {ex.Message}");
                return BadRequest(new { error = $"Stripe error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[TEST-WEBHOOK] Unexpected error: {ex.Message}");
                return StatusCode(500, new { error = $"Internal error: {ex.Message}" });
            }
        }
    }

    // Test webhook request model
    public class TestWebhookRequest
    {
        public string SessionId { get; set; } = string.Empty;
    }
}
