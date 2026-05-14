using System;

namespace OctafxIndia.Models
{
    public class UserTradingAccount
    {
        public int Id { get; set; }
        public string? UserId { get; set; }          // AspNetUsers.Id (nullable)
        public string AccountNumber { get; set; } = null!;
        public string? AccountType { get; set; }     // REAL / DEMO etc
        public string? Server { get; set; }
        public decimal Balance { get; set; }
        public decimal Equity { get; set; }
        public string? Status { get; set; }          // Active / Deactivated
        public DateTime LastUpdated { get; set; }
    }
}
