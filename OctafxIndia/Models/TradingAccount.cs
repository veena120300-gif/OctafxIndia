using System;

namespace OctafxIndia.Models
{
    public class TradingAccount
    {
        public int id { get; set; }
        public string AccountNumber { get; set; }
        public string AccountName { get; set; } // e.g. OctaFX-Real7
        public decimal Balance { get; set; }
        public decimal FreeMargin { get; set; }
        public decimal Equity { get; set; }
        public string Leverage { get; set; }
        public string Server { get; set; }
        public bool NoSwap { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
