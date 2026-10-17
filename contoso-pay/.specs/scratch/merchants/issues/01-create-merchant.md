# 01: Criar merchant

**What to build:** um integrador cria um merchant informando nome e documento e recebe apenas o identificador gerado, com duplicidade de documento rejeitada.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] `POST /v1/merchants` público com `{name, document}` válido retorna `201 {id}`
- [ ] Nome ou documento ausente/vazio retorna `400` no envelope padrão de problema
- [ ] Documento já cadastrado retorna `409` no envelope padrão de problema
- [ ] Resposta de criação expõe somente o id, sem ecoar nome/documento
- [ ] Endpoint aparece na referência Scalar versionada com exemplo executável

## Comments

- Implementado no commit 017fbb7 (`feat/merchant-crud`): `POST /v1/merchants` com slice vertical completo (Domain `Merchant`, Core `CreateMerchant` + `IMerchantRepository`, adapter Npgsql, endpoint Minimal API). Verificado: build 0 warnings, 9/9 testes verdes (unidade + integração HTTP com Postgres real), E2E manual 201/400/409 + Scalar/OpenAPI.
- Retrabalho do projeto de testes conforme `.specs/docs/automated-tests.md`: builders (`CreateMerchantRequestBuilder`, `MerchantBuilder`), NSubstitute substituindo o fake manual, Given–When–Then, asserts de envelope de problema (400/409) e coberturas extras de validação (nome/documento sobretamanho, documento ausente). String de conexão dos testes resolvida via `WebApplicationFactory` → `IConfiguration.GetConnectionString("Postgres")` (`appsettings.Development.json` da API), sem hardcode. Padrão de trait revisado: um único `[Trait("Merchant", "CreateMerchant (Unit)/(Integration)")]` com a categoria em parênteses (sem o segundo trait `Category`), cobrindo também `[Domain]`/`[Services]`; testes de repositório não rodam mais de forma avulsa — o round-trip passa pelos testes de endpoint (duplo `POST` → 409). `automated-tests.md` atualizado para refletir esses dois pontos (nomes `Given/When/Should` + parenthesized trait). Verificado: build 0 warnings, 15/15 testes verdes (Postgres real via compose).
- Regra definida em `automated-tests.md`: a matriz de validação é da **unidade**; na integração, no máximo **um teste de invalidação por endpoint**, só para provar que a invalidação dispara e produz a resposta HTTP esperada. `CreateMerchantEndpointTests` ajustado em consequência: um único caso de invalidação (documento vazio → 400 + envelope) e o 409 da duplicata com envelope; os casos de nome/documento em branco e sobretamanho saíram da integração, mantidos na unidade. Suíte: 9 testes unitários + 3 de integração = 12/12 verdes, build 0 warnings.
- Decisão de cobertura (aprovada): o critério "Resposta de criação expõe somente o id" passa a ser verificado apenas positivamente na integração (id presente e não-vazio, sem asserts negativos de `name`/`document` no corpo). As asserções negativas foram deliberadamente removidas; não restaurar sem nova decisão.

## Technical plan (approved)

- DDL: `contosopay.merchants (id UUID PK, name VARCHAR(100), document VARCHAR(14) UNIQUE, created_at TIMESTAMPTZ)` — CPF/CNPJ crus, só dígitos.
- Classes: `Domain.Merchant`, `CreateMerchantRequest`, `CreateMerchantRequestValidator` (FluentValidation), `CreateMerchant.ExecuteAsync → Result<Guid>`, porta `IMerchantRepository`, adapter EF (`ContosoPayDbContext` + `MerchantConfiguration`), `POST /v1/merchants → 201 {id}`.
- Libs: EF Core 10 + Npgsql provider, FluentValidation 12. Seams: endpoint → use-case → porta. 9 testes verdes.
