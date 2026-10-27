using System.Security.Cryptography.X509Certificates;

using FluentValidation.TestHelper;

using Microsoft.Extensions.Time.Testing;

using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Validation;

namespace PaymentGateway.Api.Tests.ValidationTests
{
    public class PostPaymentRequestValidatorTests
    {
        private static readonly DateTimeOffset today = new DateTimeOffset(2026, 10, 27, 18, 0, 0, TimeSpan.Zero);
        private readonly PostPaymentRequestValidator validator = new PostPaymentRequestValidator(new FakeTimeProvider(today));

        private static PostPaymentRequest TestRequest() => new()
        {
            CardNumber = "11122233344455",
            ExpiryMonth = 10,
            ExpiryYear = 2026,
            Currency = Currency.GBP,
            Amount = 1050,
            Cvv = "123"
        };

        [Fact]
        public void EmptyCardNumberIsNotValid()
        {
            var request = TestRequest();
            request.CardNumber = String.Empty;

            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.CardNumber);
        }

        [Theory]
        [InlineData("1112223334445")] //13
        [InlineData("11122233344455566677")] //20
        [InlineData("0111222233334")] //13
        [InlineData("011 122 223 334")] //spaces
        [InlineData("test")] 
        public void InvalidCarNumberIsNotValid(string cardNumber)
        {
            var request = TestRequest();
            request.CardNumber = cardNumber;

            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.CardNumber);
        }

        [Theory]
        [InlineData("11122233344455")] //14
        [InlineData("1112223334445556667")] //19
        [InlineData("0112223334445556667")] //leading zero
        public void ValidCardNumberIsValid(string cardNumber)
        {
            var request = TestRequest();
            request.CardNumber = cardNumber;

            var result = validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.CardNumber);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(13)]
        public void ExpiryMonthIsOutOfBoundsIsInvalid(int expiryMonth)
        {
            var request = TestRequest();
            request.ExpiryMonth = expiryMonth;

            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.ExpiryMonth);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(3)]
        [InlineData(12)]
        public void ExpiryMonthIsWithinBoundsIsValid(int expiryMonth)
        {
            var request = TestRequest();
            request.ExpiryMonth = expiryMonth;

            var result = validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.ExpiryMonth);
        }

        [Theory]
        [InlineData(1, 2024)]
        [InlineData(10, 2025)]
        [InlineData(11, 2025)]
        [InlineData(9, 2026)]
        public void ExpiryDateNotInFutureIsInvalid(int expiryMonth, int expiryYear)
        {
            var request = TestRequest();
            request.ExpiryMonth = expiryMonth;
            request.ExpiryYear = expiryYear;

            var result = validator.TestValidate(request);
            result.ShouldHaveAnyValidationError().WithErrorMessage("Expiry Month and Expiry Year must be in the future");
        }

        [Theory]
        [InlineData(10, 2026)]
        [InlineData(11, 2026)]
        [InlineData(09, 2027)]
        [InlineData(1, 2027)]
        public void ExpiryDateInFutureIsValid(int expiryMonth, int expiryYear)
        {
            var request = TestRequest();
            request.ExpiryMonth = expiryMonth;
            request.ExpiryYear = expiryYear;

            var result = validator.TestValidate(request);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void ZeroAmountIsInvalid()
        {
            var request = TestRequest();
            request.Amount = 0;

            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Amount);
        }

        [Theory]
        [InlineData("tes")]
        [InlineData("12")]
        [InlineData("12345")]
        [InlineData("12 3")]
        public void InvalidCvvIsInvalid(string cvv)
        {
            var request = TestRequest();
            request.Cvv = cvv;

            var result = validator.TestValidate(request);
            result.ShouldHaveValidationErrorFor(x => x.Cvv);
        }

        [Theory]
        [InlineData("123")]
        [InlineData("1234")]
        public void ValidCvvIsValid(string cvv)
        {
            var request = TestRequest();
            request.Cvv = cvv;

            var result = validator.TestValidate(request);
            result.ShouldNotHaveValidationErrorFor(x => x.Cvv);
        }

    }
}
