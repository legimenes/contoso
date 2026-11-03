# 02: Ciclo de vida do app client (criação, leitura e rotação)

**What to build:** um integrador cria o app client único do seu merchant publicamente, lê os dados públicos com Bearer e rotaciona o secret provando o secret atual. `POST /v1/appclients` responde `201 {clientId, clientSecret}` e é público; `GET /v1/appclients/{clientId}` responde `200 {clientId, merchantId, name}` e exige Bearer; `POST /v1/appclients/{clientId}/rotate-secret` exige Bearer e `{currentSecret}`, devolve `200 {clientId, clientSecret}` e invalida o secret anterior imediatamente. O `client_id` nunca muda e não existe endpoint de desativação.

**Blocked by:** 01: Fundação de identidade com OpenIddict e emissão de token.

**Status:** ready-for-agent

- [ ] `POST /v1/appclients` público com `{merchantId, name}` válido retorna `201 {clientId, clientSecret}` e persiste exatamente um app client para o merchant
- [ ] Merchant inexistente retorna `404`; merchant inativo retorna `409`; merchant que já tem app client retorna `409`; corpo inválido retorna `400`; todos no envelope padrão de problema
- [ ] `GET /v1/appclients/{clientId}` exige Bearer, devolve `200 {clientId, merchantId, name}` sem qualquer campo de secret e retorna `401` sem token válido
- [ ] `GET` de client desconhecido ou de outro merchant retorna `404`, sem revelar a existência do client alheio
- [ ] `POST /v1/appclients/{clientId}/rotate-secret` com Bearer e `{currentSecret}` correto devolve `200 {clientId, clientSecret}`, mantém o `client_id` e invalida o secret anterior imediatamente
- [ ] Rotação com secret atual errado retorna `401` no envelope padrão; client desconhecido ou de outro merchant retorna `404`
- [ ] O secret em texto puro só aparece na resposta de criação e na de rotação; `GET` nunca o devolve e o banco guarda apenas o hash; nenhuma resposta ou log ecoa o secret recebido
- [ ] A regra 1:1 é garantida pelo índice único de `merchant_id`, pela checagem de existência no use case e pelo mapeamento de unique violation para `409` como rede de segurança da corrida
- [ ] Um token emitido antes da rotação ou da desativação do merchant continua válido até expirar, porque a validação não consulta o banco; isso é comportamento documentado, não bug
- [ ] Os três endpoints entram na referência Scalar da v1 com exemplos executáveis em `API.http`; o `GET` e a rotação documentam o requisito de Bearer
- [ ] Testes unitários cobrem a matriz de criação (merchant ausente/inativo, client já existente, nome inválido, sucesso), leitura (sucesso, desconhecido, merchant estrangeiro) e rotação (sucesso, desconhecido/estrangeiro, secret errado, secret ausente)
- [ ] Testes de integração contra Postgres real cobrem `201` + `GET` sem secret, `409` de duplicata, `404` de merchant, `409` de merchant inativo, `401` sem Bearer, `200`/`401`/`404` da rotação e o ciclo em que o secret novo obtém token e o antigo falha
- [ ] As specs antigas `.specs/scratch/appclients/spec.md` e `.specs/scratch/connect/spec.md` ficam marcadas como superseded; glossário e ADRs refletem o modelo 1:1 e a rotação
- [ ] Build sem warnings e suíte completa verde

## Technical plan (pending approval)

### DDL delta

Nenhum. As tabelas `app_clients` e `app_client_tokens`, os índices e a FK de `merchant_id` entram no ticket 01. Nenhum volume é tocado nesta fatia.

### Classes, campos e tamanhos

- `Core/UseCases/AppClients/CreateAppClient/`:
  - `CreateAppClientRequest(Guid MerchantId, string Name)`.
  - `CreateAppClientRequestValidator`: `MerchantId` não vazio; `Name` obrigatório, `Trim`, máximo 100.
  - `ICreateAppClient`, `CreateAppClient` e `CreateAppClientResponse(string ClientId, string ClientSecret)`.
  - Fluxo: `IMerchantRepository.GetByIdAsync` → `404` se ausente; `!IsActive` → `409`; `IAppClientCredentials.ExistsByMerchantIdAsync` → `409`; `CreateAsync` → `201`; `DuplicateAppClientException` → `409`.
- `Core/UseCases/AppClients/GetAppClient/`:
  - `GetAppClientRequest(string ClientId, Guid MerchantId)`, `IGetAppClient`, `GetAppClient`, `GetAppClientResponse(string ClientId, Guid MerchantId, string Name)`.
  - `IAppClientQueries` no folder da slice: `Task<GetAppClientResponse?> GetByClientIdAsync(GetAppClientRequest request, CancellationToken cancellationToken)`.
  - Fluxo: normaliza `ClientId` (trim); consulta a projeção; ausente ou `MerchantId` diferente do token → `404`.
