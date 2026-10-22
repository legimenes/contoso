# 04: Separar o acesso de leitura (Queries) do acesso a agregados (Repository)

**What to build:** dividir o acesso a dados em duas portas com critério de classificação explícito — `IMerchantRepository` (a coleção de agregados) e `IMerchantQueries` (o lado de leitura) — movendo a listagem paginada do repositório para a query e fazendo o read side devolver a projeção do slice em vez da entidade de domínio.

**Blocked by:** None (01, 02 e 03 estão `resolved`).

**Status:** resolved

- [x] `IMerchantRepository` mantém `AddAsync`, `ExistsByDocumentAsync` e `GetByIdAsync`; `SearchAsync` sai da porta
- [x] `IMerchantQueries` criada em `Core/UseCases/Merchants/`, devolvendo `PagedResponse<ListMerchantsResponse>`
- [x] Adapter relacional `MerchantQueries` em `Adapters.Persistence/Queries/`, com a semântica de filtro, ordenação e paginação preservada (ILIKE escapado, `LOWER(name)` + `id`, count + `Skip/Take`)
- [x] A projeção acontece no adapter, direto para o record do slice — a listagem não materializa mais `Merchant`
- [x] `ListMerchants` passa a depender da query e repassa o envelope; o mapeamento `Merchant` → response sai do use case
- [x] `GetById` continua lendo o agregado pelo repositório
- [x] Sem DDL delta (`01-objects.sql` intacto) e sem package novo
- [x] Unit tests do `List` migrados para a nova porta; integração do endpoint sem mudança de contrato
- [x] ADR-0002 registrada e `coding-patterns.md` atualizado com o critério de classificação
- [x] `GLOSSARY.md` criado com os termos de domínio firmados (Merchant, Document)

## Comments

- Decidido em duas rodadas de grilling, com o código e a doutrina do repo como insumo. Critério fechado: **pergunta sobre a coleção de agregados** → repositório (devolve agregados); **leitura para um leitor, expressa como projeção** → query (devolve a projeção do slice, nunca o agregado).
- `ExistsByDocumentAsync` **fica** no repositório, mas pelo motivo correto: unicidade de `document` é **restrição de coleção**, não invariante do agregado (um `Merchant` não conhece os outros). A distinção está na ADR-0002 e na seção "Repositories and Queries" do `coding-patterns.md`. O pre-check segue como caminho primário (fail-fast, sem exceção para cenário de negócio esperado), o índice único como garantia, e o catch de `UniqueViolation` como rede contra o TOCTOU.
- `GetByDocumentAsync` **não** foi criado: nenhum consumidor precisa dele — o filtro exato por documento da listagem já cobre a user story 15.
- O `GetById` permanece no repositório porque devolve o agregado. Gatilho de revisão registrado na ADR-0002: se **todas** as leituras migrarem para o lado de leitura, o repositório degenera em porta de escrita e a ADR precisa ser revista.
- Consistente com a decisão anterior de `AddAsync` commitar dentro do repositório: a fronteira de transação (um `IUnitOfWork`) ficou fora de escopo e registrada para decisão futura — não é efeito colateral desta mudança.
- Consequência aceita e registrada: `IMerchantQueries` é organizado por agregado, mas cada método devolve o response de um slice, então o port de agregado depende do contrato de um slice. A alternativa (port por slice, `IListMerchantsQuery`) foi considerada e rejeitada nesta rodada.
- O trim dos filtros continua sendo responsabilidade única do use case (decisão do ticket 03): a query recebe a `ListMerchantsRequest` do slice e o use case entrega uma **cópia normalizada** (`request with { ... }`), então o adapter nunca normaliza nada. A assinatura com parâmetros primitivos foi rejeitada na revisão: dois `int` adjacentes (`page`, `pageSize`) e dois `string?` adjacentes são fáceis de trocar por engano, e a porta passaria a enumerar os filtros.
- `List<T>` não é usado nos arquivos novos: o namespace `Core.UseCases.Merchants.List` colide com `System.Collections.Generic.List<T>` em arquivos que importam ambos, então as coleções usam `IReadOnlyList<T>` (já o tipo de `PagedResponse<T>.Items`).
- Testes conforme `automated-tests.md`: unit 6/6 no seam de use case com NSubstitute na nova porta (`PagedResponseBuilder<ListMerchantsResponse>`; o teste de mapeamento de itens virou de pass-through do envelope, porque o item já chega pronto da query); integração via `WebApplicationFactory` com Postgres real 9/9 sem mudança de contrato; `CreateMerchant`/`GetById` sem alteração. Suite total 31/31 verde, build 0 warnings.
- Registro: ADR-0002 (`.specs/adr/0002-repositorio-para-agregados-queries-para-leitura.md`), `GLOSSARY.md` (criado), `backend/.specs/docs/coding-patterns.md` (nova subseção "Repositories and Queries").

## Technical plan (approved)

- DDL delta: nenhum (`docker/scripts/postgresql/01-objects.sql` não alterado).
- Classes: `IMerchantQueries` (nova, `Core/UseCases/Merchants/`, método `SearchAsync(ListMerchantsRequest request, CancellationToken)` devolvendo `Task<PagedResponse<ListMerchantsResponse>>`); `MerchantQueries` (nova, `Adapters.Persistence/Queries/`, adapter EF com `AsNoTracking`, ILIKE escapado, count + `Skip/Take` ordenados por `LOWER(name)` + `id`, projetando direto para `ListMerchantsResponse`); `IMerchantRepository` (porta reduzida, sem `SearchAsync`); `MerchantRepository` (adapter sem `SearchAsync` e sem o helper de escape, que migrou para o adapter de query); `ListMerchants` (use case passa a injetar `IMerchantQueries`, entrega à query uma cópia normalizada da request com o trim dos filtros e repassa o envelope, sem mapeamento).
- Libs: nenhum package novo (EF Core e Npgsql já presentes).
- Seams/ports: uma nova porta `IMerchantQueries` (driven port de leitura, no nível do agregado, dentro da pasta da feature) e uma porta de repositório reduzida; sem testes standalone de adapter; unit tests no seam de use case com NSubstitute; integração no seam do endpoint com Postgres real; DI em `Adapters.Persistence/ServiceCollectionExtensions.cs` (`AddScoped<IMerchantQueries, MerchantQueries>`).
