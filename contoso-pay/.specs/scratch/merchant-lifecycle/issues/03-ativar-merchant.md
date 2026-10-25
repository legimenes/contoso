# 03: Ativar merchant

**What to build:** um operador reativa um merchant que estava inativo, e a leitura volta a mostrá-lo ativo. `POST /v1/merchants/{id}/activate` responde `204` sem corpo para id existente e `404` no envelope padrão para id desconhecido. Chamar de novo sobre um merchant já ativo continua respondendo `204`.

**Blocked by:** 02: Desativar merchant.

**Status:** resolved

- [x] `POST /v1/merchants/{id}/activate` retorna `204` sem corpo e persiste o estado ativo
- [x] Id desconhecido retorna `404` no envelope padrão, sem efeito persistido
- [x] Chamada repetida sobre um merchant já ativo retorna `204` e não é erro
- [x] O efeito é verificável pela leitura: um merchant previamente desativado volta a aparecer ativo no `GET` por id
- [x] O slice de ativação reutiliza o contrato de sucesso-sem-corpo e o mapper introduzidos na desativação, sem duplicar o caminho de erro
- [x] O slice é registrado na composição da aplicação e aparece na referência Scalar versionada, com exemplo executável em `API.http`
- [x] Testes unitários: sucesso devolve sucesso-sem-corpo e commita uma vez; id desconhecido devolve not found e não commita
- [x] Teste de integração contra Postgres real: desativar, ativar, `204` com corpo vazio em ambas, estado ativo visível no `GET` ao final, e `404` no envelope para id desconhecido
- [x] Endpoint público nesta fase, com a mesma dívida explícita dos demais: Bearer entra quando o slice de connect existir, sem mudar o contrato

## Comments

- Ativação é a transição reversa: reativar não ressuscita app clients desativados individualmente (ADR-0003).
- A ordem inversa (ativar antes de desativar) não foi escolhida porque desativar é a necessidade primária — bloquear a operação de um merchant — e é quem inaugura o contrato `204`.
- DDL delta: nenhum. `is_active BOOLEAN NOT NULL` entrou no 01 e a porta `IMerchantRepository.UpdateAsync` entrou no 02; nenhum volume foi tocado nesta fatia.
- Implementação: slice `Core/UseCases/Merchants/ActivateMerchant/` com `ActivateMerchantRequest(Guid Id)`, `IActivateMerchant` e `ActivateMerchant` (lê o agregado, chama `Activate()`, commita e devolve `Result.Success(ResultType.NoContent)`); sem validator, sem modelo de resposta. Registro em `AddCore`; endpoint v1.0 público com `Produces(204)`/`Produces<ApiProblemDetails>(404)` e exemplo em `API.http` usando `{{createMerchant.response.body.$.id}}`.
- Testes: 2 de unidade (sucesso devolve `NoContent`, ativa o agregado e commita uma vez; id desconhecido devolve `NotFound` com `Validation_Merchant_NotFound` e não commita) e 3 de integração contra Postgres real (criar → desativar `204` vazio → ativar `204` vazio → `GET` ativo; ativação repetida `204` vazio; 404 no envelope). Suite total 46/46 verde, build 0 warnings.
- OpenAPI v1 conferido com a API em execução: `/v1/merchants/{id}/activate` presente com `204` e `404` (`ApiProblemDetails`).
