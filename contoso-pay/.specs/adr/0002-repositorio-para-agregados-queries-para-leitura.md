---
Status: accepted
Date: 2026-10-22
---

# 0002: Repositório para agregados, Queries para o lado de leitura

O `IMerchantRepository` acumulava três responsabilidades: criar o agregado, perguntar sobre a coleção de agregados e servir de read model para a listagem (devolvendo `Merchant` para o use case mapear para o response do slice). Separamos o acesso a dados em duas portas com critério de classificação explícito:

- **Repositório** (`IMerchantRepository`) — a coleção de agregados: cria, obtém por identidade e responde perguntas sobre a própria coleção (existência por chave natural). Devolve agregados.
- **Queries** (`IMerchantQueries`) — o lado de leitura: devolve a projeção do slice (o `Response` do próprio use case), nunca o agregado.

Critério de classificação: **pergunta sobre a coleção de agregados** → repositório; **leitura para um leitor, expressa como projeção** → query. `GetByIdAsync` permanece no repositório porque devolve o agregado; a listagem filtrada sai do repositório e passa a ser `IMerchantQueries.SearchAsync`, devolvendo `PagedResponse<ListMerchantsResponse>`.

A unicidade de `document` é **restrição de coleção**, não invariante do agregado: um `Merchant` não conhece os outros `Merchant`, então não pode garantir a unicidade sozinho. Ela fica no repositório (`ExistsByDocumentAsync`), que é a abstração da coleção, com o pre-check como caminho primário (fail-fast explícito, sem depender de exceção para um cenário de negócio esperado, conforme a doutrina de Results), o índice único do Postgres como garantia e o catch de `UniqueViolation` como rede contra a corrida (TOCTOU).

## Considered Options

- **Manter as buscas filtradas no repositório (YAGNI)**: rejeitado — a listagem devolvia a entidade de domínio e o use case mapeava para o response do slice; o read side vazava o agregado e não havia separação real entre escrita e leitura.
- **Queries devolvendo read model do agregado (`MerchantSummary`), com o use case mapeando para o response do slice**: rejeitado — cria um segundo tipo para a mesma informação e mantém o mapeamento no use case; o ganho de projeção não compensava neste estágio.
- **Port de leitura por slice (`IListMerchantsQuery` em `Core/UseCases/Merchants/ListMerchants/`)**: rejeitado — optou-se por manter o port no nível do agregado (`Core/UseCases/Merchants/`), aceitando que ele acumule um método por slice, cada um devolvendo o response do slice correspondente.
- **Port de leitura por agregado devolvendo read model**: rejeitado pelo mesmo motivo da segunda opção, com o agravante de exigir um novo tipo de projeção.
- **Base genérica `IQueries<TReadModel, TFilter>` desde já**: rejeitado — com um único agregado e três slices futuros, seria especulação; a duplicação que realmente dói (contrato de paginação) já está centralizada na ADR-0001.
- **Assinatura da query com parâmetros primitivos (`document`, `namePrefix`, `page`, `pageSize`)**: rejeitado na revisão da assinatura — dois `int` adjacentes (`page`, `pageSize`) e dois `string?` adjacentes formam uma assinatura fácil de trocar por engano (uma troca silenciosa entre `page` e `pageSize` produz paginação errada sem erro de compilação), e a porta passa a enumerar os filtros, o que faria cada filtro novo alterar a assinatura da porta. A query recebe a request do slice e devolve o response do mesmo slice.
- **`IUnitOfWork` para retirar o commit de dentro do repositório**: rejeitado neste momento (fora de escopo) — `AddAsync` continua commitando; a fronteira de transação é assunto para uma decisão futura.

## Consequences

- `IMerchantRepository` ficou com `AddAsync`, `ExistsByDocumentAsync` e `GetByIdAsync`; `SearchAsync` saiu. `GetByDocumentAsync` não foi criado: nenhum consumidor precisa dele (o filtro exato por documento da listagem já cobre a busca por documento).
- `IMerchantQueries` é organizado por agregado, mas cada método devolve o response de um slice — o port de agregado depende do contrato de um slice. Consequência aceita e registrada aqui porque um leitor futuro vai notar que o port de queries não é genérico nem "do slice".
- A projeção passou a ser feita no adapter (`Adapters.Persistence/Queries/MerchantQueries.cs`), direto para o record do slice: a listagem não materializa mais `Merchant`.
- `IMerchantQueries.SearchAsync` recebe a `ListMerchantsRequest` do slice e devolve o `ListMerchantsResponse` do mesmo slice: entrada e saída são os dois contratos do slice. A request chega crua, então o use case entrega à query uma **cópia normalizada** (`request with { ... }` aplicando o trim dos filtros) — o trim continua sendo responsabilidade do use case, conforme a decisão do ticket 03, e o adapter não normaliza nada.
- O `GetById` continua lendo o agregado pelo repositório. Se, no futuro, **todas** as leituras migrarem para o lado de leitura, o repositório degenera em porta de escrita e esta ADR precisa de revisão explícita.
- Os slices `appclients` e `sales`, já especificados, têm agregados com restrições de unicidade composta (`merchant` + `merchant order id`): a mesma distinção entre restrição de coleção e invariante do agregado se aplica lá.
- Sem DDL delta e sem package novo. A suíte permanece com 31 testes.
