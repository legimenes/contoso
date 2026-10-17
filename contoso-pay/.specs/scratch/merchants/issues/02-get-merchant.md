# 02: Consultar merchant (público temporário)

**What to build:** um integrador consulta um merchant pelo id e recebe id, nome e documento, temporariamente sem exigir token, até o slice de connect ligar a proteção.

**Blocked by:** 01: Criar merchant.

**Status:** ready-for-agent

- [ ] `GET /v1/merchants/{id}` para id existente retorna `200 {id, name, document}` sem exigir token nesta fase
- [ ] Id inexistente retorna `404` no envelope padrão de problema
- [ ] Endpoint aparece na referência Scalar versionada com exemplo executável
- [ ] Proteção por Bearer fica registrada como dívida explícita para ser ligada quando o slice de connect existir, sem mudar o contrato de resposta
