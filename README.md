# Gelita Printer & Scanner Installer

## Visão Geral

Gelita Printer & Scanner Installer é uma ferramenta interna desenvolvida para a equipe de Service Desk da Gelita AG. O aplicativo automatiza o processo de instalação de impressoras, Epson Scan 2, NAPS (Network Application Platform Suite) e configuração de scanners Epson.

## Características

- ✅ Instalação automática de impressoras por unidade
- ✅ Instalação e configuração do Epson Scan 2
- ✅ Instalação do NAPS para digitalização em rede
- ✅ Configuração automática de scanners Epson
- ✅ Suporte para múltiplas unidades da Gelita (Maringá, Cotia, Mococa)
- ✅ Logging detalhado de operações
- ✅ Interface amigável em Windows Forms

## Estrutura do Projeto

```
GelitaInstaller/
├── Assets/                 # Arquivos de recursos e instaladores
│   ├── EpsonScan2/        # Instaladores do Epson Scan 2
│   ├── NAPS/              # Instaladores do NAPS
│   ├── Drivers/           # Drivers de impressoras
│   ├── Icons/             # Ícones da aplicação
│   └── Images/            # Imagens e recursos gráficos
├── Config/                # Arquivos de configuração JSON
│   ├── printers.json      # Configuração de impressoras
│   ├── scanners.json      # Configuração de scanners
│   ├── units.json         # Dados das unidades
│   └── appsettings.json   # Configurações gerais da aplicação
├── Models/                # Classes de modelo de dados
│   ├── Printer.cs         # Modelo de impressora
│   ├── Scanner.cs         # Modelo de scanner
│   ├── Unit.cs            # Modelo de unidade
│   └── InstallOptions.cs  # Opções de instalação
├── Services/              # Serviços de negócio
│   ├── PrinterService.cs     # Gerenciamento de impressoras
│   ├── ScannerService.cs     # Gerenciamento de scanners
│   ├── EpsonService.cs       # Operações específicas do Epson
│   ├── InstallService.cs     # Orquestração de instalação
│   ├── JsonService.cs        # Manipulação de arquivos JSON
│   ├── ProcessService.cs     # Gerenciamento de processos
│   ├── LoggerService.cs      # Sistema de logging
│   └── NetworkService.cs     # Operações de rede
├── Helpers/               # Classes auxiliares
│   ├── FileHelper.cs         # Operações com arquivos
│   ├── RegistryHelper.cs     # Acesso ao registro do Windows
│   ├── JsonHelper.cs         # Operações com JSON
│   ├── ProcessHelper.cs      # Auxiliares de processo
│   └── WindowsHelper.cs      # Operações do Windows
├── Forms/                 # Formulários da aplicação
│   ├── MainForm.cs           # Tela principal
│   ├── LoadingForm.cs        # Tela de carregamento
│   ├── SettingsForm.cs       # Configurações
│   └── AboutForm.cs          # Sobre a aplicação
├── Logs/                  # Arquivos de log (gerado em runtime)
├── Resources/             # Recursos adicionais
├── Program.cs             # Ponto de entrada da aplicação
└── README.md              # Este arquivo

```

## Requisitos

- **.NET 8.0** ou superior
- **Visual Studio 2022** (recomendado)
- **Windows 10/11** (x64 preferencialmente)
- **Privilégios de Administrador** para execução

## Instalação

1. Clone ou copie o projeto para sua máquina
2. Abra a solução em Visual Studio 2022
3. Restaure os pacotes NuGet
4. Compile a solução

## Como Usar

*(Documentação a ser preenchida após implementação dos Services)*

1. Inicie a aplicação (requer privilégios de administrador)
2. Selecione a unidade desejada (Maringá, Cotia ou Mococa)
3. Escolha as opções de instalação desejadas
4. Clique em "Instalar" e aguarde a conclusão

## Modelos de Dados

### Printer
Representa uma impressora com propriedades:
- `Name` - Nome amigável da impressora
- `Server` - Servidor de impressão
- `Share` - Compartilhamento da impressora
- `Unit` - Unidade associada
- `Model` - Modelo da impressora

### Scanner
Representa um scanner Epson com propriedades:
- `Model` - Modelo do scanner (ES0269, ES0288, etc)
- `IpAddress` - Endereço IP na rede
- `Name` - Nome do scanner
- `ScannerId` - Identificador único
- `ProductId` - ID do produto
- `DisplayName` - Nome de exibição
- `Guid` - GUID do dispositivo

### Unit
Representa uma unidade da Gelita com propriedades:
- `Name` - Nome da unidade
- `PrintServer` - Servidor de impressão
- `Printers` - Lista de impressoras configuradas

### InstallOptions
Define as opções de instalação:
- `InstallDrivers` - Instalar drivers
- `InstallNaps` - Instalar NAPS
- `InstallEpsonScan` - Instalar Epson Scan 2
- `ConfigureScanner` - Configurar scanners
- `InstallPrinters` - Instalar impressoras

## Arquivos de Configuração

### printers.json
Contém as configurações de impressoras por unidade:
```json
{
  "units": [
    {
      "name": "Maringá",
      "printServer": "\\\\br-mga1-srv013v",
      "printers": ["MG_PRINTER_224", "MG_PRINTER_225"]
    }
  ]
}
```

### scanners.json
Contém a configuração de scanners disponíveis:
```json
{
  "scanners": [
    {
      "model": "ES0269",
      "ipAddress": "192.168.1.100",
      "displayName": "Epson Perfection ES0269"
    }
  ]
}
```

### appsettings.json
Configurações gerais da aplicação, logging e caminhos.

## Desenvolvimento Futuro

Os seguintes componentes estão estruturados e prontos para implementação:

1. **Services**: Implementar lógica de negócio
2. **Helpers**: Implementar operações do Windows, registro e processos
3. **Forms**: Adicionar controles e eventos à interface
4. **Integração**: Conectar serviços aos formulários

## Logging

Os logs são salvos no diretório `./Logs` com configurações em `appsettings.json`.

## Notas Técnicas

- A aplicação requer privilégios de administrador para instalar drivers e modificar o registro
- O timeout padrão para operações de instalação é 5 minutos
- Máximo de 3 tentativas de reconexão em caso de falha

## Autor

Desenvolvido para a Gelita AG - Service Desk
Data de Criação: 2024

## Licença

Uso interno exclusivamente para Gelita AG
