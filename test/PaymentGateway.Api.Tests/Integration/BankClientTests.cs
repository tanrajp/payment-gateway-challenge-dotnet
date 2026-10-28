using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging.Abstractions;

using PaymentGateway.Api.Clients;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;

namespace PaymentGateway.Api.Tests.Integration
{
    public class BankClientTests
    {
        private readonly BankClient _bankClient;

        public BankClientTests()
        {
            var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8080/") };
            _bankClient = new BankClient(httpClient, NullLogger<BankClient>.Instance);
        }

        [Fact]
        public async Task AuthorizedRequestReturnsSuccessResponse()
        {
            var request = new BankRequest
            {
                CardNumber = "1234567890123",
                Amount = 110,
                Currency = "GBP",
                ExpiryDate = "10/2028",
                Cvv = "123"
            };

            var response = await _bankClient.ProcessPayment(request, CancellationToken.None);

            Assert.True(response.Authorized);
            Assert.NotNull(response.AuthorizationCode);
        }

        [Fact]
        public async Task UnAutoriziedRequestReturnsSuccessResponse()
        {
            var request = new BankRequest
            {
                CardNumber = "1234567890122",
                Amount = 110,
                Currency = "GBP",
                ExpiryDate = "10/2028",
                Cvv = "123"
            };

            var response = await _bankClient.ProcessPayment(request, CancellationToken.None);

            Assert.False(response.Authorized);
            Assert.Equal(string.Empty, response.AuthorizationCode);
        }

        [Fact]
        public async Task ServiceErrorFromBankThrowsException()
        {
            var request = new BankRequest
            {
                CardNumber = "1234567890120",
                Amount = 110,
                Currency = "GBP",
                ExpiryDate = "10/2028",
                Cvv = "123"
            };

            await Assert.ThrowsAsync<BankServiceUnavailableException>(async () => await _bankClient.ProcessPayment(request, CancellationToken.None));
        }

        [Fact]
        public async Task BadRequestErrorFromBankThrowsException()
        {
            var request = new BankRequest
            {
                CardNumber = "1234567890121",
                Amount = 110,
                Currency = "",
                ExpiryDate = "10/2028",
                Cvv = "123"
            };

            await Assert.ThrowsAsync<BanksServiceBadRequestException>(async () => await _bankClient.ProcessPayment(request, CancellationToken.None));
        }
    }
}
