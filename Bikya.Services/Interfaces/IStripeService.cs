using Bikya.DTOs.StripeDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Services.Interfaces
{
    public interface IStripeService
    {
        Task<StripeSessionInfo> CreateCheckoutSessionAsync(decimal amount, int orderId );
    }

}
