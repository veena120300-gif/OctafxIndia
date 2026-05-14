using System.ComponentModel.DataAnnotations;

namespace OctafxIndia.ViewModels
{
    public class EditTradingAccountViewModel
    {
        [Display(Name = "Account Number")]
        [StringLength(100)]
        public string? AccountNumber { get; set; }

        [Display(Name = "Account Name")]
        [StringLength(200)]
        public string? AccountName { get; set; }

        [Display(Name = "Balance")]
        [DataType(DataType.Currency)]
        public decimal Balance { get; set; }

        [Display(Name = "Free Margin")]
        [DataType(DataType.Currency)]
        public decimal FreeMargin { get; set; }

        [Display(Name = "Equity")]
        [DataType(DataType.Currency)]
        public decimal Equity { get; set; }

        [Display(Name = "Leverage")]
        [StringLength(50)]
        public string? Leverage { get; set; }

        [Display(Name = "Server")]
        [StringLength(200)]
        public string? Server { get; set; }

        [Display(Name = "No Swap")]
        public bool NoSwap { get; set; }
    }
}
