using FluentValidation;

using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Validation
{
    public class PostPaymentRequestValidator : AbstractValidator<PostPaymentRequest>
    {
        private readonly HashSet<string> acceptableCurrency = new HashSet<string>
        {
            "GBP",
            "EUR",
            "USD"
        };

        public PostPaymentRequestValidator(TimeProvider timeProvider)
        {
            RuleFor(x => x.CardNumber).NotEmpty().Length(14, 19).Matches("^[0-9]+$");
            RuleFor(x => x.Currency).NotEmpty().Must(c => acceptableCurrency.Contains(c)).WithMessage($"Currency must be one of: {string.Join(",", acceptableCurrency)}");
            RuleFor(x => x.Amount).GreaterThan(0);
            RuleFor(x => x.Cvv).NotEmpty().Length(3, 4).Matches("^[0-9]+$");
            RuleFor(x => x.ExpiryMonth).InclusiveBetween(1, 12);
            RuleFor(x => x).Must(x => IsInFuture(x.ExpiryMonth, x.ExpiryYear, timeProvider.GetUtcNow())).WithMessage("Expiry Month and Expiry Year must be in the future");
        }

        private bool IsInFuture(int expiryMonth, int expiryYear, DateTimeOffset now)
        {
            var currentMonth = now.Month;
            var currentYear = now.Year;

            if (expiryYear == currentYear)
            {
                return expiryMonth >= currentMonth;
            }else
            {
                return expiryYear > currentYear;
            }
        }
    }
}
