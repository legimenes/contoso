# 01: Fundação de identidade com OpenIddict e emissão de token

**What to build:** a API passa a ter o adapter `Adapters.Identity` com OpenIddict, o modelo de duas tabelas (`app_clients`, `app_client_tokens`) no schema `contosopay` e o endpoint `POST /connect/token` (somente `client_credentials`, `application/x-www-form-urlencoded`) que emite um JWT HS256 de uma hora com `sub`, `merchant_id` e audience `contoso-pay-api`. O token só é emitido para app client cujo merchant existe e está ativo. Esta fatia entrega a credencial, a persistência e a emissão; os endpoints públicos de criação, leitura e rotação ficam no ticket 02.

**Blocked by:** None (can start immediately).

**Status:** resolved

- [x] O projeto `Adapters.Identity` existe, referencia `Core` e concentra OpenIddict, `IdentityDbContext`, entidade de persistência, descriptor/manager customizados e as implementações das portas; `Core` não referencia tipos do OpenIddict
- [x] O `IdentityDbContext` usa o schema `contosopay`, a connection string `Postgres` e mapeia apenas `app_clients` e `app_client_tokens`; token storage ligado e token entry validation desligado
- [x] `docker/scripts/postgresql/01-objects.sql` ganha as duas tabelas em snake_case, com índices únicos de `client_id`, `merchant_id` e `reference_id`, FK de `merchant_id` para `merchants` somente no DDL e `authorization_id` nulo sem FK; o delta para volumes existentes fica documentado no arquivo, sem recriar volume
- [x] A criação de credencial gera `client_id` como `Guid.NewGuid().ToString("N")` (32 caracteres) e secret de 32 bytes em Base64URL (43 caracteres); o OpenIddict armazena apenas o hash PBKDF2 e o secret em texto puro só é devolvido pela operação que o gerou
- [x] O modelo do OpenIddict está configurado para somente `AllowClientCredentialsFlow()`, rota `/connect/token`, chave HS256 vinda de configuração, access token de 1 hora, audience `contoso-pay-api`, issuer `https://contoso-pay/`, criptografia de access token desligada e `UseLocalServer` na validação
- [x] `POST /connect/token` aceita apenas formulário URL-encoded, valida `grant_type`, `client_id` e `client_secret`, devolve `200 {access_token, token_type, expires_in}` e erros OAuth (`401 invalid_client`, `400 invalid_request`, `400 unsupported_grant_type`)
- [x] A porta `IExchangeToken` no Core devolve `Result<ClaimsPrincipal>`; a implementação em `Adapters.Identity` valida o secret com OpenIddict, exige merchant existente e ativo via `IMerchantRepository` e monta o principal com `sub` = `client_id` e `merchant_id` = merchant dono
- [x] `ResultType.Unauthorized` existe e o `EndpointBase` mapeia para `401` no envelope de problema; o endpoint de token usa o formato OAuth, não o envelope
- [x] A rota `/connect/token` pertence ao grupo de documentação da versão 1, mas não carrega prefixo de versão na URL
- [x] Testes de integração comprovam emissão `200`, claims `sub`/`merchant_id`, `expires_in` 3600, `401` para client desconhecido/secret errado/merchant inativo, `400` para grant inválido, e que o token emitido é aceito pelo handler de validação; o app client é semeado pelo adapter quando o endpoint público ainda não existe e a aceitação em endpoint protegido é fechada no ticket 02
- [x] Testes usam chave de assinatura determinística e relógio controlável; a suíte existente continua verde e o build fica sem warnings
- [x] ADR nova registra OpenIddict, o vínculo 1:1 e a rotação; ADR-0003 é emendada para retirar o `is_active` do app client; o glossário reflete 1:1 e os termos novos

## Comments

