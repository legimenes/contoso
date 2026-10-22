# Contoso Pay

Gateway de pagamento de estudo: registra merchants e, a partir deles, credenciais de acesso e pagamentos. Contexto único — todos os conceitos convivem no mesmo domínio.

## Language

**Merchant**:
Negócio registrado na plataforma, dono de app clients e de pagamentos. É a raiz do modelo Merchant 1-N AppClients e o escopo de isolamento das operações.
_Avoid_: Estabelecimento, Empresa, Lojista, Cliente

**Document**:
Identificador fiscal (CPF ou CNPJ) que identifica um merchant no cadastro; único em todo o sistema.
_Avoid_: Registro, Inscrição, TaxId
