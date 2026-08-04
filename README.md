# Gelita IT Toolkit

Ferramenta interna e portátil do Service Desk da Gelita para preparar, configurar, diagnosticar e reparar computadores Windows.

**Versão atual:** 1.0.4
**Plataforma:** Windows x64
**Tecnologia:** .NET 8 e Windows Forms

> Uso interno. Algumas funções alteram configurações do Windows e precisam ser executadas com uma conta administrativa autorizada.

## Funcionalidades

### Dashboard

- Identificação do computador, usuário, domínio, IP e endereço MAC.
- Processador, memória RAM, tipo e frequência da memória.
- Nome do Windows, versão de recurso (como 24H2 ou 25H2) e build completo.
- Diagnóstico de BitLocker, Microsoft Defender, Firewall, Secure Boot e TPM.
- Verificação do SentinelOne, GlobalProtect e administradores locais.

### Impressoras

- Carregamento dos equipamentos de cada unidade pelo arquivo JSON.
- Pesquisa e seleção de uma ou várias impressoras.
- Instalação apenas dos equipamentos selecionados ou de toda a unidade.
- Detecção de filas já instaladas.
- Definição da impressora padrão e impressão de página de teste.
- Testes de ping, porta TCP 9100 e página web do equipamento.
- Correção de filas offline e remoção de configurações duplicadas.

### Scanners

- Configuração de vários scanners no Epson Scan 2.
- Adição automática dos perfis correspondentes ao NAPS2.
- Nome dos perfis conforme o padrão de cada unidade.
- Remoção conjunta das configurações do Epson Scan 2 e NAPS2.
- Validação de todos os scanners configurados.
- Sincronização para usuários existentes e usuários de domínio que entrarem futuramente.
- Verificação da presença do equipamento e teste de digitalização.

### Instalações

- Epson Scan 2.
- NAPS2.
- SentinelOne.
- Palo Alto GlobalProtect.
- Microsoft Office pelo Office Deployment Tool (ODT).
- Pacote de habilitação do Windows 11 25H2.

### Citrix

- Configuração das contas corporativas no Citrix Workspace.
- Seleção independente de `CitrixBR` e `CitrixEB`.
- Possibilidade de adicionar uma ou as duas contas.

### Central de reparos

- Limpeza de disco e de arquivos temporários.
- Renovação do endereço IP e limpeza do cache DNS.
- Redefinição do Winsock e reinício dos serviços de rede.
- Reparo do Windows Update.
- Verificações SFC, DISM e CHKDSK.
- Limpeza e reinício do spooler de impressão.
- Atualização dos programas com `winget upgrade --all`.
- Histórico, indicadores de estado e barra de progresso.

### Configuração e manutenção

- Configurações em JSON, atualizáveis sem recompilar.
- Validação automática dos arquivos JSON.
- Caminhos de instaladores configuráveis.
- Backup de impressoras, scanners e configurações.
- Logs com ocultação de tokens, senhas e outros dados sensíveis.
- Atualização completa do Toolkit com validação SHA-256, backup e restauração em caso de falha.

## Como usar a versão portátil

1. Baixe o ZIP da versão desejada na seção **Releases** do GitHub.
2. Extraia todo o conteúdo para uma pasta local ou para o pendrive.
3. Mantenha as pastas `Assets` e `Config` ao lado do executável.
4. Segure **Shift** e clique com o botão direito em `Gelita-IT-Toolkit.exe`.
5. Escolha **Executar como outro usuário**.
6. Informe uma conta administrativa autorizada do Service Desk.

Também é possível iniciar pelo arquivo `Abrir-Gelita-IT-Toolkit.cmd`.

Não execute o programa diretamente de dentro do ZIP. A versão publicada é autossuficiente e não exige a instalação do .NET na máquina do usuário.

## Requisitos

### Para executar

- Windows 10 ou Windows 11 de 64 bits.
- Credenciais administrativas para instalações e reparos.
- Acesso à rede corporativa quando o recurso depender de servidores internos.
- Drivers e instaladores correspondentes dentro da pasta `Assets`.

### Para desenvolver

- Windows 10 ou Windows 11.
- .NET 8 SDK.
- Visual Studio 2022 ou Visual Studio Code com suporte a C#.
- Git, caso as alterações sejam versionadas.

O erro **“dotnet não é reconhecido”** significa que o SDK não está instalado ou não está no `PATH`. Isso afeta somente a compilação pelo código-fonte; para máquinas de usuários, utilize o executável autossuficiente publicado.

## Estrutura principal