- Implementado: `Adapters.Identity` com OpenIddict 7.7.2, `IdentityDbContext`, quatro entidades EF (application, authorization, scope e token), duas tabelas no DDL, porta `IAppClientCredentials`, `IExchangeToken`, `POST /connect/token` e validação local do Bearer. Verificado: build 0 warnings, 61/61 testes verdes (5 unitários de `ExchangeToken` e 10 de integração HTTP com Postgres real).
- Adaptações em relação ao plano aprovado, impostas pela API do OpenIddict 7.7.2: (1) o modelo EF tem quatro entidades, não seis, então `AppClientResource`/`AppClientSession` não existem; (2) `app_client_tokens` não tem `session_id`; (3) as propriedades de lista do application são `TEXT` com JSON serializado, não `TEXT[]`; (4) não existe `CreateAsync(descriptor, secret)` — o secret vai no descriptor e o manager faz o hash; (5) a validação usa `AddAudiences(...)` e o servidor `RegisterAudiences(...)`; (6) o issuer precisa ser URI absoluta e é normalizado para `https://contoso-pay/`; (7) o OpenIddict 7 exige chave assimétrica de assinatura e chave de criptografia, registradas como efêmeras, mantendo a chave simétrica de configuração como preferida para os access tokens (HS256).
- A rota `/connect/token` usa o grupo versionado de v1 sem prefixo de URL. Para que requisições sem segmento de versão resolvam para v1, `DefaultApiVersion` passou a ser `1.0`; os endpoints versionados continuam resolvendo pela URL (`/v1`, `/v2`). A documentação OpenAPI publica `/connect/token` apenas em `v1.json`.
- O teste de aceitação do Bearer usa um probe HTTP somente de teste (`/__auth-probe`, via `IStartupFilter`) porque o endpoint protegido de app client só chega no ticket 02; o probe exercita o handler de validação do OpenIddict com token válido, adulterado e expirado. Relógio controlado por `TestTimeProvider` e chave de assinatura determinística em `appsettings.Testing.json`.
- Erros do token endpoint usam `OAuthErrorResponse` (`error`, `error_description`) no OpenAPI, não o envelope `ApiProblemDetails`.
- Ajustes após revisão: o endpoint passou a exigir literalmente `application/x-www-form-urlencoded` (rejeitando `multipart/form-data`), o `grant_type` é validado antes dos campos de credencial, e a integração agora verifica `alg=HS256`, a persistência do token em `app_client_tokens` e a rejeição de corpo JSON.
- Reorganização solicitada: a porta `IExchangeToken` foi movida para `Core/Abstractions/Identity/` (namespace `Core.Abstractions.Identity`), e `Core/UseCases/AppClients/` inteiro (`IAppClientCredentials`, `AppClientCredential`, `CreatedAppClientCredential`) foi movido para `Adapters.Identity/AppClients/` (namespace `Adapters.Identity.AppClients`); o Core não referenciava esses tipos, então nenhum uso no Core precisou mudar. Os testes ficam em `Tests/Core/Abstractions/Identity/`.

## Technical plan (approved)

### DDL delta

Adicionar em `docker/scripts/postgresql/01-objects.sql`, depois de `merchants`, sem alterar `merchants`:

```sql
CREATE TABLE contosopay.app_clients (
    id VARCHAR(100) PRIMARY KEY,
    application_type VARCHAR(50) NULL,
    client_id VARCHAR(100) NOT NULL,
    client_secret TEXT NULL,
    client_type VARCHAR(50) NULL,
    concurrency_token VARCHAR(50) NOT NULL,
    consent_type VARCHAR(50) NULL,
    display_name VARCHAR(100) NULL,
    display_names TEXT NULL,
    json_web_key_set TEXT NULL,
    permissions TEXT NULL,
    post_logout_redirect_uris TEXT NULL,
    properties TEXT NULL,
    redirect_uris TEXT NULL,
    requirements TEXT NULL,
    settings TEXT NULL,
    merchant_id UUID NOT NULL,
    CONSTRAINT fk_app_clients_merchants FOREIGN KEY (merchant_id)
        REFERENCES contosopay.merchants (id)
);

CREATE UNIQUE INDEX ux_app_clients_client_id ON contosopay.app_clients (client_id);
CREATE UNIQUE INDEX ux_app_clients_merchant_id ON contosopay.app_clients (merchant_id);

CREATE TABLE contosopay.app_client_tokens (
    id VARCHAR(100) PRIMARY KEY,
    application_id VARCHAR(100) NULL,
    authorization_id VARCHAR(100) NULL,
    concurrency_token VARCHAR(50) NOT NULL,
    creation_date TIMESTAMPTZ NULL,
    expiration_date TIMESTAMPTZ NULL,
    payload TEXT NULL,
    properties TEXT NULL,
    redemption_date TIMESTAMPTZ NULL,
    reference_id VARCHAR(100) NULL,
    status VARCHAR(50) NULL,
    subject VARCHAR(400) NULL,
    type VARCHAR(150) NULL,
    CONSTRAINT fk_app_client_tokens_app_clients FOREIGN KEY (application_id)
        REFERENCES contosopay.app_clients (id)
);

CREATE UNIQUE INDEX ux_app_client_tokens_reference_id ON contosopay.app_client_tokens (reference_id);
CREATE INDEX ix_app_client_tokens_application_status_subject_type
    ON contosopay.app_client_tokens (application_id, status, subject, type);

GRANT ALL PRIVILEGES ON TABLE contosopay.app_clients TO appuser;
GRANT ALL PRIVILEGES ON TABLE contosopay.app_client_tokens TO appuser;
```

