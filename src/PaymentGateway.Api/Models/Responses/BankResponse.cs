namespace PaymentGateway.Api.Models.Responses
{
    public class BankResponse
    {
        public bool Authorized { get; set; }
        public string AuthorizationCode { get; set; } = string.Empty;
    }
}
