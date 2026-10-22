# 05: Formalizar o agregado Merchant (comportamento e invariantes)

**What to build:** transformar `Merchant` em um agregado de verdade — hoje é uma classe selada com propriedades get-only e factory estática, sem comportamento, sem invariantes e sem base de agregado. O ticket 04 separou o acesso a dados (repository vs. queries) e expôs essa lacuna: o repositório devolve um agregado que não sabe se comportar.

**Blocked by:** None.

**Status:** ready-for-agent

- [ ] Base `AggregateRoot` no `Domain` (marcador, sem comportamento) e `Merchant` derivando dela
- [ ] Comportamento no agregado para as transições que o domínio permitir (ex.: `Rename`, `ChangeDocument`), com validação no próprio agregado e sem estado anêmico
- [ ] Invariante de **formato** do `Document` dentro do agregado (a **unicidade** é restrição de coleção e permanece no repositório — ver ADR-0002)
- [ ] `Merchant.New` passa a expressar as regras de criação, e o `createdAtUtc` deixa de ser gerado por `DateTime.UtcNow` direto na factory (injetável/controlável)
- [ ] Caminho de atualização do agregado no repositório (`UpdateAsync`) e, se aplicável, a operação de remoção — hoje não há update path
- [ ] Testes de domínio no padrão `automated-tests.md` cobrindo os comportamentos e as invariantes do agregado
- [ ] ADR ou atualização de ADR-0002 se a formalização mudar o critério de classificação do acesso a dados

## Comments

- Dívida registrada durante a implementação do ticket 04 (separação repository vs. queries). A lacuna é anterior a ele: `Merchant` nasceu anêmico e nenhuma fatia de produto exigiu comportamento até agora.
- Ao formalizar, o repositório passa a ter methods que "modificam" o agregado, e aí a fronteira repository vs. queries pode merecer revisão (ver a observação da ADR-0002 sobre a degeneração do repositório em porta de escrita).
- O glossário (`.specs/GLOSSARY.md`) já fixa os termos *Merchant* e *Document*; a formalização deve alinhar o vocabulário do código a esses termos.

## Technical plan (approved)

- DDL delta: a definir no momento da implementação (depende de quais invariantes virarem coluna ou restrição).
- Classes: a definir.
- Libs: a definir.
- Seams/ports: a definir.
