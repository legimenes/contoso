# 02: Desativar merchant

**What to build:** um operador desativa um merchant existente e passa a vê-lo inativo na leitura, sem que o registro perca identidade, nome, documento ou data de criação. `POST /v1/merchants/{id}/deactivate` responde `204` sem corpo para id existente e `404` no envelope padrão para id desconhecido. Chamar de novo continua respondendo `204`.

**Blocked by:** 01: Estado de ativação do merchant e exposição na leitura.

**Status:** resolved

- [x] `POST /v1/merchants/{id}/deactivate` retorna `204` sem corpo e persiste o estado inativo
- [x] Id desconhecido retorna `404` no envelope padrão, sem efeito persistido
- [x] Chamada repetida sobre um merchant já inativo retorna `204` e não é erro
- [x] O efeito é verificável pela leitura: o `GET` por id passa a devolver o merchant inativo, com nome, documento e data de criação intactos
- [x] O tipo de resultado ganha o valor de sucesso sem corpo, e o mapper de resultado para HTTP ganha o caminho de sucesso sem valor — hoje todo sucesso carrega corpo e um tipo não mapeado degrada para erro interno
- [x] A porta de escrita do agregado é explícita: existe uma operação de atualização que commita o agregado, sem depender de a entidade ter sido carregada rastreada por uma leitura anterior
- [x] Nenhum unit of work é introduzido; o repositório continua dono da fronteira de transação
- [x] O slice de desativação é registrado na composição da aplicação e aparece na referência Scalar versionada, com exemplo executável em `API.http`
- [x] Testes unitários: sucesso devolve sucesso-sem-corpo e commita uma vez; id desconhecido devolve not found e não commita
- [x] Teste de integração contra Postgres real: `204` com corpo vazio, estado visível no `GET` depois da chamada, repetição `204`, e `404` no envelope para id desconhecido
- [x] Endpoint público nesta fase, com a mesma dívida explícita dos demais: Bearer entra quando o slice de connect existir, sem mudar o contrato

## Comments

- Este é o primeiro sucesso sem corpo da API e o primeiro slice sem modelo de resposta. Não é omissão de arquivo: o corpo seria determinado pela rota (`204` deactivate sempre produz inativo), então não carregaria informação.
- Decisão de concorrência: last-write-wins, sem token de concorrência e sem lock de linha. Transições opostas simultâneas são uma corrida cujo vencedor é indefinido por natureza.
- A emissão de token passa a exigir merchant ativo, mas isso é do slice de connect (ADR-0003); nada é aplicado aqui.
- O ticket `merchants/05-formalizar-agregado-merchant` cita o caminho de atualização como item de formalização do agregado. Esta fatia introduz esse caminho; o 05 permanece aberto para os demais itens.
- DDL delta: nenhum. `is_active BOOLEAN NOT NULL` já entrou no 01; nenhum volume foi tocado nesta fatia.
- Implementação: `ResultType.NoContent = 204`; `EndpointBase` ganhou a sobrecarga não genérica `CreateResult(Result)` e as duas sobrecargas passaram a delegar os tipos de falha para `CreateFailureResult`, mantendo o degrade para 500 do tipo não mapeado e sem duplicar o caminho de erro. Slice `Core/UseCases/Merchants/DeactivateMerchant/` com `DeactivateMerchantRequest(Guid Id)`, `IDeactivateMerchant` e `DeactivateMerchant` (lê o agregado, chama `Deactivate()`, commita e devolve `Result.Success(ResultType.NoContent)`); sem validator, sem modelo de resposta. Porta `IMerchantRepository.UpdateAsync` implementada com `Set<Merchant>().Update(...)` + `SaveChangesAsync`, então o commit não depende de rastreamento prévio. Registro em `AddCore`, endpoint v1.0 público com `Produces(204)`/`Produces<ApiProblemDetails>(404)` e exemplo em `API.http`.
- Testes: 2 de unidade (sucesso devolve `NoContent` e commita uma vez; id desconhecido devolve `NotFound` com `Validation_Merchant_NotFound` e não commita) e 3 de integração contra Postgres real (204 com corpo vazio + `GET` inativo com nome/documento/`created_at` intactos, repetição 204, 404 no envelope). Suite total 41/41 verde, build 0 warnings. Scalar/OpenAPI v1 conferido manualmente: `/v1/merchants/{id}/deactivate` com 204 e 404.
- `API.http`: o `POST /v1/merchants` virou a request nomeada `createMerchant` e o exemplo de desativação usa `{{createMerchant.response.body.$.id}}`, então o caminho 204 é executável de fato; um GUID fixo qualquer só produziria 404.
- Awaits novos seguem sem `.ConfigureAwait(false)`, conforme `coding-patterns.md`; os arquivos vizinhos anteriores ainda carregam a chamada e não foram tocados.
- O teste de integração lê `created_at` direto no Postgres porque o `GET` por id não expõe a data de criação: o contrato de leitura não foi alterado nesta fatia.