```text
installerprinters/
├── Assets/                         Instaladores, ícone e pacotes auxiliares
├── Config/                         Configurações JSON
├── Core/                           Modelos e serviços centrais
├── Forms/                          Interface Windows Forms
├── Services/                       Instalação, diagnóstico e integrações
├── Program.cs                      Ponto de entrada
├── Gelita-IT-Toolkit.csproj        Configuração do projeto
├── Gelita-IT-Toolkit.sln           Solução do Visual Studio
├── Abrir-Gelita-IT-Toolkit.cmd     Inicializador auxiliar
└── README.md                       Documentação consolidada
```

Os instaladores grandes ou confidenciais podem estar ignorados pelo Git. Eles precisam existir localmente antes da publicação do pacote.

## Arquivos de configuração

| Arquivo | Finalidade |
| --- | --- |
| `Config/appsettings.json` | Configuração geral da aplicação e logs |
| `Config/units.json` | Unidades disponíveis |
| `Config/printers.json` | Impressoras e scanners de cada unidade |
| `Config/toolkit-settings.json` | Caminhos dos programas, pacotes e recursos |
| `Config/installer-hashes.json` | Hashes SHA-256 permitidos para instaladores |
| `Config/security-policy.json` | Política e lista permitida de administradores locais |

Valores locais podem usar o formato `${NOME_DA_VARIAVEL}` e são preenchidos pelo
arquivo `.env` durante a inicialização. Copie `.env.example` para `.env`, preencha
os valores corporativos e nunca envie o `.env` ao Git. Variáveis definidas
diretamente no Windows têm prioridade sobre o arquivo.

Recursos que dependem de variáveis opcionais, como contas Citrix, permanecem
desabilitados quando não estão configurados sem impedir a abertura do Toolkit.

Depois de alterar um JSON, use a validação e o recarregamento disponíveis no programa. Um JSON inválido não deve ser distribuído.

### Caminhos padrão importantes

- Epson Scan 2: `Assets/EpsonScan2`
- NAPS2: `Assets/NAPS`
- SentinelOne: `Assets/SentinelOne`
- GlobalProtect: `Assets/PaloAlto Client`
- Windows 11 25H2: `Assets/WindowsUpdates/windows11.0-kb5054156-x64.msu`
- Office ODT: `C:\ODT`
- Configuração do Office: `C:\ODT\Configuração.xml`

Para o Office, o comando executado é equivalente a:

```powershell
C:\ODT\setup.exe /configure "C:\ODT\Configuração.xml"
```

## Instaladores e validação de hash

Quando um instalador for incluído ou substituído:

1. Coloque o arquivo na pasta definida em `Config/toolkit-settings.json`.
2. Calcule o SHA-256:

   ```powershell
   Get-FileHash -Algorithm SHA256 -LiteralPath "C:\caminho\instalador.exe"
   ```

3. Atualize a entrada correspondente em `Config/installer-hashes.json`.
4. Valide os JSONs pelo Toolkit.
5. Teste a instalação em uma máquina controlada.

Um hash ausente ou diferente bloqueia a execução. Não armazene tokens, senhas ou chaves nos arquivos JSON.

## Atualização para Windows 11 25H2

O Toolkit utiliza o pacote:

```text
Assets/WindowsUpdates/windows11.0-kb5054156-x64.msu
```

O equipamento deve ser x64, estar no Windows 11 24H2 e possuir uma build compatível com o pacote. Antes da execução, o programa valida elegibilidade, existência do arquivo e SHA-256. A atualização pode exigir reinicialização.

## Compilação

Restaure as dependências e compile em modo Release:

```powershell
dotnet restore .\Gelita-IT-Toolkit.csproj
dotnet build .\Gelita-IT-Toolkit.csproj -c Release
```

O resultado padrão fica em:

```text
bin\Release\net8.0-windows\
```

## Publicação portátil

Para gerar a versão Windows x64 autossuficiente:

```powershell
dotnet publish .\Gelita-IT-Toolkit.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -o .\dist\Gelita-IT-Toolkit-v1.0.4-win-x64
```

Antes de distribuir:

1. Confirme a versão no arquivo `Gelita-IT-Toolkit.csproj`.
2. Compile sem erros.
3. Teste o executável publicado.
4. Verifique se `Assets`, `Config`, `LEIA-ME.txt` e o inicializador foram copiados.
5. Gere o ZIP.
6. Gere e publique o arquivo `.sha256` correspondente.
7. Teste a atualização e a restauração em uma máquina controlada.

