# 01: Estado de ativação do merchant e exposição na leitura

**What to build:** todo merchant passa a ter estado de ativação. O registro ganha a coluna `is_active`, o agregado ganha o estado e as duas transições, e o valor aparece em `GET /v1/merchants/{id}` e em cada item de `GET /v1/merchants`. Nesta fatia o estado é sempre ativo, porque nenhuma transição ainda é exposta pela API — o que se entrega é o campo observável e o agregado pronto para receber as duas features seguintes.

**Blocked by:** None (can start immediately).

**Status:** resolved

- [x] A tabela de merchants ganha `is_active BOOLEAN NOT NULL`, **sem default na declaração**: o valor é sempre escrito pela aplicação, e um writer que esquecer a coluna falha no not-null em vez de receber `TRUE` silencioso
- [x] O arquivo único de objetos SQL é atualizado; o delta para volumes já existentes é documentado e ordenado (adicionar nullable, preencher com `TRUE`, aplicar `NOT NULL`), sem recriar volume
- [x] `Merchant` expressa o estado com setter privado, mantém o construtor único recebendo o booleano, e a factory estática sempre cria um *active merchant*
- [x] O agregado expõe as duas transições (`Activate` e `Deactivate`), sem guarda e sem exceção: repetir uma transição é no-op bem-sucedido
- [x] O mapeamento de persistência marca a coluna como obrigatória e **não** declara default de banco — um default faria o provider omitir a coluna quando o valor fosse `false`
- [x] `GET /v1/merchants/{id}` devolve o estado de ativação junto de id, nome e documento
- [x] `GET /v1/merchants` devolve o estado em cada item, e a query string permanece inalterada: nenhum filtro novo, nenhuma ordenação nova
- [x] Testes de domínio: criação nasce ativa; cada transição; transição repetida permanece no estado alvo
- [x] Testes de integração: o campo aparece e é `true` tanto na leitura por id quanto na listagem
- [x] Builders de teste produzem um merchant inativo **pelo método de domínio**, não por atalho de construtor
- [x] Specs de `connect`, `appclients` e `merchants` emendadas: o estado existe, não é aplicado nesta fatia, e o que cada slice futura deve exigir fica registrado

## Comments

- O estado não tem enforcement nesta fatia: autenticação é feature futura. A decisão está em `.specs/adr/0003-estado-ativo-do-merchant-barra-credenciais-e-pagamentos.md`.
- Terminologia: o glossário fixa *Active merchant* e *Deactivate*; "inactivate", "disable" e "blocked" estão listados como termos a evitar.
- Sobreposição conhecida: o ticket `merchants/05-formalizar-agregado-merchant` também prevê comportamento no agregado e caminho de atualização. Esta fatia entrega as transições de ativação, mas **não** fecha o 05 — base `AggregateRoot`, `Rename`/`ChangeDocument`, invariante de formato do documento e clock injetável continuam em aberto lá.
- DDL delta aplicado: `docker/scripts/postgresql/01-objects.sql` declara `is_active BOOLEAN NOT NULL` (sem default) entre `document` e `created_at`, e carrega no fim do arquivo o delta manual ordenado (`ADD COLUMN is_active BOOLEAN` → `UPDATE ... SET is_active = TRUE` → `SET NOT NULL`) para volumes existentes. O delta foi executado no volume local `contosopay_postgres` (3 linhas preenchidas) antes dos testes; nenhum volume foi recriado.
- Implementação: `Merchant` ganhou `bool isActive` no construtor único e `IsActive { get; private set; }`; `New` sempre passa `true`; `Activate`/`Deactivate` atribuem o setter privado, sem guarda e sem exceção. Mapeamento EF `is_active` com `IsRequired()` e sem default. `GetMerchantByIdResponse` e `ListMerchantsResponse` ganharam `bool IsActive` (JSON `isActive`); use case e projeção de `MerchantQueries` repassam o valor; nenhum filtro ou ordenação foi adicionado à listagem.
- Testes: 5 testes de domínio (nasce ativa, desativar, ativar, desativar repetido, ativar repetido); `MerchantBuilder.Inactive()` desativa pelo método de domínio; unidade de get/list conferem o campo; integração contra Postgres real confere `isActive: true` no `GET` por id e em cada item da listagem. Suite total 36/36 verde, build 0 warnings.
- Specs emendadas: `connect` (emissão exige merchant ativo, token já emitido vale até expirar), `appclients` (criação exige merchant existente **e** ativo, sem cascata de `is_active`), `merchants` (leitura expõe `isActive`, listagem sem filtro de ativação, transições apontam para o slice de lifecycle). Glossário e ADR-0003 já existiam e permanecem como registrado.
- Fora desta fatia (tickets 02/03): endpoints `activate`/`deactivate`, `ResultType.NoContent`, mapper de sucesso sem corpo, porta de atualização do repositório, exemplos Scalar/`API.http`.
