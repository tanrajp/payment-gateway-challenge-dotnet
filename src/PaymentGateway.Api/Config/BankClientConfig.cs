using System.ComponentModel.DataAnnotations;

namespace PaymentGateway.Api.Config
{
    public class BankClientConfig
    {
        public const string SectionName = "BankClient";
        
        [Required, Url]
        public string BaseUrl { get; set; }
        public int TimeoutSeconds { get; set; } = 10;

    }
}
