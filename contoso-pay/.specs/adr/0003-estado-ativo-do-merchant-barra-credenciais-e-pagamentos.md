---
Status: accepted
Date: 2026-10-24
---

# 0003: Estado ativo do merchant barra credenciais e pagamentos

`Merchant` ganhou `IsActive` — nasce ativo, a desativação é reversível. O flag é a condição de existência do merchant como origem de credenciais e pagamentos, e o bloqueio acontece **na emissão do token** (`POST /connect/token`), não na validação de cada requisição: o `connect` exige credencial de app client válida **e** `merchant.is_active` ao emitir. Consequência aceita: um token já emitido continua válido por até uma hora depois da desativação.

A desativação **não cascateia** para os app clients. O `is_active` do app client e o `is_active` do merchant são flags independentes: reativar o merchant não deve ressuscitar credenciais desativadas individualmente, e a cascata impediria distinguir as duas origens. Os slices que criam recursos em nome do merchant (`appclients`, `sales`) passam a exigir merchant existente **e** ativo nos seus próprios use cases — a regra não é centralizada em middleware.

## Considered Options

- **Validar o merchant ativo em cada requisição autenticada**: rejeitado — bloqueio imediato ao custo de um lookup por chamada; a emissão já cobre o caso de uso real (a integração desativada para de obter token em até uma hora).
- **Cascata física de `is_active` do merchant para os app clients**: rejeitado — dessincroniza dois flags independentes e mistura as duas causas de desativação.
- **Middleware central de autorização por merchant**: rejeitado por ora — não existe autenticação no código; a regra fica no use case de cada slice, onde a decisão de criar o recurso acontece.

## Consequences

- Nada no código atual verifica `IsActive`: a autenticação é feature futura. Até lá o flag é estado sem enforcement, e esta ADR registra que isso é deliberado.
- A spec do `connect` precisa incluir "merchant ativo na emissão"; a spec de `appclients` precisa incluir "merchant existente e ativo" na criação.
- Se o produto exigir revogação imediata, a decisão volta para a mesa, e o caminho é lookup por requisição ou lista de revogação — não uma mudança no modelo.
