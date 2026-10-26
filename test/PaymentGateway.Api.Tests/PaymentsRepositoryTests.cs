using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests;

public class PaymentsRepositoryTests
{
    private readonly Random _random = new();

    [Fact]
    public void AddStoresPayment()
    {
        // Arrange
        var payment = CreatePayment();
        var paymentsRepository = new PaymentsRepository();

        // Act
        paymentsRepository.Add(payment);

        // Assert
        Assert.Single(paymentsRepository.Payments, payment);
    }

    [Fact]
    public void GetReturnsPaymentThatWasAdded()
    {
        // Arrange
        var payment = CreatePayment();
        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        // Act
        var result = paymentsRepository.Get(payment.Id);

        // Assert
        Assert.Same(payment, result);
    }

    [Fact]
    public void GetReturnsNullWhenRepositoryIsEmpty()
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();

        // Act
        var result = paymentsRepository.Get(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetReturnsNullWhenPaymentIdDoesNotMatch()
    {
        // Arrange
        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(CreatePayment());
        paymentsRepository.Add(CreatePayment());

        // Act
        var result = paymentsRepository.Get(Guid.NewGuid());

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GetReturnsMatchingPaymentWhenMultiplePaymentsExist()
    {
        // Arrange
        var first = CreatePayment();
        var second = CreatePayment();
        var third = CreatePayment();
        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(first);
        paymentsRepository.Add(second);
        paymentsRepository.Add(third);

        // Act
        var result = paymentsRepository.Get(second.Id);

        // Assert
        Assert.Same(second, result);
    }


    private PostPaymentResponse CreatePayment() => new()
    {
        Id = Guid.NewGuid(),
        Status = PaymentStatus.Authorized,
        ExpiryYear = _random.Next(2023, 2030),
        ExpiryMonth = _random.Next(1, 12),
        Amount = _random.Next(1, 10000),
        CardNumberLastFour = _random.Next(1111, 9999),
        Currency = "GBP"
    };
}
