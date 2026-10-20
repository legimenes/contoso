# 02: Consultar merchant (público temporário)

**What to build:** um integrador consulta um merchant pelo id e recebe id, nome e documento, temporariamente sem exigir token, até o slice de connect ligar a proteção.

**Blocked by:** 01: Criar merchant.

**Status:** resolved

- [x] `GET /v1/merchants/{id}` para id existente retorna `200 {id, name, document}` sem exigir token nesta fase
- [x] Id inexistente retorna `404` no envelope padrão de problema
- [x] Endpoint aparece na referência Scalar versionada com exemplo executável
- [ ] Proteção por Bearer fica registrada como dívida explícita para ser ligada quando o slice de connect existir, sem mudar o contrato de resposta
  *(Critério de dívida: registrado, não implementado — ver comentário; o item só fecha quando o guard entrar no slice de connect.)*

## Comments

- Implementado como slice vertical `GetById` (Domain reusado; porta `IMerchantRepository.GetByIdAsync` adicionada; `GetMerchantById`/`IGetMerchantById`/`GetMerchantByIdRequest`/`GetMerchantByIdResponse` no Core; adapter EF `FirstOrDefaultAsync`; endpoint Minimal API `GET /v1/merchants/{id:guid}` → `EndpointBase.CreateResult`, 200/404). Sem validador FluentValidation: a rota já vincula `Guid`, então não há requisição validável. `Program.cs` registra `IGetMerchantById`. Mensagem de not-found em `Resource.resx`/`pt-BR` (`Validation_Merchant_NotFound`).
- Testes conforme `automated-tests.md`: unidade NSubstitute (found → Ok com id/nome/documento; unknown → NotFound com erro da resource; não persiste) 3/3; integração via `WebApplicationFactory` com Postgres real (round-trip duplo POST→GET 200 com id/nome/documento; id inexistente → 404 + envelope) 2/2. Suite total 17/17 verde, build da solution 0 warnings. E2E manual: 201 → 200 → 404 (observation: rodando a API da máquina host, a connection string de Development aponta para o host Docker `postgres:5432`; usei override para `localhost:5333` — quirk pré-existente deste ambiente, fora do escopo do slice).
- **Dívida explícita de proteção:** o endpoint está público por decisão temporária do ticket. Quando o slice de connect existir, exigir `Authorization: Bearer <access_token>` neste endpoint (e nos demais GET) **sem mudar o contrato de resposta** de 200/404. Marcado em comentário no código (`GetMerchantByIdEndpoint.cs`).
- Exemplo executável: `API.http` ganhou `GET /v1/merchants/{id}`; OpenAPI v1 apresenta o path com `200 GetMerchantByIdResponse` e `404 ApiProblemDetails` (param `uuid`), que é o mesmo documento servido ao Scalar por versão.
- Decisão de cobertura (aprovada): o teste unitário `GivenAnyRequest_WhenGettingMerchant_ShouldNotPersistChanges` (assert de `DidNotReceive().AddAsync` no substituto) foi deliberadamente removido — contrato com a decisão de testes da spec (comportamento observável, não detalhes de implementação); o efeito persistente do get é coberto pela integração com Postgres real. Não restaurar sem nova decisão.
- Deduplicação (aprovada): o helper de leitura de corpo JSON migrou das cópias privadas dos testes de endpoint para `TestingWebApplicationFactory.ReadBodyAsync` (fixture compartilhada, ao lado de `GetConnectionString`); `NewDocument`/`DeleteByDocumentAsync` permanecem locais por serem específicos do cleanup de merchants (decisão a revisar quando mais recursos criarem merchants).
- Decisão pós-code-review: a rota usa a constraint `{id:guid}`; ids mal formados (não-Guid) caem no 404 vazio de rota do ASP.NET, sem envelope — o critério do ticket cobre apenas "id inexistente" (Guid válido, inexistente), que responde com o envelope. Sem novo validator: `GetMerchantByIdRequest` não tem campos validáveis além do Guid já vinculado pela rota (adição de um `GetMerchantByIdRequestValidator` seria especulativa; renunciado conscientemente quanto à letra de "request/response plus validator").
