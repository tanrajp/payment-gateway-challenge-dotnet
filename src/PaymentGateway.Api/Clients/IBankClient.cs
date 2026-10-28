using System.Text.Json;

using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;

namespace PaymentGateway.Api.Clients
{
    public interface IBankClient
    {
        Task<BankResponse> ProcessPayment(BankRequest request, CancellationToken cancellationToken);
    }

    public class BankClient : IBankClient
    {
        private readonly HttpClient httpClient;
        private readonly ILogger<BankClient> logger;

        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        public BankClient(HttpClient httpClient, ILogger<BankClient> logger)
        {
            this.httpClient = httpClient;
            this.logger = logger;
        }

        public async Task<BankResponse> ProcessPayment(BankRequest request, CancellationToken cancellationToken)
        {            
            using var response = await httpClient.PostAsJsonAsync("payments", request, JsonOptions, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                if(response.StatusCode == System.Net.HttpStatusCode.ServiceUnavailable)
                {
                    logger.LogError("Bank is unavailable");
                    throw new BankServiceUnavailableException();
                }

                if(response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var error = await response.Content.ReadAsStringAsync(cancellationToken);
                    string message = $"Bad request to bank with error {error}";
                    logger.LogError(message);
                    throw new BankServiceBadRequestException(error);
                }
            }

            var bankAccountResponse = await response.Content.ReadFromJsonAsync<BankResponse>(JsonOptions, cancellationToken);

            if(bankAccountResponse == null)
            {
                logger.LogError("Empty response returned from bank");
                throw new InvalidOperationException("Empty response returned from bank");
            }

            return bankAccountResponse;
        }
    }
}
