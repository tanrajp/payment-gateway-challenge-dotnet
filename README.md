# Instructions for candidates

This is the .NET version of the Payment Gateway challenge. If you haven't already read this [README.md](https://github.com/cko-recruitment/) on the details of this exercise, please do so now. 

## Template structure
```
src/
    PaymentGateway.Api - a skeleton ASP.NET Core Web API
test/
    PaymentGateway.Api.Tests - an empty xUnit test project
imposters/ - contains the bank simulator configuration. Don't change this

.editorconfig - don't change this. It ensures a consistent set of rules for submissions when reformatting code
docker-compose.yml - configures the bank simulator
PaymentGateway.sln
```

## Running Locally
To run the api locally run:

dotnet run --project src/PaymentGateway.Api

To run the tests, the simulator must be running as it is required.

docker-compose up -d
dotnet test


Feel free to change the structure of the solution, use a different test library etc.

## Assumptions
- Currency needs to be uppercase
- Autorization code field can / should be returned to the caller.
- 5xx from the bank client doen't need to be retried.
- Rejected payments aren't stored as they don't reach the bank.
- 

## Improvements
- Add idempotency key to the post endpoint this is to prevent charging custoemr twice.
- implement auth
- Currency - instead of an enum use a currency provider or maintain a list.
- Improve the tests that require the docker container to be running to start the container in the tests. Therefore starting up the simulator isn't required before the tests can run.
- Implement a correlation Id to track the request through.
- Add readiness / liveness endpoints

## Obervability
- Track how long the call to the third party takes.
- Track endpoint status codes to track trends.