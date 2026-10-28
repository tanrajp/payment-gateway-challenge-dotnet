using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;

namespace PaymentGateway.Api.Tests.Unit.ServicesTests;

public class PaymentsRepositoryTests
{
    private readonly Random _random = new();

    [Fact]
    public void AddStoresPayment()
    {
        var payment = CreatePayment();
        var paymentsRepository = new PaymentsRepository();

        paymentsRepository.Add(payment);

        Assert.Single(paymentsRepository.Payments, payment);
    }

    [Fact]
    public void GetReturnsPaymentThatWasAdded()
    {
        var payment = CreatePayment();
        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(payment);

        var result = paymentsRepository.Get(payment.Id);

        Assert.Same(payment, result);
    }

    [Fact]
    public void GetReturnsNullWhenRepositoryIsEmpty()
    {
        var paymentsRepository = new PaymentsRepository();

        var result = paymentsRepository.Get(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void GetReturnsNullWhenPaymentIdDoesNotMatch()
    {
        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(CreatePayment());
        paymentsRepository.Add(CreatePayment());

        var result = paymentsRepository.Get(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public void GetReturnsMatchingPaymentWhenMultiplePaymentsExist()
    {
        var first = CreatePayment();
        var second = CreatePayment();
        var third = CreatePayment();
        var paymentsRepository = new PaymentsRepository();
        paymentsRepository.Add(first);
        paymentsRepository.Add(second);
        paymentsRepository.Add(third);

        var result = paymentsRepository.Get(second.Id);

        Assert.Same(second, result);
    }


    private PostPaymentResponse CreatePayment() => new()
    {
        Id = Guid.NewGuid(),
        Status = PaymentStatus.Authorized,
        CardNumberLastFour = "123",
        ExpiryMonth = 12,
        ExpiryYear = 2030,
        Currency = "GBP",
        Amount = 159,
        AuthorizationCode = Guid.NewGuid().ToString()
    };
}
