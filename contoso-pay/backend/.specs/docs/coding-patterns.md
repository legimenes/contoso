# Coding Patterns

This document provides technical implementation patterns for endpoints, use cases, repositories and adapters in our architecture.

## General Development Guideline (For Developers and LLMs)

When adding a new feature, follow this mental workflow:

1. **Entities (Domain)**: Does the business rule require changing the state of an entity? Modify the `Domain` first.
2. **Contracts (Core)**: If communication with an external system is required (database, queue, vendor), create the interface in Core.
3. **Use Case (Core)**: Implement the feature following the Vertical Slice approach. Create a new directory named after the feature, then another directory for the business capability, and add its respective files (e.g., Interface, UseCase, Request, Response, and Validator).
4. **Data Access (Persistence/Adapters)**: Implement the newly created interfaces in their respective adapters.
5. **Exposure (API)**: Create the endpoint by mapping the Minimal API to trigger the corresponding use case in `Core`. Never implement business rules in the Minimal API.

## Results Pattern

Use the **Results Pattern** to represent the outcome of an operation explicitly, distinguishing successful execution from failure without relying on exceptions for expected business scenarios. A `Result` object should indicate whether the operation succeeded or failed and, when appropriate, carry the resulting value on success or error information on failure.

**Recommendation**: Prefer the **Results Pattern** for methods that return an object or value and may encounter an expected failure during execution. Examples include use cases, repository operations, domain services, and external service integrations. Instead of returning `null`, using ambiguous return values, or throwing exceptions for expected failures, return a `Result<T>` that clearly communicates success or failure and provides the corresponding value or error information. Reserve exceptions for unexpected situations and exceptional conditions that cannot be reasonably represented as part of the operation's normal flow.

The application provides an internal **Results Pattern** implementation under `Domain/Core/Results`. Reuse the existing result types and abstractions whenever an operation needs to explicitly represent success or failure. Do not introduce external libraries or create alternative result abstractions unless explicitly required.

## Domain Entities

Domain entities must strictly adhere to the following rules:

- **Single constructor**: Each entity must have only one constructor, accepting all the properties required to fully initialize the instance.
- **Static factory method**: Each entity must provide a **static factory method** responsible for creating and returning a new instance of the entity by using the single constructor.

Conceptual example:

```cs
public sealed class Customer(
    Guid id,
    string name,
    string document,
    DateTime createdAtUtc)
{
    public Guid Id { get; } = id;
    public string Name { get; } = name;
    public string Document { get; } = document;
    public DateTime CreatedAtUtc { get; } = createdAtUtc;

    public static Customer New(
        string name,
        string document)
    { 
        return new(
            id: Guid.NewGuid(),
            name: name.Trim(),
            document: document.Trim(),
            createdAtUtc: DateTime.UtcNow);
    }
}
```

## Resource File

All user-facing messages must be defined in the **resource file** `Resource.resx` located in the **Domain** project. Do not hardcode message strings throughout the codebase. Always reference the appropriate resource entry when displaying validation messages, error messages, or other messages that may be exposed to the user. Hardcoded strings are allowed only for one-off log messages that will not be exposed to the user.

## Features Folder Structure

For example, in the platform's Signup feature, you could find the following feature-oriented structure (**Vertical Slice** + **Screaming**). All files related to an operation are kept together:

```text
Core
└── UseCases/
    └── Signup/
        └── Initiate/
            ├── IInitiateRegistration.cs
            ├── InitiateRegistration.cs
            ├── InitiateRegistrationRequest.cs
            ├── InitiateRegistrationResponse.cs
            └── InitiateRegistrationRequestValidator.cs
```

*Features Coding Rules*:

- Use the **`FluentValidation` NuGet package** to implement validators for all request objects. Do not use data annotations or custom validation logic.
- Each *Use Case* MUST own its own *Request* and *Response* models within its respective directory. **NEVER reuse, reference, or share Request or Response models between different Use Cases**, even if their definitions are structurally identical.
- Generic standardized contracts - such as pagination envelopes - MAY live under the `Abstractions` folder and be composed into any use case's response models; per-use-case Request and Response models MUST still be owned by the slice and never shared.
- **Paginated requests** are positional records inheriting from `Core.Abstractions.Pagination.PaginationRequest`, restating Page/PageSize defaults only via the base's constants (`Page = PaginationRequest.PageDefault`, `PageSize = PaginationRequest.PageSizeDefault`); slice validators MUST inherit from `AbstractValidator<TRequest>` and execute the shared pagination rules through `Include(new PaginationRequestValidator())` (composition; FluentValidation's contravariant `IValidator<in T>` accepts the base-type validator), so the pagination rules run for every slice without duplication.

