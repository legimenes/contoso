---
Status: accepted
Date: 2026-10-21
---

# 0001: Contrato compartilhado de paginação (PaginationRequest + PagedResponse em Core/Abstractions/Pagination)

A listagem de merchants (ticket 03) fechou o contrato de listagem, e os próximos slices (`AppClients`, `sales`) vão repetir paginação e filtros. Decidimos centralizar o contrato de paginação em `backend/src/Core/Abstractions/Pagination/`:

- `PaginationRequest` — record abstrato **posicional** com defaults `Page = 1` e `PageSize = 20` e as constantes `PageDefault`, `PageMinimum`, `PageSizeMinimum`, `PageSizeMaximum`, `PageSizeDefault`; é herdada por toda request de slice que pagina.
- `PagedResponse<T>` — envelope de resposta, migrado do `Abstractions` raiz para o namespace `Core.Abstractions.Pagination`.
- `PaginationRequestValidator` — validator concreta (não genérica, sobre `PaginationRequest`) com as regras `page >= 1` e `pageSize` entre 1 e 100 com mensagens do resource file; os validators dos slices executam essas regras por composição (`Include(new PaginationRequestValidator())`), sem duplicação de código.

A forma herdada foi verificada por spike antes do aceite (FluentValidation 12.2.0, .NET 10): binding `[AsParameters]` aplicando os defaults, nomes de query intactos e parâmetros opcionais no OpenAPI.

## Considered Options

- **Composição** (`ListMerchantsRequest(..., PaginationRequest Pagination)`): rejeitada — o binding `[AsParameters]` liga pelo construtor, então a propriedade aninhada mudaria a superfície da query para `pagination.page=...`, quebrando o contrato do ticket 03.
- **Base não-posicional com propriedades settable (inicializadores 1/20 na base)**: rejeitada com spike — o binder `[AsParameters]` trata propriedades de tipo sem construtor posicional como **obrigatórias**: omitir `page`/`pageSize` na query produz `BadHttpRequestException: Required parameter "int Page"` (400), o inicializador nunca é aplicado e o OpenAPI gerado marca os parâmetros como `required=true` (contrato errado). A variante posicional, no mesmo spike, aplicou os defaults (sem query → 1/20), ligou por nome e listou os parâmetros como opcionais no OpenAPI.
- **Regras de validação duplicadas por slice**: rejeitada — risco de drift de limites e mensagens; `Include(new PaginationRequestValidator())` compila (`IValidator<in T>` é contravariante, então a validator da base entra na validator do slice) e executa as regras em todos os slices a partir de uma única fonte.
- **Nome `PagedResult` para o envelope**: rejeitado — "Result" já designa o Results Pattern (`Domain.Core.Results`, `Result<T>`, `ResultType`); `PagedResponse` mantém o vocabulário limpo.

## Consequences

- Requests paginadas são records posicionais que herdam de `PaginationRequest` e redeclaram os parâmetros `Page`/`PageSize` **referenciando as constantes da base** (`int Page = PaginationRequest.PageDefault`), nunca literais — um só ponto de mudança de default.
- O contrato HTTP do slice existente (`GET /v1/merchants`) fica intacto: mesmos query params, mesmos defaults, mesmo envelope, mesmas mensagens de 400.
- Erros de binding (`page=abc`) continuam fora do envelope de problema (comportamento de framework, registrado no ticket 03).
- O padrão (pasta, herança e validator compartilhado) deve ser registrado em `backend/.specs/docs/coding-patterns.md` como convenção para os próximos slices.
