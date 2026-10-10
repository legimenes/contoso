# Architecture Overview

This document provides a high-level introduction to our architecture approach and serves as a navigation hub to detailed guidelines.

## Core Philosophy

Our architecture blends concepts from **Clean Architecture**, **Ports and Adapters (Hexagonal Architecture)**, and **Vertical Slice Architecture**, specifically designed for our scenario, which consists of a single Bounded Context.

The primary goal of this architecture is to isolate business rules from infrastructure and framework details, ensuring that the system is testable, maintainable, and ready to scale as its complexity increases.

## Project Structure (Solution)

The solution is divided into the following logical projects, following a strict dependency direction rule (from the outside in):

- **`API`**: The application's entry point. It uses the ASP.NET Core Minimal APIs pattern. It is responsible only for routing, initial dependency injection, and mapping HTTP requests to use cases.
- **`Domain`**: The heart of the software. It contains exclusively the entities and domain rules inherent to the business. **It has no dependencies on infrastructure or any other project in the solution**.
- **`Domain`**: The heart of the software. It contains the entities and domain rules inherent to the business. It also contains common classes, value objects and types that need to be shared exclusively between the `Domain` and `Core` layers (e.g., result types, standardized domain exceptions, and base interfaces). **It has no dependencies on infrastructure or any other project in the solution**.
- **`Core`**: The orchestration layer. It contains the application's use cases and domain services. This is where the application flow is orchestrated. It depends on `Domain` and `SharedKernel`.
- **`Adapters.Persistence`**: The concrete implementation of data access. It contains repositories, ORM configurations (such as Entity Framework Core), and database migrations. It acts as an **outbound Adapter**.
- **`Tests`**: Automated testing setup covering both unit and integration tests.

## Architectural Patterns and Characteristics

### Single Bounded Context

The entire business domain is contained within a single context. We do not prematurely fragment the domain into multiple bounded contexts or microservices. All ubiquitous language and business models coexist cohesively within the `Domain` layer.

### Vertical Slice and Screaming Architecture

The `Core` layer is not organized by technical concerns (e.g., `UseCases folders`, `DTOs folders`), but rather by **business capabilities** (Screaming Architecture).

### Ports and Adapters (Hexagonal Architecture)

The application was designed to be agnostic to external resources (vendors).

- **Ports**: Interfaces that define contracts (e.g., **IConnectService**, **IAppClientRepository**) must reside in **Core**.
- **Adapters**: Implementations such as **Adapters.Persistence**.
- **Replaceability**: If we need to replace the identity provider or database in the future, only a new **Adapters.*** project needs to be created, with no changes required to **Core**.

## Project Architecture Guide

### Strict Rules (Do Not Violate!):

- The **Domain** layer must never reference **Core**, **Persistence**, **API**, or **Adapters**.
- Circular references between Slices within **Core** must be avoided. If two use cases share a significant amount of logic, extract it into a **domain service within Core**.
- **Adapters** projects reference **Core** (to implement its interfaces), but **Core** must never reference the adapters. Composition and dependency injection take place exclusively at the **API** level (Composition Root).
