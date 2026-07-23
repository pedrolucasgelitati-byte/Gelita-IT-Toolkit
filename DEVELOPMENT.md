# Guia de Desenvolvimento - Gelita Printer & Scanner Installer

## Visão Geral

Este documento fornece um roteiro para implementação dos Services e funcionalidades do projeto.

## Fases de Desenvolvimento

### Fase 1: Setup e Configuração (✅ COMPLETA)
- [x] Criar estrutura de pastas
- [x] Criar classes de modelo (Models)
- [x] Criar stubs de Services
- [x] Criar stubs de Helpers
- [x] Criar stubs de Forms
- [x] Criar arquivos JSON de exemplo
- [x] Configurar projeto .NET 8

### Fase 2: Implementação de Helpers (⏳ PRÓXIMO)

#### 2.1 FileHelper.cs
**Métodos a implementar:**
```csharp
✔ FileExists() - Usar System.IO.File.Exists()
✔ DirectoryExists() - Usar System.IO.Directory.Exists()
✔ CreateDirectoryAsync() - Usar System.IO.Directory.CreateDirectory()
✔ CopyFileAsync() - Usar System.IO.File.Copy()
✔ DeleteFileAsync() - Usar System.IO.File.Delete()
✔ ReadTextFileAsync() - Usar System.IO.File.ReadAllTextAsync()
✔ WriteTextFileAsync() - Usar System.IO.File.WriteAllTextAsync()
✔ GetFileSize() - Usar FileInfo.Length
✔ GetFilesInDirectoryAsync() - Usar Directory.GetFiles()
```

#### 2.2 RegistryHelper.cs
**Métodos a implementar:**
```csharp
✔ GetRegistryValue() - Usar Microsoft.Win32.Registry
✔ SetRegistryValue() - Usar Registry.SetValue()
✔ RegistryKeyExists() - Verificar subkey
✔ DeleteRegistryKey() - Usar Registry.LocalMachine.DeleteSubKey()
✔ IsApplicationInstalled() - Verificar em Uninstall keys
✔ GetApplicationInstallPath() - Ler InstallLocation
```

#### 2.3 JsonHelper.cs
**Métodos a implementar:**
```csharp
✔ SerializeToJson() - Usar System.Text.Json.JsonSerializer
✔ DeserializeFromJson() - Usar JsonSerializer.Deserialize()
✔ IsValidJson() - Try/catch na desserialização
✔ FormatJson() - Usar JsonWriter com indentation
✔ MergeJsonObjects() - Merge manual de propriedades
```

#### 2.4 ProcessHelper.cs
**Métodos a implementar:**
```csharp
✔ ExecuteCommand() - Usar cmd.exe com ProcessStartInfo
✔ ExecutePowerShellCommand() - Usar powershell.exe
✔ ExecuteAsAdmin() - UseShellExecute + Verb = "runas"
✔ GetProcessesByName() - Usar Process.GetProcessesByName()
✔ GetProcessById() - Usar Process.GetProcessById()
✔ KillProcessById() - Usar Process.Kill()
✔ WaitForProcess() - Usar Process.WaitForExit()
```

#### 2.5 WindowsHelper.cs
**Métodos a implementar:**
```csharp
✔ IsRunningAsAdmin() - Usar WindowsIdentity.GetCurrent()
✔ GetWindowsVersion() - Usar Environment.OSVersion
✔ GetSystemArchitecture() - Usar RuntimeInformation
✔ GetComputerName() - Usar Environment.MachineName
✔ GetCurrentUsername() - Usar Environment.UserName
✔ RestartComputer() - ProcessHelper.ExecuteCommand("shutdown /r")
✔ ShutdownComputer() - ProcessHelper.ExecuteCommand("shutdown /s")
✔ OpenDeviceManager() - ProcessHelper.ExecuteCommand("devmgmt.msc")
✔ OpenPrinterManagement() - ProcessHelper.ExecuteCommand("control printers")
```

### Fase 3: Implementação de Services (⏳ PRÓXIMO)

#### 3.1 JsonService.cs
**Dependências:** JsonHelper, FileHelper, LoggerService
**Métodos a implementar:**
```csharp
✔ ReadJsonFile<T>() - Usar FileHelper + JsonHelper
✔ WriteJsonFile<T>() - Usar FileHelper + JsonHelper
✔ LoadAllConfigurations() - Carregar todos os JSONs em cache
✔ SaveAllConfigurations() - Salvar cache em disco
```

