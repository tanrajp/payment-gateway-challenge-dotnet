using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Exceptions;
using PaymentGateway.Api.Models;
using PaymentGateway.Api.Models.Requests;
using PaymentGateway.Api.Models.Responses;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validation;

namespace PaymentGateway.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
public class PaymentsController : Controller
{
    private readonly PaymentsRepository _paymentsRepository;
    private readonly PostPaymentRequestValidator _validator;
    private readonly PaymentService _paymentService;

    public PaymentsController(PaymentsRepository paymentsRepository, PostPaymentRequestValidator validator, PaymentService paymentService)
    {
        _paymentsRepository = paymentsRepository;
        _validator = validator;
        _paymentService = paymentService;
    }

    [HttpPost]
    public async Task<ActionResult<PostPaymentResponse>> PostPaymentAsync([FromBody]PostPaymentRequest request, CancellationToken cancellationToken)
    {
        var validationResult = _validator.Validate(request);
        if (!validationResult.IsValid)
        {
            return BadRequest(new
            {
                status = PaymentStatus.Rejected,
                errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList()
            });
        }

        try
        {
            var ret = await _paymentService.ProcessPayment(request, cancellationToken);
            return new OkObjectResult(ret);
        }
        catch (BanksServiceBadRequestException bre)
        {
            return BadRequest(new
            {
                status = PaymentStatus.Rejected,
                errors = bre.Message
            });
        }catch(BankServiceUnavailableException bse)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                error = "Error posting payment, please  retry later"
            });
        }
        catch(Exception e)
        {
            throw;
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<GetPaymentResponse?>> GetPaymentAsync(Guid id)
    {
        var payment = _paymentsRepository.Get(id);

        if (payment == null)
        {
            return new NotFoundResult();
        }

        return new OkObjectResult(payment);
    }
}