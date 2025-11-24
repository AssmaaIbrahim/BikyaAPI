using Bikya.Data.Enums;
using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.Data.Response;
using Bikya.DTOs.PaymentDTOs;
using Bikya.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Bikya.Services.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentRepository _paymentRepository;
        private readonly ITransactionRepository _transactionRepository;
        private readonly IStripeService _stripeService;
        private readonly IOrderRepository _orderRepository;

        public PaymentService(IPaymentRepository paymentRepository,
                              ITransactionRepository transactionRepository,
                              IStripeService stripeService,
                              IOrderRepository orderRepository)
        {
            _paymentRepository = paymentRepository;
            _transactionRepository = transactionRepository;
            _stripeService = stripeService;
            _orderRepository = orderRepository;
        }

        public async Task<ApiResponse<PaymentResponseDto>> CreatePaymentAsync(PaymentRequestDto dto)
        {
            try
            {
                // Step 1: Validate order exists and belongs to user
                var order = await _orderRepository.GetByIdAsync(dto.OrderId);
                if (order == null)
                {
                    return ApiResponse<PaymentResponseDto>.ErrorResponse(
                        "Order not found", 404, new List<string> { "The specified order does not exist" });
                }

                if (order.BuyerId != dto.UserId)
                {
                    return ApiResponse<PaymentResponseDto>.ErrorResponse(
                        "Unauthorized", 403, new List<string> { "You can only pay for your own orders" });
                }

                // Step 2: Check if order is already paid
                if (order.Status == OrderStatus.Paid)
                {
                    return ApiResponse<PaymentResponseDto>.ErrorResponse(
                        "Order already paid", 400, new List<string> { "This order has already been paid for" });
                }

                // Step 3: Validate amount matches order total
                if (order.TotalAmount != dto.Amount)
                {
                    return ApiResponse<PaymentResponseDto>.ErrorResponse(
                        "Amount mismatch", 400, new List<string> { $"Order total amount is {order.TotalAmount}, but payment amount is {dto.Amount}" });
                }

                // Step 4: Create Stripe session
                var session = await _stripeService.CreateCheckoutSessionAsync(dto.Amount, dto.OrderId);

                // Step 5: Create payment record in database
                var payment = new Payment
                {
                    Amount = dto.Amount,
                    OrderId = dto.OrderId,
                    UserId = dto.UserId,
                    Status = PaymentStatus.Pending,
                    Gateway = PaymentGateway.Stripe,
                    StripeSessionId = session.Id,
                    Description = dto.Description ?? $"Payment for Order #{dto.OrderId}"
                };

                var createdPayment = await _paymentRepository.AddAsync(payment);

                // Step 6: Create response
                var response = new PaymentResponseDto
                {
                    PaymentId = createdPayment.Id,
                    Amount = createdPayment.Amount,
                    OrderId = createdPayment.OrderId.Value,
                    Status = createdPayment.Status.ToString(),
                    StripeUrl = session.Url,
                    StripeSessionId = session.Id,
                    CreatedAt = createdPayment.CreatedAt,
                    Message = "Payment session created successfully. Redirect user to StripeUrl to complete payment."
                };

                return ApiResponse<PaymentResponseDto>.SuccessResponse(response, "Payment session created successfully");
            }
            catch (Exception ex)
            {
                return ApiResponse<PaymentResponseDto>.ErrorResponse(
                    "Payment creation failed", 500, new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<PaymentStatusDto>> GetPaymentStatusAsync(int paymentId)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return ApiResponse<PaymentStatusDto>.ErrorResponse(
                        "Payment not found", 404, new List<string> { "The specified payment does not exist" });
                }

                var response = new PaymentStatusDto
                {
                    PaymentId = payment.Id,
                    Amount = payment.Amount,
                    OrderId = payment.OrderId ?? 0,
                    Status = payment.Status.ToString(),
                    StripeSessionId = payment.StripeSessionId ?? string.Empty,
                    CreatedAt = payment.CreatedAt,
                    Message = GetStatusMessage(payment.Status)
                };

                return ApiResponse<PaymentStatusDto>.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<PaymentStatusDto>.ErrorResponse(
                    "Failed to get payment status", 500, new List<string> { ex.Message });
            }
        }

        public async Task<ApiResponse<PaymentSummaryDto>> GetPaymentSummaryAsync(int paymentId)
        {
            try
            {
                var payment = await _paymentRepository.GetByIdAsync(paymentId);
                if (payment == null)
                {
                    return ApiResponse<PaymentSummaryDto>.ErrorResponse(
                        "Payment not found", 404, new List<string> { "The specified payment does not exist" });
                }

                var response = new PaymentSummaryDto
                {
                    PaymentId = payment.Id,
                    Amount = payment.Amount,
                    Status = payment.Status.ToString(),
                    CreatedAt = payment.CreatedAt,
                    Description = payment.Description ?? string.Empty
                };

                return ApiResponse<PaymentSummaryDto>.SuccessResponse(response);
            }
            catch (Exception ex)
            {
                return ApiResponse<PaymentSummaryDto>.ErrorResponse(
                    "Failed to get payment summary", 500, new List<string> { ex.Message });
            }
        }

        private string GetStatusMessage(PaymentStatus status)
        {
            return status switch
            {
                PaymentStatus.Pending => "Payment is pending. Please complete the payment on Stripe.",
                PaymentStatus.Paid => "Payment completed successfully.",
                PaymentStatus.Failed => "Payment failed. Please try again.",
                _ => "Unknown payment status."
            };
        }

        public async Task<IEnumerable<PaymentDto>> GetPaymentsByOrderIdAsync(int orderId)
        {
            var payments = await _paymentRepository.GetPaymentsByOrderIdAsync(orderId);
            return payments.Select(ToDto);
        }

        public async Task<IEnumerable<PaymentDto>> GetPaymentsByUserIdAsync(int userId)
        {
            var payments = await _paymentRepository.GetByUserIdAsync(userId);
            return payments.Select(ToDto);
        }

        private PaymentDto ToDto(Payment p)
        {
            return new PaymentDto
            {
                Id = p.Id,
                Amount = p.Amount,
                OrderId = p.OrderId,
                Status = p.Status.ToString(),
                StripeUrl = p.StripeSessionId,
                CreatedAt = p.CreatedAt
            };
        }
    }
}