**Cache em memória:**
```csharp
private Dictionary<string, object> _configCache = new();
```

#### 3.2 LoggerService.cs
**Dependências:** FileHelper
**Métodos a implementar:**
```csharp
✔ LogInfo() - Escrever em arquivo com timestamp
✔ LogWarning() - Escrever com prefixo [WARNING]
✔ LogError() - Escrever com prefixo [ERROR]
✔ LogException() - Escrever Exception.ToString()
✔ LogDebug() - Escrever com prefixo [DEBUG]
✔ GetLogContent() - Ler arquivo de log
✔ ClearLog() - Limpar arquivo de log
```

**Padrão de arquivo:** `Logs/GelitaInstaller_YYYYMMDD_HHMMSS.log`

#### 3.3 NetworkService.cs
**Dependências:** ProcessHelper, LoggerService
**Métodos a implementar:**
```csharp
✔ CheckInternetConnectivity() - Ping a 8.8.8.8
✔ TestConnectivity() - Usar System.Net.NetworkInformation.Ping
✔ GetNetworkShares() - ExecuteCommand com "net view"
✔ GetLocalIpAddress() - Usar IPAddress.GetHostEntry()
✔ ResolveHostname() - Usar Dns.GetHostAddresses()
```

#### 3.4 ProcessService.cs
**Dependências:** ProcessHelper, LoggerService
**Métodos a implementar:**
```csharp
✔ ExecuteProcessAsync() - Wrapper de ProcessHelper
✔ ExecuteProcessAsAdminAsync() - Wrapper com elevação
✔ GetProcessOutput() - Capturar StandardOutput
✔ IsProcessRunning() - Usar ProcessHelper.GetProcessesByName()
✔ KillProcess() - Wrapper de ProcessHelper
```

#### 3.5 PrinterService.cs
**Dependências:** ProcessHelper, RegistryHelper, LoggerService
**Métodos a implementar:**
```csharp
✔ GetPrintersByUnit() - Ler de printers.json
✔ InstallPrinter() - ExecuteProcessAsAdmin() com printui.dll
✔ InstallMultiplePrinters() - Loop com timeout entre iterações
✔ RemovePrinter() - Usar WMI ou printui.dll
✔ IsPrinterInstalled() - Verificar em \\ComputerName\
```

**Referência Windows:**
```powershell
Add-Printer -PrinterName "name" -PortName "IPP_Port" -DriverName "driver"
```

#### 3.6 ScannerService.cs
**Dependências:** RegistryHelper, ProcessHelper, LoggerService
**Métodos a implementar:**
```csharp
✔ GetAvailableScanners() - Ler scanners.json
✔ ConfigureScanner() - Modificar registro do Epson Scan
✔ ConfigureMultipleScanners() - Loop com validação
✔ RemoveScanner() - Remover entrada do registro
✔ IsScannerConfigured() - Verificar em HKEY_LOCAL_MACHINE
```

**Registro Epson:**
```
HKEY_LOCAL_MACHINE\SOFTWARE\EPSON\Scan
```

#### 3.7 EpsonService.cs
**Dependências:** RegistryHelper, ProcessHelper, LoggerService
**Métodos a implementar:**
```csharp
✔ IsEpsonScanInstalled() - Verificar Registry
✔ InstallEpsonScan() - Executar setup com parâmetros silenciosos
✔ UninstallEpsonScan() - Usar MsiExec ou built-in uninstaller
✔ GetEpsonScanVersion() - Ler de Registry ou arquivo
✔ StartEpsonScan() - Executar exe do Epson Scan
```

#### 3.8 InstallService.cs
**Dependências:** Todos os services acima
**Métodos a implementar:**
```csharp
✔ ExecuteFullInstallation() - Orquestrador principal
✔ ValidatePrerequisites() - Verificar privilégios, espaço em disco
✔ GetInstallationProgress() - Retornar percentual
✔ CancelInstallation() - Matar processos em andamento
✔ GetInstallationStatus() - Retornar status atual
```

**Fluxo de ExecuteFullInstallation():**
1. Validar pré-requisitos
2. Se InstallDrivers: PrinterService.InstallMultiplePrinters()
3. Se InstallNaps: Executar NAPS installer
4. Se InstallEpsonScan: EpsonService.InstallEpsonScan()
5. Se ConfigureScanner: ScannerService.ConfigureMultipleScanners()
6. Logging de tudo

