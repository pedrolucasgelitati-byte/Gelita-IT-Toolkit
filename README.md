# Gelita IT Toolkit

Ferramenta portátil do Service Desk da Gelita para preparar, configurar e reparar computadores Windows.

## Download

Baixe o arquivo ZIP mais recente na seção **Releases** do GitHub, extraia todo o conteúdo e mantenha as pastas `Assets` e `Config` ao lado do executável.

## Como executar na máquina do usuário

1. Extraia o ZIP ou copie a pasta completa para o computador.
2. Segure **Shift** e clique com o botão direito em `Gelita-IT-Toolkit.exe`.
3. Selecione **Executar como outro usuário**.
4. Informe a conta administrativa do Service Desk.

Não execute o programa diretamente de dentro do ZIP.

## Principais recursos

- Instalação e gerenciamento de impressoras.
- Configuração conjunta do Epson Scan 2 e NAPS2.
- Instalação de Office, SentinelOne, GlobalProtect e outros pacotes corporativos.
- Dashboard de hardware, rede e versão do Windows.
- Central de reparos com SFC, DISM, CHKDSK, rede, Windows Update, spooler e winget.
- Configurações JSON recarregáveis sem recompilar.
- Validação de assinatura e SHA-256 dos instaladores.
- Logs e histórico persistente de execuções.

## Requisitos

- Windows 10 ou Windows 11 de 64 bits.
- Credenciais administrativas para instalações e reparos.
- Conectividade com a rede corporativa quando o recurso depender de servidores internos.

## Segurança

O Toolkit valida os instaladores configurados antes da execução. Se um instalador for atualizado, seu hash em `Config/installer-hashes.json` também precisa ser atualizado.

- Logs removem automaticamente senhas, tokens, chaves de API e segredos conhecidos.
- A lista permitida de administradores locais fica em `Config/security-policy.json`.
- Atualizações só são baixadas quando o Release possui um arquivo `.sha256` correspondente.
- Depois da validação, o Toolkit fecha, cria um backup da versão atual, substitui todos os arquivos e abre novamente. Em caso de falha, a versão anterior é restaurada automaticamente.

Como o repositório é privado, a consulta de atualizações pode usar um token
somente de leitura fornecido pela variável de ambiente
`GELITA_TOOLKIT_GITHUB_TOKEN`. O token nunca é salvo nem exibido nos logs. Não
coloque tokens nos arquivos JSON.
