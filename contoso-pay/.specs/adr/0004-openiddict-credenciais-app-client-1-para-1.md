---
Status: accepted
Date: 2026-10-27
---

# 0004: OpenIddict como provedor de credenciais e app client 1:1

`AppClient` deixa de ser um agregado de domínio com `is_active` e `created_at` e passa a ser uma credencial OAuth 2.0 persistida pelo OpenIddict no adapter `Adapters.Identity`. Cada merchant tem no máximo um app client (1:1), identificado por um `client_id` opaco (GUID no formato "N", 32 caracteres) e por um secret de 32 bytes em Base64URL. O secret é exibido uma única vez, na criação ou na rotação, e o armazenamento guarda apenas o hash PBKDF2 produzido pelo OpenIddict. A criação é pública (`POST /v1/appclients`); leitura e rotação exigem Bearer.

O fluxo é somente `client_credentials`, em `/connect/token` (rota sem versão, formulário URL-encoded), com token HS256 de uma hora, audience `contoso-pay-api`, claims `sub` (client_id) e `merchant_id`. O issuer canônico é `https://contoso-pay/`; como o OpenIddict normaliza o valor para uma URI absoluta, a barra final é a forma emitida. O merchant precisa existir e estar ativo **na emissão**; um token já emitido vale até expirar porque a validação de token não consulta o banco (`EnableTokenEntryValidation` desligado). A rotação é a alavanca de desligamento: não há endpoint de desativação de app client, e rotacionar o secret invalida o anterior imediatamente. O `client_id` é imutável.

O OpenIddict 7 exige ao menos uma chave assimétrica registrada no servidor. A chave simétrica de configuração assina os access tokens (HS256) e é a preferida pelo pacote; as chaves assimétrica de assinatura e de criptografia são efêmeras e cobrem apenas artefatos internos que este fluxo não emite (identity tokens e payloads de authorization).

O banco tem duas tabelas — `app_clients` e `app_client_tokens` — no schema `contosopay`. O modelo EF do OpenIddict registra as quatro entidades do pacote (`application`, `authorization`, `scope` e `token`), mas apenas application (`app_clients`) e token (`app_client_tokens`) existem no DDL; authorization e scope não são usadas pelo fluxo e não têm tabela. `app_client_tokens` mantém `authorization_id` nulo, sem FK, porque o modelo inclui essa coluna no insert. As propriedades de lista do application (`permissions`, URIs de redirecionamento e `requirements`) são `TEXT` com JSON serializado. O `merchant_id` é único em `app_clients` e tem FK para `merchants` apenas no DDL, não no modelo EF.

## Considered Options

- **Manter AppClient como agregado com `is_active` e `created_at`**: rejeitado — duplica o estado do merchant e cria duas causas de desativação; a rotação de secret já é a alavanca de desligamento.
- **Permitir N app clients por merchant**: rejeitado — o produto é de estudo e a 1:1 reduz o modelo; múltiplos app clients ficam para uma iniciativa futura.
- **Token opaco com introspecção por requisição**: rejeitado — JWT local com `merchant_id` evita um lookup por chamada e mantém o custo baixo.
- **Endpoint de desativação de app client**: rejeitado — rotação cobre o caso; não há `is_active` para manter.
- **Criação autenticada**: adiada — não há identidade de merchant antes do primeiro app client; o risco de criação pública é aceito e documentado.

## Consequences

- A criação pública permite que qualquer um crie a credencial de um merchant que ainda não tem app client; o merchant fica impedido de criar outra (409) e, sem o secret atual, não consegue rotacionar. A recuperação exigiria intervenção de operador/banco. Risco aceito no contexto de estudo.
- O token emitido continua válido por até uma hora após a desativação do merchant ou a rotação do secret, porque a validação não consulta o banco. Revogação imediata exigiria token entry validation ou lista de revogação.
- O modelo EF do OpenIddict registra quatro entidades, mas apenas application e token têm tabela; operações que consultem authorization/scope (por exemplo, excluir um app client pelo manager) não são suportadas.
- O DDL é a fonte da verdade; o delta para volumes existentes é documentado no próprio `01-objects.sql`.
- Esta ADR emenda a ADR-0003: o app client não tem mais `is_active` próprio, e o único gate de credencial é `merchant.is_active` na emissão.
