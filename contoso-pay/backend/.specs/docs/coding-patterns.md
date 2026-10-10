# Coding Patterns

This document provides technical implementation patterns for endpoints, use cases, repositories and adapters in our architecture.

## General Development Guideline (For Developers and LLMs)

When adding a new feature, follow this mental workflow:

1. **Entities (Domain)**: Does the business rule require changing the state of an entity? Modify the `Domain` first.
2. **Contracts (Core)**: If communication with an external system is required (database, queue, vendor), create the interface in Core.
3. **Use Case (Core)**: Implement the feature following the Vertical Slice approach. Create a new directory named after the feature, then another directory for the business capability, and add its respective files (e.g., Interface, UseCase, Request, Response, and Validator).
4. **Data Access (Persistence/Adapters)**: Implement the newly created interfaces in their respective adapters.
5. **Exposure (API)**: Create the endpoint by mapping the Minimal API to trigger the corresponding use case in `Core`. Never implement business rules in the Minimal API.

## Core Project

### Features inside the UseCases folder

For example, in the platform's Signup feature, you could find the following feature-oriented structure (Vertical Slice + Screaming). All files related to an operation are kept together:

```text
Core
└── UseCases/
    └── Signup/
        └── Initiate/
            ├── IInitiateRegistration.cs
            ├── InitiateRegistration.cs
            ├── InitiateRegistrationRequest.cs
            ├── InitiateRegistrationResponse.cs
            └── InitiateRegistrationValidator.cs
```

*Features Coding Rules*:

- If the **request** or **response** contains only a single property, there is no need to create a dedicated class or validator.

## Coding Rules and Style

- **Nested classes are not allowed within the same file**: Classes must always be defined in separate files, in order to maintain clean navigation and appropriate granularity.
- **File-scoped namespaces**: All C# files must use file-scoped namespaces instead of block-scoped namespaces.
- **Primary constructors**: Use primary constructors whenever a class or record requires constructor parameters. Avoid declaring explicit constructors when the primary constructor syntax is applicable.