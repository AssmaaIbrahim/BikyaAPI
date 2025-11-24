using Bikya.Data.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bikya.Data
{

    public static class SeedChatBotData
    {
        public static async Task SeedAsync(BikyaContext context)
        {
            if (await context.ChatBotFaqs.AnyAsync())
            {
                return; // عشان ما يعيدش التحقين كل مرة
            }

            var faqs = new List<ChatBotFaq>
{
    new() { Keyword = "register", Answer = "You can register your account here: /register" },
    new() { Keyword = "login", Answer = "Login using your email and password here: /login" },
    new() { Keyword = "forgot password", Answer = "Click 'Forgot Password' on the login page to reset your password." },
    new() { Keyword = "payment", Answer = "We accept Visa, MasterCard, and Cash on Delivery." },
    new() { Keyword = "track", Answer = "You can track your order here: /track-order" },
    new() { Keyword = "cancel order", Answer = "To cancel an order, go to 'My Orders' and click the cancel button." },
    new() { Keyword = "return", Answer = "We offer returns within 14 days. Visit /return-policy for more info." },
    new() { Keyword = "contact", Answer = "You can contact our support at support@babyshop.com" },
    new() { Keyword = "delivery", Answer = "Orders are delivered within 2-4 working days." },
    new() { Keyword = "shipping", Answer = "Free shipping on orders above 500 EGP." },
    new() { Keyword = "baby toys", Answer = "You can find baby toys here: /categories/toys" },
    new() { Keyword = "diapers", Answer = "We offer top brands of diapers here: /categories/diapers" },
    new() { Keyword = "stroller", Answer = "Check out our baby strollers here: /categories/strollers" },
    new() { Keyword = "baby clothes", Answer = "Browse our baby clothing here: /categories/clothes" },
    new() { Keyword = "sizes", Answer = "Size charts are available on each product page." },
    new() { Keyword = "how to order", Answer = "Add products to your cart and click 'Checkout'." },
    new() { Keyword = "offers", Answer = "See our latest offers and discounts here: /offers" },
    new() { Keyword = "warranty", Answer = "Some items include a warranty. Check the product description for details." }
};

            await context.ChatBotFaqs.AddRangeAsync(faqs);
            await context.SaveChangesAsync();
        }
    }
}