O delta para volume existente é o mesmo bloco, executado via `psql`; nenhum volume é recriado. A coluna `authorization_id` existe porque o modelo EF do OpenIddict a inclui no insert, mas fica sempre nula e não tem FK porque a tabela de authorization não existe. O modelo EF registra as quatro entidades do pacote (application, authorization, scope e token); apenas `app_clients` e `app_client_tokens` existem no DDL. As propriedades de lista do application (`permissions`, URIs de redirecionamento e `requirements`) são `TEXT` com JSON serializado no OpenIddict 7.7.x.

### Classes, campos e tamanhos

- `Adapters.Identity/Models/AppClient.cs`: `sealed class AppClient : OpenIddictEntityFrameworkCoreApplication` com `Guid MerchantId`; o nome exibido usa `DisplayName` (máx. 100), sem `is_active` e sem `created_at`. Os modelos `AppClientAuthorization`, `AppClientScope` e `AppClientToken` completam as quatro entidades do pacote.
- `Adapters.Identity/AppClientDescriptor.cs`: `sealed class AppClientDescriptor : OpenIddictApplicationDescriptor` com `Guid MerchantId`.
- `Adapters.Identity/AppClientManager.cs`: herda `OpenIddictApplicationManager<AppClient>` e sobrescreve `PopulateAsync` para copiar `MerchantId` do descriptor para a entidade.
- `Adapters.Identity/IdentityDbContext.cs`: `DbContext` com `HasDefaultSchema("contosopay")`, `UseOpenIddict<AppClient, ...>()` via opções do `AddDbContext` e `ApplyConfigurationsFromAssembly` para os mapeamentos snake_case.
- `Adapters.Identity/Configurations/AppClientConfiguration.cs` e `AppClientTokenConfiguration.cs`: sobrescrevem `ToTable`, nomes de coluna, `display_name` com 100, `merchant_id` obrigatório e único.
- `Core/Abstractions/Identity/IExchangeToken.cs`: `Task<Result<ClaimsPrincipal>> ExchangeAsync(string clientId, string clientSecret, CancellationToken cancellationToken)`.
- `Adapters.Identity/AppClients/IAppClientCredentials.cs` (junto com os records `AppClientCredential` e `CreatedAppClientCredential`): porta do adapter para `ExistsByMerchantIdAsync`, `CreateAsync`, `FindByClientIdAsync`, `ValidateSecretAsync` e `RotateSecretAsync`; retorna apenas strings/Guid, nunca tipos do OpenIddict.
- `Adapters.Identity/Repositories/AppClientRepository.cs`: implementa a porta acima; gera client id e secret, preenche `descriptor.ClientSecret` e usa `AppClientManager.CreateAsync(descriptor)` (o manager faz o hash) e `UpdateAsync(appClient, newSecret)`; mapeia unique violation de `merchant_id` para `DuplicateAppClientException`.
- `Domain/Core/Exceptions/DuplicateAppClientException.cs`: exceção esperada lançada pelo adapter quando o índice único de `merchant_id` dispara; o use case de criação a converte em `409`.
- `Adapters.Identity/ExchangeToken.cs`: implementa `IExchangeToken`; reutiliza `IAppClientCredentials` para localizar o client e validar o secret, exige merchant existente e ativo via `IMerchantRepository` e devolve `ResultType.Unauthorized` para qualquer falha de credencial.
- `API/Endpoints/Connect/ExchangeTokenEndpoint.cs`: lê o formulário, valida o grant type, chama `IExchangeToken` e devolve `Results.SignIn` no scheme do OpenIddict server; erros em JSON OAuth.
- `API/Endpoints/IUnversionedEndpoint.cs`, `RouteGroup.MapUnversionedGroup()` e ajuste em `MapEndpoints`: rota sem prefixo, metadata de v1 para documentação.
- `Adapters.Identity/ServiceCollectionExtensions.cs`: `AddIdentity(IConfiguration)` registra `IdentityDbContext`, OpenIddict (core/server/validation), `AppClientManager`, as portas, a autenticação e a autorização; falha no startup se `Identity:SigningKey` faltar ou for curta. Além da chave simétrica de configuração, registra chaves assimétrica de assinatura e de criptografia efêmeras, exigidas pelo OpenIddict 7 para artefatos internos.
- `API/API.csproj` referencia `Adapters.Identity`; `Program.cs` chama `AddIdentity(builder.Configuration)` e adiciona `UseAuthentication()`/`UseAuthorization()` antes do mapeamento dos endpoints. `backend/ContosoPay.slnx` ganha o projeto `src/Adapters.Identity/Adapters.Identity.csproj`.
- `Domain/Core/Results/ResultType.cs`: `Unauthorized = 401`; `API/Endpoints/EndpointBase.cs`: mapeamento para `401` com ProblemDetails.
- `Domain/Resources/Resource.resx`: mensagens de erro do token (`invalid_client`, `invalid_request`, `unsupported_grant_type` descriptions) e as mensagens de validação/negócio desta fatia; códigos OAuth são constantes de protocolo e não texto de usuário.
- `Directory.Packages.props`: versões centrais de OpenIddict.EntityFrameworkCore, OpenIddict.Server.AspNetCore, OpenIddict.Validation.AspNetCore e OpenIddict.Validation.ServerIntegration (7.7.x).

