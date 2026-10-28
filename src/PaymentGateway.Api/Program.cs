using Microsoft.Extensions.Options;

using PaymentGateway.Api.Clients;
using PaymentGateway.Api.Config;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<BankClientConfig>()
    .Bind(builder.Configuration.GetSection(BankClientConfig.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PaymentsRepository>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddSingleton<PostPaymentRequestValidator>();

builder.Services.AddHttpClient<IBankClient, BankClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<BankClientConfig>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
