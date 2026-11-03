# Contoso Pay

Gateway de pagamento de estudo: registra merchants e, a partir deles, credenciais de acesso e pagamentos. Contexto único — todos os conceitos convivem no mesmo domínio.

## Language

**Merchant**:
Negócio registrado na plataforma, dono de um app client e de pagamentos. É a raiz do modelo Merchant 1:1 AppClient e o escopo de isolamento das operações.
_Avoid_: Estabelecimento, Empresa, Lojista, Cliente

**AppClient**:
Credencial OAuth 2.0 de um merchant, identificada por um `client_id` opaco e por um client secret rotacionável. Existe no máximo um app client por merchant; não é um agregado de negócio.
_Avoid_: Aplicação, Integração, Credencial, API Key

**Client secret**:
Segredo do app client, exibido em texto puro apenas na criação e na rotação; o armazenamento guarda somente o hash.
_Avoid_: Senha, Token, Chave

**Rotate secret**:
Transição que substitui o client secret de um app client; o secret anterior deixa de autenticar imediatamente e o `client_id` não muda.
_Avoid_: Refresh, Renovar token, Resetar senha

**Access token**:
JWT de curta duração emitido em `/connect/token` a partir de um app client; carrega o `merchant_id` e autoriza chamadas Bearer.
_Avoid_: Sessão, Token de sessão

**Document**:
Identificador fiscal (CPF ou CNPJ) que identifica um merchant no cadastro; único em todo o sistema.
_Avoid_: Registro, Inscrição, TaxId

**Active merchant**:
Merchant habilitado a operar na plataforma; todo merchant nasce ativo e a desativação é reversível.
_Avoid_: Enabled, Disabled, Blocked, Suspended

**Deactivate**:
Transição que torna um merchant inativo, impedindo-o de originar credenciais de acesso e pagamentos.
_Avoid_: Disable, Inactivate, Inativar, Suspender, Bloquear