## Driven Ports

All **Driven Port interfaces** must be placed according to their scope, following the Vertical Slice Architecture pattern:

- **Feature-specific interfaces**: Driven Port interfaces that belong to a specific feature must be created inside that feature's folder.
- **Generic interfaces**: Driven Port interfaces that provide cross-cutting or reusable capabilities across multiple features must be created inside the `Abstractions` folder.

## Data Access

Use the `Repository Pattern` for data access, keeping persistence logic isolated from business logic. Use **EF Core** as the ORM for database operations. Do not access the `DbContext` directly from business layers; use repository interfaces and their implementations, following dependency injection principles.

**Never drop Docker volumes to apply schema changes**: fresh environments pick the file up via the init script, while existing databases receive only the new delta executed directly (e.g. via `psql`), with the file remaining the single source of truth.

### Repositories and Queries

Split data access into two ports, classified by what the caller needs:

- **Repository** — the collection of aggregates. It creates aggregates, retrieves them by identity, and answers questions about the collection itself (existence by a natural key). It returns aggregates.
- **Queries** — the read side. It serves filtered and paginated reads shaped by the reader, returning the slice's own projection (its `Response` record) and never an aggregate.

The classification rule: a question about the aggregate collection belongs to the repository; a read for a reader, expressed as a projection, belongs to the queries. `GetByIdAsync` stays on the repository because it returns an aggregate; a filtered listing belongs to the queries.

A queries port takes the slice's request and returns the slice's own response: both sides of the contract belong to the slice. The request arrives raw, so normalization (trimming filters) stays with the use case, which hands the query a normalized copy — the adapter never normalizes.

A uniqueness rule that spans aggregates is a **collection constraint**, not an aggregate invariant — a single aggregate cannot guarantee it. It belongs to the repository (existence check by natural key), backed by a unique index, with the constraint violation mapped as a safety net for the race.

Do not introduce a separate read model type when the slice's own `Response` already carries the projection, and do not add a generic `IQueries<,>` base before a second aggregate actually requires it.

Both are driven ports: place each one in the folder of the feature that owns it (`Core/UseCases/<Feature>/`) and implement it in `Adapters.Persistence`, under `Repositories/` and `Queries/` respectively. A queries port named after an aggregate still lives in the feature folder, never in `Abstractions/`.

### EF Core Conventions

Use dedicated entity configuration classes implementing `IEntityTypeConfiguration<TEntity>` to define entity mappings, including table names, column properties, primary keys, relationships, constraints, indexes, and other database-specific configurations. Keep these mappings separate from domain entities and apply them through the `DbContext` configuration.

Do not use *Migrations* in **Entity Framework Core** to manage database schema changes. Maintain a single SQL file at `docker/scripts/postgresql/01-objects.sql` containing the complete and up-to-date definition of all database objects. Whenever the data model changes, update this file to reflect the desired schema, including tables, columns, keys, relationships, indexes, constraints, and any other required objects.

Do not declare `DbSet<TEntity>` properties in the `DbContext` to expose entities. To access entities in repositories, use `dbContext.Set<TEntity>()` directly.

## Asynchronous Calls

**Never use `.ConfigureAwait()` in asynchronous code**. This applies to all await expressions throughout the application. Always use await directly on asynchronous operations without chaining `.ConfigureAwait(false)` or `.ConfigureAwait(true)`.

## General Coding Rules and Style

- **Nested classes are not allowed within the same file**: Classes must always be defined in separate files, in order to maintain clean navigation and appropriate granularity.
- **File-scoped namespaces**: All C# files must use file-scoped namespaces instead of block-scoped namespaces.
- **Primary constructors**: Use primary constructors whenever a class or record requires constructor parameters. Avoid declaring explicit constructors when the primary constructor syntax is applicable.
- **Expression-bodied member**: Não utilize expression-bodied members (=>) na declaração de métodos. Sempre utilize a sintaxe tradicional com blocos de código delimitados por {}. Métodos que retornam valores devem utilizar a instrução `return` explicitamente.
- **Never add a newline character at the end of a C# source file**: The file must end immediately after the final character of the last line of code, typically the closing brace `}`.