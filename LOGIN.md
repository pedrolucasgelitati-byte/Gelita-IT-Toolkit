# Acesso ao Toolkit

O Toolkit utiliza a identidade já autenticada pelo Windows. A tela de acesso permite continuar com essa conta ou solicitar uma sessão administrativa pelo UAC do Windows.

Domínio da empresa: `sa.gelita.local`. O nome NetBIOS correspondente é `GSA`. Na janela de credenciais do Windows, podem ser usados os formatos `usuario@sa.gelita.local` e `GSA\usuario`.

## Perfis

| Área | Sessão comum | Sessão administrativa elevada |
|---|---|---|
| Dashboard | Dados do computador e da conta | Completo |
| Impressoras | Consultar catálogo/IP, testar ping e conectar ou remover conexões da própria sessão, quando permitido pelo Windows | Completo |
| Scanners | Consultar, testar ping e abrir Epson Scan 2/NAPS2 já instalados | Completo |
| Citrix | Abrir Workspace e adicionar as lojas à própria sessão | Completo |
| Ferramentas | Ping/TCP, abrir as configurações de impressoras do Windows e copiar dados de rede | As 24 ferramentas |
| Instalações, Configurações e Logs | Não disponíveis | Disponíveis |

A área comum não solicita elevação para suas operações. A conexão de impressoras continua sujeita às permissões e à disponibilidade de drivers no Windows; o Toolkit não altera as políticas de instalação de drivers.

## Entrada administrativa

O botão **Acesso administrativo** solicita ao Windows a abertura de uma nova instância com o verbo `runas`. O próprio Windows recebe e valida as credenciais. O Toolkit não possui campo de senha e não armazena credenciais.

Uma conta que pertença ao grupo Administradores mas esteja com token limitado permanece na área comum até a elevação. O acesso completo exige um token efetivamente administrativo, verificado por `WindowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator)`. O domínio apresentado orienta o formato da conta; a autorização depende dos privilégios efetivos naquele computador.

O argumento `--admin` indica a intenção de abrir a área administrativa e não concede permissões. A entrada administrativa e o construtor de `MainForm` verificam novamente o token do Windows. A área comum usa uma janela independente, sem criar o formulário administrativo.

Se o UAC for cancelado, a tela atual permanece disponível. Quando a abertura administrativa é confirmada pelo Windows, a instância anterior é encerrada e a nova utiliza a conta escolhida.

Operações que pertencem a um perfil de usuário, como conexões de impressoras e lojas do Citrix, aplicam-se à conta Windows que está executando aquela sessão. Para trabalhar no perfil de um usuário comum, use os recursos da área comum dessa conta.
