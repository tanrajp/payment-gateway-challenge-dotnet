using PaymentGateway.Api.Helpers;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Tests.Unit.HelpersTests
{
    public class ModelHelpersTest
    {
        [Fact]
        public void CanConvertPaymentRequestCorrectly()
        {
            var paymentRequest = new PostPaymentRequest
            {
                CardNumber = "1234567890123",
                Amount = 100,
                Currency = "EUR",
                ExpiryMonth = 10,
                ExpiryYear = 2028,
                Cvv = "123"
            };


            var result = paymentRequest.ToBankRequest();

            Assert.Equal("1234567890123", result.CardNumber);
            Assert.Equal(100, result.Amount);
            Assert.Equal("EUR", result.Currency);
            Assert.Equal("10/2028", result.ExpiryDate);
            Assert.Equal("123", result.Cvv);
        }
    }
}
