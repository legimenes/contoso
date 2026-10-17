# Automated Tests

The automated testing project uses the `xUnit` framework and implements a comprehensive automated test suite, including **unit tests** for **use cases**, **services**, and **rich domain entities**, as well as **integration tests** to validate the application's complete flow starting from the **HTTP endpoints**.

All automated tests must follow the **Given–When–Then** pattern:

- **Given**: Set up the test context, objects, and initial conditions.
- **When**: Execute the action or behavior being tested.
- **Then**: Verify the expected outcome and the resulting side effects.

Conceptual example of folder structure:

```text
Tests/
├── Core/
│   └── Customers/
│       └── Create/
│           ├── CreateCustomerBuilder.cs
│           ├── CreateCustomerEndpointTests.cs
│           └── CreateCustomerTests.cs
└── Domain/
    ├── CustomerBuilder.cs
    └── CustomerTests.cs
```

## Test Methods Conventions

**Test names** must follow the convention `MethodName_When[Condition]_Should[ExpectedBehavior]`. The method name carries the *When* and *Should* parts of the **Given–When–Then** narrative.

Conceptual example:

```cs
public void GivenValidData_WhenCreatingCustomer_ShouldCreateCustomer()
```

The **test body** must contain comments using the terms *Given*, *When*, and *Then*.

Conceptual example:

```cs
[Fact]
[Trait("Customer", "Customer (Domain)")]
public void GivenValidData_WhenCreatingCustomer_ShouldCreateCustomer()
{
    // Given
    var name = "John Doe";
    var document = "98765432109";

    // When
    var customer = Customer.New(name, document);

    // Then
    Assert.NotNull(customer);
    Assert.Equal(name, customer.Name);
    Assert.Equal(customer.Document, document);
}
```

These conventions are highly readable and align with the **Given–When–Then** pattern: names emphasize the condition and the expected behavior, while the `Given` prefix may be used when the scenario context deserves emphasis.

### Test Categorization with *Trait*

All automated tests must be identified using **xUnit** `[Trait]` attributes, allowing tests to be categorized and selectively executed by type and purpose.

The convention uses a **single `[Trait]` pair**: a feature/capability key, followed by a value that embeds the test category in parentheses — `(Unit)`, `(Integration)`, `(Domain)` or `(Services)`.

Conceptual example:

```cs
[Fact]
[Trait("Customer", "CreateCustomer (Unit)")]
public async Task GivenValidCustomer_WhenCreatingCustomer_ShouldCreateCustomer()
{
}
```

Trait attributes can receive the following values:

```cs
[Trait("Customer", "Customer (Domain)")]
// or
[Trait("Customer", "CreateCustomer (Unit)")]
// or
[Trait("Customer", "CreateCustomer (Integration)")]
// or
[Trait("Email", "Send (Services)")]
```

## Unit Tests

Unit tests must cover:

- **Use Cases**: the validations defined in the request validator (if one exists), business rules, decision flows, and interactions with dependencies.
- **Services**: service behavior, including processing rules, error handling, and interactions with external dependencies.
- **Rich Domain Entities**: test the entities' behaviors, invariants, business rules, methods, and state transitions. Prioritize observable behavior rather than merely achieving coverage of properties or trivial methods.

Use the NuGet package **NSubstitute** to create substitutes for dependencies used in unit tests, such as repository interfaces, gateways, external services, providers, and other abstractions. Do not create manual fake, stub, or mock classes to replace dependencies that can be substituted using **NSubstitute**.

Classes following the ``Builder`` pattern must be created to build test objects, including, but not limited to, requests, responses, domain entities, value objects, and DTOs. These builders should simplify test data creation, improve readability, and promote reuse across tests.

Conceptual example of builder class:

```cs
public sealed class CreateCustomerRequestBuilder
{
    private string _name = "John Doe";
    private string _document = "98765432109";

    public CreateCustomerRequestBuilder WithName(string name)
    {
        _name = name;
        return this;
    }

    public CreateCustomerRequestBuilder WithEmail(string email)
    {
        _email = email;
        return this;
    }

    public CreateCustomerRequest Build()
    {
        return new CreateCustomerRequest
        {
            Name = _name,
            Email = _email
        };
    }
}
```

## Integration Tests

Integration tests must be created **exclusively for HTTP endpoints** and must validate the application's execution flow, starting with an HTTP request and continuing through the relevant application and domain layers to the persistence layer, when applicable. Do not create standalone integration tests for repositories, services, use cases, domain entities, or any other individual component.

Request validation must not be extensively tested through integration tests. At most, one integration test per endpoint is required to verify that request validation is triggered and that an invalid request produces the expected HTTP response. Do not create integration test cases specifically to verify request validation rules, such as required fields, invalid formats, missing values, or invalid property values. These validations should be covered by appropriate unit tests.

Preferably use `WebApplicationFactory<TEntryPoint>` and the `Microsoft.AspNetCore.Mvc.Testing` package when compatible with the application's structure.

*Substitutes* should be used for external dependencies when appropriate for the test scenario. However, do not substitute use cases, services, or domain entities that are part of the application flow being validated by the test.

At the end of an assisted execution, debugging session, or validation workflow, integration tests may be skipped using the **xUnit** `Skip` attribute **to avoid unnecessary consumption of external resources**.

If the `TestFixture` class exists, it should serve as a test fixture or test environment bootstrap. Its primary responsibility is to manually configure the dependency injection container `ServiceProvider` to simulate the application environment, allowing infrastructure repositories and services to be tested in isolation without the need to start an HTTP server.
