using System.Runtime.Serialization;

namespace PaymentGateway.Api.Exceptions
{
    public class BankServiceBadRequestException : Exception
    {
        public BankServiceBadRequestException()
        {
        }

        public BankServiceBadRequestException(string? message) : base(message)
        {
        }

        public BankServiceBadRequestException(string? message, Exception? innerException) : base(message, innerException)
        {
        }
    }
}
