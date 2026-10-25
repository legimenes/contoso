# Contoso Pay

Gateway de pagamento de estudo: registra merchants e, a partir deles, credenciais de acesso e pagamentos. Contexto único — todos os conceitos convivem no mesmo domínio.

## Language

**Merchant**:
Negócio registrado na plataforma, dono de app clients e de pagamentos. É a raiz do modelo Merchant 1-N AppClients e o escopo de isolamento das operações.
_Avoid_: Estabelecimento, Empresa, Lojista, Cliente

**Document**:
Identificador fiscal (CPF ou CNPJ) que identifica um merchant no cadastro; único em todo o sistema.
_Avoid_: Registro, Inscrição, TaxId

**Active merchant**:
Merchant habilitado a operar na plataforma; todo merchant nasce ativo e a desativação é reversível.
_Avoid_: Enabled, Disabled, Blocked, Suspended

**Deactivate**:
Transição que torna um merchant inativo, impedindo-o de originar credenciais de acesso e pagamentos.
_Avoid_: Disable, Inactivate, Inativar, Suspender, Bloquear