### Configuração

- `Identity:SigningKey`: Base64 de pelo menos 32 bytes, obrigatória; falhar no startup se ausente ou curta. Valor de desenvolvimento/teste só em `appsettings.Development.json`/`appsettings.Testing.json`; produção via variável de ambiente ou user secrets.
- `Identity:Audience` = `contoso-pay-api` e `Identity:Issuer` = `https://contoso-pay/` (constantes ou configuração com esses defaults).
- `ConnectionStrings:Postgres` reaproveitada pelo `IdentityDbContext`; nenhuma connection string nova.

### Libs e seams

- Libs: OpenIddict 7.7.x (`EntityFrameworkCore`, `Server.AspNetCore`, `Validation.AspNetCore`, `Validation.ServerIntegration`), EF Core 10 + Npgsql provider já existentes, `Microsoft.IdentityModel.Tokens` (transitivo) para Base64URL/chave simétrica. O projeto `Adapters.Identity` referencia os pacotes OpenIddict, `Npgsql.EntityFrameworkCore.PostgreSQL` e `Core`; `API` referencia `Adapters.Identity`.
- Seams: endpoint HTTP (primário, integração com Postgres real) → `IExchangeToken` (porta Core) → `AppClientManager`/`IMerchantRepository` (implementação no adapter). A porta de credencial é implementação de adapter exercitada pelo endpoint; não há teste de store/repositório isolado.
- O endpoint de token é testável semeando um app client via `IAppClientCredentials`/`AppClientManager` no host de teste, já que a criação pública só chega no ticket 02. O teste de aceitação do Bearer em endpoint protegido usa o `GET /v1/appclients` do ticket 02; nesta fatia o teste verifica a emissão e a validação do token pelo handler.