## Atualizador do Toolkit

O botão de atualização baixa e substitui o programa completo, incluindo os arquivos distribuídos no pacote. O processo:

1. Consulta a versão e o SHA-256 do pacote disponível.
2. Baixa o ZIP e seu arquivo `.sha256`.
3. Valida a integridade.
4. Fecha o Toolkit.
5. Cria um backup da versão instalada.
6. Substitui os arquivos e abre a nova versão.
7. Restaura o backup automaticamente se a atualização falhar.

O arquivo `.env` local é copiado da instalação anterior para a nova. Ele não é
incluído no pacote publicado e não é enviado ao GitHub.

Além do número da versão, o Toolkit compara o SHA-256 do pacote publicado com o
pacote instalado. Assim, uma correção publicada na mesma versão também é
oferecida como atualização. O workflow `publish-current-version.yml` recompila e
substitui os arquivos da release atual a cada push na branch `main`.

A consulta por atualizações também ocorre silenciosamente ao iniciar, sem impedir
o uso do programa quando a rede ou o GitHub estiverem indisponíveis. A assinatura
do executável pode ser habilitada no GitHub Actions pelos secrets
`WINDOWS_SIGNING_CERTIFICATE_BASE64` e `WINDOWS_SIGNING_CERTIFICATE_PASSWORD`.

Se o repositório for privado, a API do GitHub pode responder **404 (Not Found)** em máquinas sem autenticação. O Toolkit pode usar um token somente de leitura fornecido pela variável de ambiente:

```powershell
$env:GELITA_TOOLKIT_GITHUB_TOKEN = "token-somente-leitura"
```

O token não deve ser gravado no projeto, no ZIP, nos JSONs ou nos logs. Para distribuição ampla, prefira um repositório público dedicado às atualizações ou outro local corporativo com autenticação gerenciada.

## Segurança

- Instaladores são validados por assinatura digital quando aplicável e por SHA-256.
- Tokens, senhas e segredos conhecidos são removidos dos logs.
- Administradores locais são comparados com `Config/security-policy.json`.
- Pacotes corporativos confidenciais não devem ser enviados ao repositório sem autorização.
- A atualização exige ZIP e checksum correspondentes.
- A versão anterior é preservada até a confirmação da atualização.
- Os logs devem conter somente os dados necessários para diagnóstico.

## Solução de problemas

| Problema | Ação recomendada |
| --- | --- |
| `dotnet` não é reconhecido | Instale o .NET 8 SDK para compilar ou use o executável publicado |
| Atualização retorna 404 | Verifique se o repositório é privado e se há autenticação de leitura |
| Instalador foi bloqueado | Confira o arquivo, sua assinatura e o hash configurado |
| JSON não carrega | Valide a sintaxe, nomes das propriedades e caminhos |
| Scanner não aparece no NAPS2 | Valide o Epson Scan 2 nos contextos administrativo e do usuário e sincronize os perfis |
| Scanner funciona apenas para o administrador | Execute a sincronização para usuários existentes e futuros do domínio |
| Citrix mostra `Store` ou `Store 1` | O Citrix pode usar nomes genéricos ao adicionar somente pela URL; revise a configuração gerenciada do Workspace |
| Impressora fica offline | Teste ping, porta 9100, página web, spooler e a porta configurada |
| Atualização 25H2 é recusada | Verifique arquitetura, versão atual, build mínima, pacote e hash |

## Validação antes de uma versão

```powershell
dotnet restore .\Gelita-IT-Toolkit.csproj
dotnet build .\Gelita-IT-Toolkit.csproj -c Release
git status
```

Além da compilação, faça testes em uma máquina sem configurações anteriores e em uma conta de usuário de domínio. Confira impressoras, Epson Scan 2, NAPS2, Citrix, instaladores, reparos, logs e atualização.

## Controle de versão

- Atualize `Version`, `AssemblyVersion` e `FileVersion` no projeto.
- Registre alterações relevantes no changelog da versão.
- Faça o commit somente dos arquivos revisados.
- Publique uma tag no formato `vX.Y.Z`.
- Anexe o ZIP e seu checksum ao Release.

## Documentos antigos

Este `README.md` passa a ser a documentação central do projeto. Os arquivos Markdown antigos foram mantidos temporariamente para conferência e podem ser removidos depois que seu conteúdo útil for validado aqui.

## Responsabilidade

Projeto de uso interno da Gelita AG. Instalação, manutenção e distribuição devem ser realizadas apenas por pessoal autorizado e conforme as políticas corporativas.
