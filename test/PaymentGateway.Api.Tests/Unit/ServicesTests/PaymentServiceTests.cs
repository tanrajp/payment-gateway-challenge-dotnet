using Microsoft.Extensions.Logging.Abstractions;

using Moq;

using PaymentGateway.Api.Clients;
using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.Unit.ServicesTests
{
    public class PaymentServiceTests
    {
        private Mock<IBankClient> _mockBankClient = new();
        private readonly PaymentsRepository _paymentsRepository = new();
        private readonly PaymentService paymentService;

        public PaymentServiceTests()
        {
            paymentService = new PaymentService(_mockBankClient.Object, _paymentsRepository, NullLogger<PaymentService>.Instance);
        }

        [Fact]
        public async Task RequestReturnsAuthorizedResponse()
        {
            var request = DefaultRequest();

            var expectedAuthCode = Guid.NewGuid().ToString();

            _mockBankClient
                .Setup(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BankResponse { Authorized = true, AuthorizationCode = expectedAuthCode });

            var response = await paymentService.ProcessPayment(request, CancellationToken.None);

            Assert.Equal(PaymentStatus.Authorized, response.Status);
            Assert.Equal("8877", response.CardNumberLastFour);
            Assert.Equal(4, response.ExpiryMonth);
            Assert.Equal(2030, response.ExpiryYear);
            Assert.Equal("GBP", response.Currency);
            Assert.Equal(100, response.Amount);
            Assert.Equal(expectedAuthCode, response.AuthorizationCode);

            _mockBankClient.Verify(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task RequestReturnsUnAuthorizedResponse()
        {
            var request = DefaultRequest();
            var expectedAuthCode = Guid.NewGuid().ToString();

            _mockBankClient
                .Setup(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BankResponse { Authorized = false, AuthorizationCode = string.Empty });

            var response = await paymentService.ProcessPayment(request, CancellationToken.None);

            Assert.Equal(PaymentStatus.Declined, response.Status);
            Assert.Equal("8877", response.CardNumberLastFour);
            Assert.Equal(4, response.ExpiryMonth);
            Assert.Equal(2030, response.ExpiryYear);
            Assert.Equal("GBP", response.Currency);
            Assert.Equal(100, response.Amount);
            Assert.Empty(response.AuthorizationCode);

            _mockBankClient.Verify(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }


        [Fact]
        public async Task BankServiceUnavailableThrowsException()
        {
            var request = DefaultRequest();

            var expectedAuthCode = Guid.NewGuid().ToString();

            _mockBankClient
                .Setup(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new BankServiceUnavailableException());

            await Assert.ThrowsAsync<BankServiceUnavailableException>(async () => await paymentService.ProcessPayment(request, CancellationToken.None));
            _mockBankClient.Verify(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task BankBadRequestThrowsException()
        {
            var request = DefaultRequest();

            var expectedAuthCode = Guid.NewGuid().ToString();

            _mockBankClient
                .Setup(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new BankServiceBadRequestException());

            await Assert.ThrowsAsync<BankServiceBadRequestException>(async () => await paymentService.ProcessPayment(request, CancellationToken.None));
            _mockBankClient.Verify(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private PostPaymentRequest DefaultRequest() => new()
        {
            CardNumber = "2222405343248877",
            ExpiryMonth = 4,
            ExpiryYear = 2030,
            Currency = "GBP",
            Amount = 100,
            Cvv = "123"
        };
    }
}
