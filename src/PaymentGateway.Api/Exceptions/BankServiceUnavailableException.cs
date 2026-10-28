using System.Runtime.Serialization;

namespace PaymentGateway.Api.Exceptions
{
    public class BankServiceUnavailableException : Exception
    {
        public BankServiceUnavailableException()
        {
        }

        public BankServiceUnavailableException(string? message) : base(message)
        {
        }

        public BankServiceUnavailableException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected BankServiceUnavailableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
