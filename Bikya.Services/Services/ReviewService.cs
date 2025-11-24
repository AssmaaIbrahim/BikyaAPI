using Bikya.Data.Models;
using Bikya.Data.Repositories.Interfaces;
using Bikya.Data.Response;
using Bikya.DTOs.ReviewDTOs;
using Bikya.Services.Interfaces;

namespace Bikya.Services.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IReviewRepository _reviewRepository;
        private readonly IUserRepository _userRepository;
        private readonly IOrderRepository _orderRepository;

        public ReviewService(
            IReviewRepository reviewRepository,
            IUserRepository userRepository,
            IOrderRepository orderRepository)
        {
            _reviewRepository = reviewRepository;
            _userRepository = userRepository;
            _orderRepository = orderRepository;
        }

        public async Task<ApiResponse<List<ReviewDTO>>> GetAllAsync()
        {
            var reviews = await _reviewRepository.GetAllReviewsWithRelationsAsync();
            var result = reviews.Select(ToReviewDTO).ToList();
            return ApiResponse<List<ReviewDTO>>.SuccessResponse(result, "Reviews retrieved successfully");
        }

        public async Task<ApiResponse<ReviewDTO>> GetByIdAsync(int id)
        {
            var review = await _reviewRepository.GetReviewWithAllRelationsAsync(id);

            if (review == null)
                return ApiResponse<ReviewDTO>.ErrorResponse("Review not found", 404);

            return ApiResponse<ReviewDTO>.SuccessResponse(ToReviewDTO(review), "Review retrieved successfully");
        }

        public async Task<ApiResponse<ReviewDTO>> AddAsync(CreateReviewDTO dto)
        {
            var reviewer = await _userRepository.FindByIdAsync(dto.ReviewerId);
            if (reviewer == null)
                return ApiResponse<ReviewDTO>.ErrorResponse("Reviewer not found", 404);

            var seller = await _userRepository.FindByIdAsync(dto.SellerId);
            if (seller == null)
                return ApiResponse<ReviewDTO>.ErrorResponse("Seller not found", 404);

            var order = await _orderRepository.GetOrderWithAllRelationsAsync(dto.OrderId);
            if (order == null)
                return ApiResponse<ReviewDTO>.ErrorResponse("Order not found", 404);

            // Validate authorization
            var canReview = await _reviewRepository.CanUserReviewOrderAsync(dto.OrderId, dto.ReviewerId);
            if (!canReview)
                return ApiResponse<ReviewDTO>.ErrorResponse("You are not authorized to review this order", 403);

            // Check for existing review
            var hasExistingReview = await _reviewRepository.HasReviewForOrderAsync(dto.OrderId);
            if (hasExistingReview)
                return ApiResponse<ReviewDTO>.ErrorResponse("Review already exists for this order", 400);

            var review = new Review
            {
                Rating = dto.Rating,
                Comment = dto.Comment,
                ReviewerId = dto.ReviewerId,
                SellerId = dto.SellerId,
                OrderId = dto.OrderId
            };

            await _reviewRepository.AddAsync(review);
            await _reviewRepository.SaveChangesAsync();

            // Get the created review with relations for DTO mapping
            var createdReview = await _reviewRepository.GetReviewWithAllRelationsAsync(review.Id);
            return ApiResponse<ReviewDTO>.SuccessResponse(ToReviewDTO(createdReview!), "Review created successfully", 201);
        }

        public async Task<ApiResponse<ReviewDTO>> UpdateAsync(int id, UpdateReviewDTO dto)
        {
            var review = await _reviewRepository.GetReviewWithAllRelationsAsync(id);
            if (review == null)
                return ApiResponse<ReviewDTO>.ErrorResponse("Review not found", 404);

            // Validate ownership
            var isOwner = await _reviewRepository.IsReviewOwnerAsync(id, dto.ReviewerId);
            if (!isOwner)
                return ApiResponse<ReviewDTO>.ErrorResponse("You are not authorized to update this review", 403);

            review.Rating = dto.Rating;
            review.Comment = dto.Comment;

            _reviewRepository.Update(review);
            await _reviewRepository.SaveChangesAsync();

            // Get updated review with relations
            var updatedReview = await _reviewRepository.GetReviewWithAllRelationsAsync(id);
            return ApiResponse<ReviewDTO>.SuccessResponse(ToReviewDTO(updatedReview!), "Review updated successfully");
        }

        public async Task<ApiResponse<bool>> DeleteAsync(int id)
        {
            var review = await _reviewRepository.GetByIdAsync(id);
            if (review == null)
                return ApiResponse<bool>.ErrorResponse("Review not found", 404);

            _reviewRepository.Remove(review);
            await _reviewRepository.SaveChangesAsync();

            return ApiResponse<bool>.SuccessResponse(true, "Review deleted successfully");
        }

        public async Task<ApiResponse<List<ReviewDTO>>> GetReviewsBySellerIdAsync(int sellerId)
        {
            var reviews = await _reviewRepository.GetReviewsBySellerIdAsync(sellerId);
            var result = reviews.Select(ToReviewDTO).ToList();
            return ApiResponse<List<ReviewDTO>>.SuccessResponse(result, "Reviews for seller retrieved successfully");
        }

        public async Task<ApiResponse<List<ReviewDTO>>> GetReviewsByReviewerIdAsync(int reviewerId)
        {
            var reviews = await _reviewRepository.GetReviewsByReviewerIdAsync(reviewerId);
            var result = reviews.Select(ToReviewDTO).ToList();
            return ApiResponse<List<ReviewDTO>>.SuccessResponse(result, "Reviews by reviewer retrieved successfully");
        }

        public async Task<ApiResponse<List<ReviewDTO>>> GetReviewsByOrderIdAsync(int orderId)
        {
            var reviews = await _reviewRepository.GetReviewsByOrderIdAsync(orderId);

            if (!reviews.Any())
                return ApiResponse<List<ReviewDTO>>.ErrorResponse("No reviews found for this order", 404);

            var result = reviews.Select(ToReviewDTO).ToList();
            return ApiResponse<List<ReviewDTO>>.SuccessResponse(result, "Reviews for order retrieved successfully");
        }

        private ReviewDTO ToReviewDTO(Review review)
        {
            return new ReviewDTO
            {
                Id = review.Id,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                ReviewerId = review.ReviewerId,
                SellerId = review.SellerId,
                OrderId = review.OrderId,
                BuyerName = review.Reviewer?.FullName ?? "",
                SellerName = review.Seller?.FullName ?? "",
                //ProductName = review.Order?.Product?.Title ?? string.Empty
            };
        }
    }
}