- `Core/UseCases/AppClients/RotateAppClientSecret/`:
  - `RotateAppClientSecretRequest(string ClientId, Guid MerchantId, string CurrentSecret)`, validator com client id e current secret obrigatórios.
  - `IRotateAppClientSecret`, `RotateAppClientSecret` e `RotateAppClientSecretResponse(string ClientId, string ClientSecret)`.
  - Fluxo: `FindByClientIdAsync`; ausente ou merchant diferente → `404`; `ValidateSecretAsync` falso → `401`; `RotateSecretAsync` → `200`; client removido na corrida → `404`.
- `Adapters.Identity/AppClients/IAppClientCredentials.cs` (já criada no ticket 01): usada pelos slices de criação e rotação; retorna `AppClientCredential(string ClientId, string ClientSecret)`, `AppClientRecord(string ClientId, Guid MerchantId, string Name)` e `bool`/`string` nas operações de validação e rotação.
- `Adapters.Identity/Queries/AppClientQueries.cs`: implementa `IAppClientQueries` com `dbContext.Set<AppClient>().Where(...).Select(...)`; não retorna a entidade.
- `API/Endpoints/v1/AppClients/`: `CreateAppClientEndpoint`, `GetAppClientEndpoint` e `RotateAppClientSecretEndpoint`; `Tags.AppClients`; `RequireAuthorization()` no `GET` e na rotação; `merchant_id` extraído do `ClaimsPrincipal` e passado ao use case; claim ausente/inválida → `401`.
- Contratos HTTP:
  - `POST /v1/appclients` → `201 {clientId, clientSecret}`; `400`, `404`, `409`, `409`.
  - `GET /v1/appclients/{clientId}` → `200 {clientId, merchantId, name}`; `401`, `404`.
  - `POST /v1/appclients/{clientId}/rotate-secret` → `200 {clientId, clientSecret}`; `401`, `404`.
- Tamanhos: `clientId` 32 caracteres, `clientSecret` 43 caracteres, `name` até 100, `merchantId` UUID; nenhum desses valores é reutilizado como identificador legível.
- `Domain/Resources/Resource.resx`: novas mensagens `Validation_Merchant_Inactive`, `Validation_AppClient_AlreadyExists`, `Validation_AppClient_NotFound`, `Validation_AppClient_CurrentSecret_Required`, `Validation_AppClient_CurrentSecret_Invalid`, `Validation_MerchantId_Required` e `Validation_ClientId_Required`; validators e use cases referenciam `Resource.*`, nunca strings soltas.

### Erros e mapeamento

- `404`: merchant inexistente na criação; client inexistente ou de outro merchant na leitura e na rotação.
- `409`: merchant inativo na criação; merchant que já possui app client na criação.
- `401`: secret atual errado na rotação (ProblemDetails via `ResultType.Unauthorized`); Bearer ausente/inválido nas rotas protegidas (challenge de autenticação).
- `400`: corpo inválido ou campos obrigatórios ausentes (ProblemDetails via FluentValidation).
- O endpoint de token continua com erros OAuth; nenhum erro desta fatia muda o contrato do ticket 01.

### Seams e testes

- Seam primário: HTTP com `WebApplicationFactory` e Postgres real, reaproveitando o setup dos merchants. Seam secundário: use cases com NSubstitute e builders (`CreateAppClientRequestBuilder`, `RotateAppClientSecretRequestBuilder`, `AppClientCredentialBuilder`).
- O adapter de identidade é exercitado pelo endpoint; não há teste isolado de `AppClientQueries`/`AppClientRepository`.
- Testes de integração: um único caso de invalidação por endpoint (regra do repo); a matriz de validação fica na unidade. O ciclo de rotação é um teste de integração com o token endpoint: secret novo → `200`, secret antigo → `401 invalid_client`.
- Teste de integração de escopo: criar app client do merchant A, obter token de A, tentar ler/rotacionar um client de B → `404`.
- Sem sleep: relógio controlável no host de teste para o caso de token expirado; a rotação não depende de tempo.

### Documentação

- `API.http` ganha `createAppClient`, `getAppClient` e `rotateAppClientSecret`, com o `clientId`/`clientSecret` encadeados pelas respostas.
- Scalar v1 lista os três endpoints; o `GET` e a rotação aparecem com o cadeado de Bearer.
- As specs antigas de `appclients` e `connect` são marcadas como superseded; o glossário passa a dizer `Merchant 1:1 AppClient`; ADR-0004 registra OpenIddict/1:1/rotação e ADR-0003 é emendada.
