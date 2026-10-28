using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

using Moq;

using PaymentGateway.Api.Clients;
using PaymentGateway.Api.Controllers;
using PaymentGateway.Api.Enums;
using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validation;

namespace PaymentGateway.Api.Tests;

public class PaymentsControllerTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private WebApplicationFactory<PaymentsController> _webApplicationFactory = new();
    private HttpClient _httpClient;

    public PaymentsControllerTests()
    {
        var paymentsRepository = new PaymentsRepository();

        _httpClient = _webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(TimeProvider.System)
                .AddSingleton(paymentsRepository)
                .AddScoped<PaymentService>()
                .AddSingleton<PostPaymentRequestValidator>()
                ))
            .CreateClient();
    }


    [Fact]
    public async Task PostReturnsSuccessfulRequest()
    {
        var paymentRequest = new
        {
            CardNumber = "12345678901211",
            Amount = 100,
            Currency = "GBP",
            ExpiryMonth = 1,
            ExpiryYear = 2029,
            Cvv = "123"
        };

        using var response = await _httpClient.PostAsJsonAsync("/api/payments", paymentRequest, CancellationToken.None);
        var actual = await response.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions, CancellationToken.None);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(actual);
        Assert.Equal(PaymentStatus.Authorized, actual.Status);
        Assert.Equal("1211", actual.CardNumberLastFour);
        Assert.Equal(1, actual.ExpiryMonth);
        Assert.Equal(2029, actual.ExpiryYear);
        Assert.Equal("GBP", actual.Currency);
        Assert.Equal(100, actual.Amount);
    }

    [Fact]
    public async Task ValidationErrorReturnsBadRequest()
    {
        var paymentRequest = new
        {
            CardNumber = "333",
            Amount = 100,
            Currency = "GBP",
            ExpiryMonth = 10,
            ExpiryYear = 2029,
            Cvv = "123"
        };

        using var response = await _httpClient.PostAsJsonAsync("/api/payments", paymentRequest, CancellationToken.None);
        var actual = await response.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions, CancellationToken.None);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BankUnavailableReturns503()
    {
        var paymentRequest = new
        {
            CardNumber = "12345678901210",
            Amount = 100,
            Currency = "GBP",
            ExpiryMonth = 10,
            ExpiryYear = 2029,
            Cvv = "123"
        };

        using var response = await _httpClient.PostAsJsonAsync("/api/payments", paymentRequest, CancellationToken.None);
        var actual = await response.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions, CancellationToken.None);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }


    [Fact]
    public async Task RetrievesAPaymentSuccessfully()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            ExpiryYear = DateTime.Now.AddYears(2).Year,
            ExpiryMonth = DateTime.Now.AddMonths(2).Month,
            Amount = 1234,
            CardNumberLastFour = "123",
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions);
        
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
    }

    [Fact]
    public async Task Returns404IfPaymentNotFound()
    {
        // Arrange
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        var client = webApplicationFactory.CreateClient();
        
        // Act
        var response = await client.GetAsync($"/api/Payments/{Guid.NewGuid()}");
        
        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RetrievedPaymentMatchesStoredPayment()
    {
        // Arrange
        var payment = new PostPaymentResponse
        {
            Id = Guid.NewGuid(),
            Status = PaymentStatus.Declined,
            ExpiryYear = 2030,
            ExpiryMonth = 12,
            Amount = 200,
            CardNumberLastFour = "123",
            Currency = "GBP"
        };

        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var client = CreateClient(paymentsRepository);

        // Act
        var response = await client.GetAsync($"/api/Payments/{payment.Id}");
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(paymentResponse);
        Assert.Equal(payment.Id, paymentResponse.Id);
        Assert.Equal(payment.Status, paymentResponse.Status);
        Assert.Equal(payment.CardNumberLastFour, paymentResponse.CardNumberLastFour);
        Assert.Equal(payment.ExpiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(payment.ExpiryYear, paymentResponse.ExpiryYear);
        Assert.Equal(payment.Currency, paymentResponse.Currency);
        Assert.Equal(payment.Amount, paymentResponse.Amount);
    }

    [Fact]
    public async Task PostAuthorizedPaymentReturnsOk()
    {
        // Arrange
        var expectedAuthCode = Guid.NewGuid().ToString();
        var mockBankClient = new Mock<IBankClient>();
        mockBankClient
            .Setup(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankResponse { Authorized = true, AuthorizationCode = expectedAuthCode });

        var client = CreateClient(mockBankClient);
        var request = DefaultPostRequest();

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions);
        Assert.NotNull(paymentResponse);
        Assert.NotEqual(Guid.Empty, paymentResponse.Id);
        Assert.Equal(PaymentStatus.Authorized, paymentResponse.Status);
        Assert.Equal("8877", paymentResponse.CardNumberLastFour);
        Assert.Equal(request.ExpiryMonth, paymentResponse.ExpiryMonth);
        Assert.Equal(request.ExpiryYear, paymentResponse.ExpiryYear);
        Assert.Equal(request.Currency, paymentResponse.Currency);
        Assert.Equal(request.Amount, paymentResponse.Amount);
        Assert.Equal(expectedAuthCode, paymentResponse.AuthorizationCode);

        mockBankClient.Verify(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PostDeclinedPaymentReturnsOk()
    {
        // Arrange
        var mockBankClient = new Mock<IBankClient>();
        mockBankClient
            .Setup(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankResponse { Authorized = false, AuthorizationCode = string.Empty });

        var client = CreateClient(mockBankClient);

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", DefaultPostRequest());

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var paymentResponse = await response.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions);
        Assert.NotNull(paymentResponse);
        Assert.Equal(PaymentStatus.Declined, paymentResponse.Status);
        Assert.Empty(paymentResponse.AuthorizationCode);
    }

    [Fact]
    public async Task PostedPaymentCanBeRetrieved()
    {
        // Arrange
        var mockBankClient = new Mock<IBankClient>();
        mockBankClient
            .Setup(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BankResponse { Authorized = true, AuthorizationCode = Guid.NewGuid().ToString() });

        var client = CreateClient(mockBankClient);

        // Act
        var postResponse = await client.PostAsJsonAsync("/api/Payments", DefaultPostRequest());
        var postedPayment = await postResponse.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions);
        var getResponse = await client.GetAsync($"/api/Payments/{postedPayment!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var retrievedPayment = await getResponse.Content.ReadFromJsonAsync<PostPaymentResponse>(JsonOptions);
        Assert.NotNull(retrievedPayment);
        Assert.Equal(postedPayment.Id, retrievedPayment.Id);
        Assert.Equal(postedPayment.Status, retrievedPayment.Status);
        Assert.Equal(postedPayment.CardNumberLastFour, retrievedPayment.CardNumberLastFour);
    }

    [Fact]
    public async Task PostInvalidPaymentReturnsBadRequestAndDoesNotCallBank()
    {
        // Arrange
        var mockBankClient = new Mock<IBankClient>();
        var client = CreateClient(mockBankClient);

        var request = DefaultPostRequest();
        request.Amount = 0;
        request.CardNumber = "not-a-card-number";

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.NotEmpty(body.RootElement.GetProperty("errors").EnumerateArray());

        mockBankClient.Verify(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PostPaymentWithExpiredCardReturnsBadRequest()
    {
        // Arrange
        var mockBankClient = new Mock<IBankClient>();
        var client = CreateClient(mockBankClient);

        var lastMonth = DateTime.UtcNow.AddMonths(-1);
        var request = DefaultPostRequest();
        request.ExpiryMonth = lastMonth.Month;
        request.ExpiryYear = lastMonth.Year;

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        mockBankClient.Verify(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task PostPaymentReturnsBadRequestWhenBankRejectsRequest()
    {
        // Arrange
        var mockBankClient = new Mock<IBankClient>();
        mockBankClient
            .Setup(x => x.ProcessPayment(It.IsAny<BankRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BankServiceBadRequestException("bad request"));

        var client = CreateClient(mockBankClient);

        // Act
        var response = await client.PostAsJsonAsync("/api/Payments", DefaultPostRequest());

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private static PostPaymentRequest DefaultPostRequest() => new()
    {
        CardNumber = "2222405343248877",
        ExpiryMonth = 4,
        ExpiryYear = DateTime.UtcNow.AddYears(2).Year,
        Currency = "GBP",
        Amount = 100,
        Cvv = "123"
    };

    private static HttpClient CreateClient(Mock<IBankClient> mockBankClient)
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        return webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services => services
                .AddSingleton(mockBankClient.Object)))
            .CreateClient();
    }

    private static HttpClient CreateClient(PaymentsRepository paymentsRepository)
    {
        var webApplicationFactory = new WebApplicationFactory<PaymentsController>();
        return webApplicationFactory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => ((ServiceCollection)services)
                .AddSingleton(paymentsRepository)))
            .CreateClient();
    }
}