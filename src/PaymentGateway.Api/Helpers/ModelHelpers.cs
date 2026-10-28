using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Helpers
{
    public static class ModelHelpers
    {
        public static BankRequest ToBankRequest(this PostPaymentRequest request)
        {
            return new BankRequest
            {
                CardNumber = request.CardNumber,
                Amount = request.Amount,
                Currency = request.Currency.ToString(),
                ExpiryDate = $"{request.ExpiryMonth}/{request.ExpiryYear}",
                Cvv = request.Cvv
            };
        }

    }
}
