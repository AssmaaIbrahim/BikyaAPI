using System.Collections.Generic;

namespace Bikya.DTOs.PaymentDTOs
{
    public class PaymentErrorDto
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public List<string> ValidationErrors { get; set; } = new List<string>();
        public string? SuggestedAction { get; set; }
    }
} 