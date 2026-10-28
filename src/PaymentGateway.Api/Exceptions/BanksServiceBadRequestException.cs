using System.Runtime.Serialization;

namespace PaymentGateway.Api.Exceptions
{
    public class BanksServiceBadRequestException : Exception
    {
        public BanksServiceBadRequestException()
        {
        }

        public BanksServiceBadRequestException(string? message) : base(message)
        {
        }

        public BanksServiceBadRequestException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected BanksServiceBadRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
