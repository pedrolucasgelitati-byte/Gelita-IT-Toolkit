# Arquitetura do Projeto - Gelita IT Toolkit

## Visão Geral da Arquitetura

O projeto segue um padrão de **arquitetura em camadas** com separação clara de responsabilidades entre apresentação, lógica de negócio e dados.

```
┌─────────────────────────────────────┐
│   Camada de Apresentação            │
│   (Windows Forms)                   │
│   - MainForm.cs                     │
│   - LoadingForm.cs                  │
│   - SettingsForm.cs                 │
│   - AboutForm.cs                    │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│   Camada de Lógica de Negócio       │
│   (Services)                        │
│   - PrinterService                  │
│   - ScannerService                  │
│   - EpsonService                    │
│   - InstallService                  │
│   - JsonService                     │
│   - ProcessService                  │
│   - LoggerService                   │
│   - NetworkService                  │
└──────────────┬──────────────────────┘
               │
               ▼
┌─────────────────────────────────────┐
│   Camada de Dados e Helpers         │
│   - Models (Printer, Scanner, etc)  │
│   - Helpers (File, Registry, etc)   │
│   - Config (JSON)                   │
└─────────────────────────────────────┘
```

## Camadas do Projeto

### 1. Camada de Apresentação (Forms)
**Responsabilidades:**
- Exibir interface ao usuário
- Capturar entrada do usuário
- Exibir feedback visual

**Componentes:**
- `MainForm.cs` - Interface principal
- `LoadingForm.cs` - Indicador de progresso
- `SettingsForm.cs` - Configurações
- `AboutForm.cs` - Informações da app

**Padrão:** Separação clara entre apresentação e lógica

### 2. Camada de Lógica de Negócio (Services)
**Responsabilidades:**
- Implementar regras de negócio
- Orquestrar operações
- Coordenar chamadas entre componentes

**Serviços Principais:**

#### PrinterService
- Gerenciar impressoras
- Listar impressoras por unidade
- Instalar/remover impressoras
- Validar status de instalação

#### ScannerService
- Gerenciar scanners Epson
- Configurar dispositivos
- Validar conectividade
- Manter estado dos scanners

#### EpsonService
- Verificar instalação do Epson Scan 2
- Instalar/desinstalar Epson Scan
- Gerenciar versão do software
- Iniciar aplicação Epson

#### InstallService
- Orquestrador principal de instalação
- Coordenar múltiplos serviços
- Validar pré-requisitos
- Rastrear progresso
- Gerenciar cancelamento

#### JsonService
- Carregar arquivos de configuração JSON
- Serializar/desserializar dados
- Gerenciar persistência de configurações
- Validar integridade de dados

#### ProcessService
- Executar processos do Windows
- Executar com privilégios elevados
- Capturar saída de processos
- Gerenciar ciclo de vida de processos

#### LoggerService
- Registrar eventos da aplicação
- Diferentes níveis de log (Debug, Info, Warning, Error)
- Gerenciar arquivo de log
- Autolimpeza de logs antigos

#### NetworkService
- Verificar conectividade
- Descobrir recursos de rede
- Resolver hostnames
- Validar acessibilidade de servidores

### 3. Camada de Dados (Models & Helpers)

#### Models
Definem a estrutura de dados da aplicação:

**Printer**
```csharp
- Name: string (nome amigável)
- Server: string (servidor de impressão)
- Share: string (compartilhamento)
- Unit: string (unidade)
- Model: string (modelo)
```

**Scanner**
```csharp
- Model: string (ES0269, ES0288)
- IpAddress: string
- Name: string
- ScannerId: string
- ProductId: string
- DisplayName: string
- Guid: string
```

**Unit**
```csharp
- Name: string
- PrintServer: string
- Printers: List<string>
```

**InstallOptions**
```csharp
- InstallDrivers: bool
- InstallNaps: bool
- InstallEpsonScan: bool
- ConfigureScanner: bool
- InstallPrinters: bool
```

#### Helpers
Classes utilitárias com métodos estáticos:

**FileHelper**
- Operações com arquivos
- Criação de diretórios
- Cópia/exclusão de arquivos
- Leitura de conteúdo

**RegistryHelper**
- Acesso ao registro do Windows
- Verificar aplicativos instalados
- Obter paths de instalação
- Modificar valores do registro

**JsonHelper**
- Serialização/desserialização
- Validação de JSON
- Formatação (pretty-print)
- Merge de objetos

**ProcessHelper**
- Executar comandos CMD
- Executar scripts PowerShell
- Elevar privilégios
- Gerenciar processos

**WindowsHelper**
- Verificar privilégios de admin
- Obter informações do sistema
- Gerenciar reinicializações
- Abrir gerenciadores do Windows

### 4. Camada de Configuração
**Config/appsettings.json**
- Configurações globais
- Paths de recursos
- Níveis de logging
- Timeouts e tentativas

**Config/printers.json**
- Definição de impressoras por unidade
- Servidores de impressão
- Compartilhamentos de rede

**Config/scanners.json**
- Modelos de scanners disponíveis
- Endereços IP
- Identificadores de dispositivos

**Config/units.json**
- Dados das unidades da Gelita
- Localização e contatos

## Fluxo de Execução Típico

```
1. Usuário abre MainForm
   │
2. MainForm carrega configurações via JsonService
   │
3. Usuário seleciona unidade e opções de instalação
   │
4. MainForm inicia InstallService.ExecuteFullInstallation()
   │
5. InstallService valida pré-requisitos
   │
6. Para cada opção selecionada:
   ├─ PrinterService.InstallPrinters()
   ├─ EpsonService.InstallEpsonScan()
   ├─ InstallService (NAPS)
   └─ ScannerService.ConfigureScanner()
   │
7. ProcessService executa scripts/instaladores
   │
8. LoggerService registra todas as operações
   │
9. MainForm atualiza LoadingForm com progresso
   │
10. Ao finalizar, exibe resultado ao usuário
```

## Padrões de Código Utilizados

### Dependency Injection
```csharp
// Registrar em Program.cs
services.AddScoped<PrinterService>();
services.AddScoped<ScannerService>();
// ... etc
```

### Async/Await
Todas as operações longas utilizam operações assíncronas:
```csharp
public async Task<bool> InstallPrinter(Printer printer)
{
    // Operação assíncrona
}
```

### XML Documentation
Todas as classes e métodos públicos possuem comentários XML:
```csharp
/// <summary>
/// Descrição do método
/// </summary>
/// <param name="param">Descrição do parâmetro</param>
/// <returns>Descrição do retorno</returns>
public Task<bool> Method(string param) { }
```

### Validação de Entrada
```csharp
if (string.IsNullOrWhiteSpace(printerName))
{
    throw new ArgumentException("Printer name cannot be empty");
}
```

## Tratamento de Erros

- Exceções são capturadas em level apropriado
- LoggerService registra todos os erros
- MainForm exibe mensagens amigáveis ao usuário
- Operações são reversíveis quando possível

## Segurança

- Validação de privilégios de administrador no início
- Paths sanitizados antes de uso
- Comandos validados antes de execução
- Acesso ao registro restrito

## Performance

- Operações de rede executadas assincronamente
- Recursos liberados adequadamente
- Caching de configurações em memória
- Paginação para listas grandes

## Extensibilidade

A arquitetura permite fácil adição de:
- Novos tipos de impressoras
- Novos scanners
- Novas unidades
- Novas operações via Services

## Próximos Passos

1. Implementar cada Service completamente
2. Criar testes unitários para cada camada
3. Adicionar tratamento robusto de erros
4. Implementar eventos e callbacks
5. Criar documentação de usuário