### Fase 4: Implementação de Forms (⏳ PRÓXIMO)

#### 4.1 MainForm.cs
**Controles necessários:**
```
- ComboBox para seleção de unidade
- CheckBox para cada opção de instalação
- Button "Instalar"
- Button "Configurações"
- Button "Sobre"
- Button "Sair"
- RichTextBox para feedback
- ProgressBar
```

#### 4.2 LoadingForm.cs
**Controles necessários:**
```
- Label com texto de status
- ProgressBar
- CancelButton
```

#### 4.3 SettingsForm.cs
**Controles necessários:**
```
- TextBox para log path
- ComboBox para log level
- CheckBox para "autodetect"
- Button "Salvar"
- Button "Cancelar"
```

#### 4.4 AboutForm.cs
**Controles necessários:**
```
- Label com versão
- Label com desenvolvedor
- Label com descrição
- Button "OK"
```

### Fase 5: Testes (⏳ FUTURO)

```csharp
// Exemplo de teste unitário
[TestClass]
public class PrinterServiceTests
{
    [TestMethod]
    public async Task InstallPrinter_ValidPrinter_ReturnsTrue()
    {
        // Arrange
        var service = new PrinterService();
        var printer = new Printer { Name = "Test", ... };
        
        // Act
        var result = await service.InstallPrinter(printer);
        
        // Assert
        Assert.IsTrue(result);
    }
}
```

## Checkpoints de Compilação

Após cada fase, o projeto deve:
- ✅ Compilar sem erros
- ✅ Compilar sem warnings
- ✅ Não ter referências circulares
- ✅ Seguir padrão de nomenclatura

## Boas Práticas

1. **Logging**: Toda operação importante deve ser logada
2. **Validação**: Validar entrada em cada método público
3. **Async/Await**: Usar Task para operações longas
4. **Documentação**: Manter XML comments atualizados
5. **Tratamento de Erro**: Try/catch em pontos críticos
6. **Limpeza de Recursos**: Usar using para streams, conexões

## Dependências NuGet

```xml
<!-- Já configurado em GelitaInstaller.csproj -->
<PackageReference Include="System.Text.Json" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
```

## Recursos Úteis

### Documentação Microsoft
- [System.Diagnostics.Process](https://docs.microsoft.com/dotnet/api/system.diagnostics.process)
- [Microsoft.Win32.Registry](https://docs.microsoft.com/dotnet/api/microsoft.win32.registry)
- [System.IO](https://docs.microsoft.com/dotnet/api/system.io)
- [System.Net](https://docs.microsoft.com/dotnet/api/system.net)

### Referências de Instalação
- [PrintUI.dll](https://docs.microsoft.com/windows-hardware/drivers/print/printer-driver-isolation)
- [Epson Scan Documentation](https://www.epson.com/support)
- [PowerShell Printer Commands](https://docs.microsoft.com/powershell/module/printmanagement)

## Dúvidas Frequentes

### P: Como elevar privilégios para admin?
A: Usar `ProcessHelper.ExecuteAsAdmin()` que configura `UseShellExecute = true` e `Verb = "runas"`

### P: Como ler do registro do Windows?
A: Usar `Microsoft.Win32.Registry` classes com `HKEY_LOCAL_MACHINE` ou `HKEY_CURRENT_USER`

### P: Como executar scripts PowerShell?
A: Usar `ProcessHelper.ExecutePowerShellCommand()` ou `ProcessService.ExecuteProcessAsync()`

### P: Qual é o timeout padrão para instalações?
A: 5 minutos (300000 ms) conforme definido em `appsettings.json`

## Timeline Estimada

- Fase 2 (Helpers): 2-3 dias
- Fase 3 (Services): 5-7 dias
- Fase 4 (Forms): 3-5 dias
- Fase 5 (Testes): 2-3 dias

**Total estimado: 12-18 dias de desenvolvimento**

## Próximos Passos

1. Revisar esta documentação com a equipe
2. Iniciar Fase 2 (Helpers)
3. Criar testes conforme implementa
4. Manter comunicação com Service Desk
5. Coletar feedback para ajustes

---

Última atualização: 2024
Versão: 1.0
