using PaymentGateway.Api.Clients;
using PaymentGateway.Api.Helpers;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Services
{
    public class PaymentService
    {
        private readonly IBankClient _bankClient;
        private readonly PaymentsRepository _paymentsRepository;

        private readonly ILogger _logger;

        public PaymentService(
            IBankClient bankClient, 
            PaymentsRepository paymentsRepository, 
            ILogger<PaymentService> logger
            )
        {
            this._bankClient = bankClient;
            this._paymentsRepository = paymentsRepository;
            this._logger = logger;
        }

        public async Task<PostPaymentResponse> ProcessPayment(PostPaymentRequest request, CancellationToken cancellationToken)
        {
            var bankRequest = request.ToBankRequest();
            var bankResponse = await _bankClient.ProcessPayment(bankRequest, cancellationToken);

            var response =  BuildResponse(request, bankResponse);

            _paymentsRepository.Add(response);

            return response;
        }

        private PostPaymentResponse BuildResponse(PostPaymentRequest request, BankResponse bankResponse)
        {
            return new PostPaymentResponse
            {
                Id = Guid.NewGuid(),
                Status = bankResponse.Authorized ? Models.PaymentStatus.Authorized : Models.PaymentStatus.Declined,
                CardNumberLastFour = request.CardNumber.Substring(request.CardNumber.Length - 4),
                ExpiryMonth = request.ExpiryMonth,
                ExpiryYear = request.ExpiryYear,
                Amount = request.Amount,
                Currency = request.Currency,
                AuthorizationCode = bankResponse.Authorized ? bankResponse.AuthorizationCode : string.Empty
            };
        }
    }
}
