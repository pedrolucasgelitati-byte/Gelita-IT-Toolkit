# Inventário sem hospedagem paga

Painel: https://pedrolucasgelitati-byte.github.io/gelita-toolkit/

Interface pública: `pedrolucasgelitati-byte/gelita-toolkit`.
Relatórios privados: `pedrolucasgelitati-byte/gelita-toolkit-inventory`.
O modo não precisa de servidor ASP.NET, Azure, Render ou banco de dados pago.
Ele usa os limites gratuitos do GitHub Pages e da API; não é um serviço de
telemetria em tempo real e o histórico do Git cresce com cada envio.

## App

Ao abrir, o app gera `%LOCALAPPDATA%\GelitaITToolkit\inventory-report.json`.
O relatório de esquema 2 inclui o dashboard da estação: processador, memória,
tipo/frequência de RAM, service tag, IP/MAC, uso de CPU/memória, tempo ligado e
temperatura da CPU quando o sensor já estiver disponível. Nenhum driver é
instalado para coletar o relatório. Valores ausentes permanecem nulos.
Esse arquivo pode ser aberto diretamente no painel, sem credencial e sem upload.
Para gerar o arquivo e sair sem abrir a interface, execute
`Gelita-IT-Toolkit.exe --export-inventory`.

Para enviar ao repositório privado, abra **Arquivo → Inventário GitHub** ou:

```powershell
.\Gelita-IT-Toolkit.exe --configure-inventory
```

Configure um token individual fine-grained do GitHub com acesso apenas ao
repositório privado de inventário e permissão **Contents: Read and write**.
A credencial é salva no Gerenciador de Credenciais do Windows da conta atual.
Não use token compartilhado entre estações e não distribua tokens no ZIP ou `.env`.
Um token com essa permissão pode ler e alterar todos os relatórios desse
repositório; configure apenas em sessões autorizadas de TI.

A janela valida que o repositório é privado. O serviço verifica a visibilidade
novamente antes de cada envio e recusa repositórios públicos.
Remova a credencial pela mesma janela e revogue-a no GitHub se não for necessária.
Reabra o app após configurar.

O envio inicial espera de 5 a 120 segundos para distribuir requisições.
Após sucesso, ocorre no máximo um envio por máquina/repositório/conta Windows a
cada 24 horas, enquanto o app estiver aberto. A atualização do esquema permite
uma nova coleta mesmo quando o relatório antigo foi enviado no mesmo dia.
Falhas são tentadas novamente
a cada 30 minutos. Não há serviço nem tarefa agendada; o relatório local é
atualizado a cada 30 minutos enquanto aberto.
O último status está em `github-inventory-status.log`, na mesma pasta local.

Os arquivos remotos usam `reports/<sha256-do-nome-da-maquina-em-maiusculas>.json`.
Máquinas com o mesmo nome em diferentes domínios compartilham a mesma entrada;
esta versão pressupõe nomes únicos na organização.
O envio informa SHA para atualizar arquivos existentes. Um conflito não sobrescreve
à força os dados: o serviço tenta novamente no próximo intervalo.

## Dados

Nome da máquina, usuário sem domínio, unidade configurada, Windows, versão
do Toolkit, presença do SentinelOne, versão do Snow e seu SiteName, quando legíveis.
Somente esses campos são lidos do Snow; nenhum config, dump ou documento é enviado.
Sistema, versão de atualização do Windows (por exemplo 25H2), build com revisão
e arquitetura são campos separados. Os dados do dashboard são uma fotografia
da coleta, e não representam acompanhamento em tempo real.
Uma sessão elevada pode informar o usuário administrativo que iniciou o processo.

## Painel

Use token individual com **Contents: Read-only**, limitado ao repositório privado.
O token permanece apenas na memória da aba. Nenhuma credencial ou relatório é
salvo em localStorage, sessionStorage ou cookies. Apenas a preferência de tema
claro/escuro é salva no navegador. Não há serviços externos de análise.
Ao desconectar ou fechar a aba, os dados e a credencial deixam a sessão.
O login Microsoft da versão ASP.NET não faz parte deste modo.

O painel lê até 1.000 relatórios (64 KB por arquivo), em lotes de quatro requisições.
A conexão e a importação ficam na janela **Conectar inventário**. Clicar no nome
de uma máquina abre seu dashboard no próprio site. Relatórios antigos continuam
compatíveis; campos não coletados aparecem como **Não informado**.
A atualização é manual para reduzir chamadas da API. Indicadores descrevem o
inventário coletado: última coleta não indica disponibilidade online, e instalação
do SentinelOne não confirma saúde do antivírus.

## Verificação

```powershell
dotnet run --project Tests/GitHubInventoryChecks/GitHubInventoryChecks.csproj
dotnet run --project Tests/PortalChecks/PortalChecks.csproj
```

O primeiro teste usa transporte em memória e dados fictícios, além de criar e
remover uma credencial sintética no cofre do Windows. Não publica inventário real.
O modo ASP.NET anterior continua disponível, mas só envia quando sua URL e chave
forem configuradas explicitamente.
