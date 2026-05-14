using System.Collections.Generic;

namespace OctafxIndia.ViewModels
{
    public class TradingAccountItemViewModel
    {
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string? Server { get; set; }
        public decimal Balance { get; set; }
        public decimal Equity { get; set; }
        public string? Status { get; set; }
    }

    public class BalanceViewModel
    {
        public decimal Balance { get; set; }
        public decimal FreeMargin { get; set; }
        public decimal Equity { get; set; }
        public string Leverage { get; set; } = "1:1000";
        public string Server { get; set; } = "OctaFX-Real7";
        public string AccountNumber { get; set; } = "N/A";
        public string AccountType { get; set; } = "REAL";
        public bool NoSwap { get; set; } = true;

        // NEW: list of user's trading accounts to display in the table
        public List<TradingAccountItemViewModel> Accounts { get; set; } = new List<TradingAccountItemViewModel>();

        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string FullName { get; set; }
        public DateTime? BirthDate { get; set; }
        public string Country { get; set; }
        public string VerificationStatus { get; set; }
        public string Nickname { get; set; }
    }
}
