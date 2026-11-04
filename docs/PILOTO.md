# Piloto 1.0.7-pilot.1

Esta versão é para validação em uma máquina piloto Windows x64. A compilação
e os testes automatizados não substituem os testes de instalação e de login.
Não iniciar distribuição em massa antes de preencher esta lista.

## Preparação

1. Extrair o ZIP inteiro em uma pasta local gravável, fora do OneDrive e de pastas protegidas.
2. Preservar a versão anterior e uma cópia de `Config`, `.env` e da configuração
   `C:\ProgramData\EPSON\Epson Scan 2\Connection\ConnectInfo.dat`, se existente.
3. O pacote é autossuficiente: não exige SDK ou runtime .NET instalado separadamente.
4. O pacote público contém inventário vazio. Copiar os arquivos internos de configuração
   por canal corporativo e preencher `.env` a partir de `.env.example`.
5. Os instaladores de terceiros são fornecidos separadamente em `Assets/EpsonScan2`,
   `Assets/NAPS` etc. Os pacotes Epson esperados são
   `WFC5810_C5890_EScan2_67810_AM.exe` e `WFM5899_EScan2_67810_AM.exe`.
   O Toolkit confere SHA-256 antes de executá-los.
6. Não há certificado de assinatura disponível nesta preparação. O pacote de piloto
   não é assinado; verificar o SHA-256 e seguir as políticas corporativas de execução.
   Não desabilitar proteções do Windows para abrir o aplicativo.

## Validação na máquina piloto — pendente

- [ ] Abrir como usuário comum: a área administrativa não deve ficar disponível sem elevação.
- [ ] Solicitar acesso administrativo e cancelar o UAC: o aplicativo deve continuar utilizável.
- [ ] Elevar com conta autorizada: confirmar as permissões e a identidade exibida.
- [ ] Instalar os dois pacotes Epson em uma máquina sem os drivers. Confirmar ausência
      de etapas do assistente, término dos processos, drivers presentes e Epson Scan 2 abrindo.
- [ ] Reiniciar se o instalador solicitar. Confirmar operação após o reinício.
- [ ] Adicionar um scanner pelo IP e digitalizar uma página real.
- [ ] Configurar CitrixBRInternal somente, CitrixEB somente e ambas. Reabrir o Workspace
      após cada caso, conferir nomes, ausência de duplicatas e remoção somente das desmarcadas.
- [ ] Confirmar que outra loja Citrix fora das gerenciadas permanece intacta.
- [ ] Fazer login e abrir um aplicativo publicado em cada loja.
- [ ] Conferir a identidade da sessão: HKCU pertence à conta que executa o Toolkit.
      Se a elevação usar outra conta, a configuração administrativa do Citrix afeta essa
      outra conta. A área comum adiciona lojas à sessão do usuário; a seleção múltipla
      e remoção estão na área administrativa nesta versão.
- [ ] Confirmar notificação no canto inferior direito, fechamento manual e desaparecimento
      após 7 segundos para sucesso e 12 segundos para falhas/avisos.
- [ ] Testar ao menos uma impressora, exportação de logs e execução sem rede.
- [ ] Registrar modelo da máquina, versão do Windows, contas testadas, resultado e evidências.

## Retorno à versão anterior

Fechar o Toolkit e voltar à pasta preservada da versão anterior. Restaurar `Config`
e `.env` se necessário. Isso reverte o aplicativo, mas não desfaz alterações de
drivers, impressoras ou Citrix já aplicadas. Para esses componentes, usar os
procedimentos corporativos de restauração e as cópias de configuração preservadas.
Não desinstalar drivers de outras pessoas ou substituir o registro inteiro.

## Compilar, testar e empacotar

Em Windows com SDK .NET 8 ou superior:

```powershell
dotnet restore Gelita-IT-Toolkit.csproj
dotnet run --project Tests/PilotChecks/PilotChecks.csproj --configuration Release -- .
powershell -File Scripts/Publish-Pilot.ps1
```

Para criar apenas localmente um pacote interno, com configuração e instaladores
Epson/NAPS presentes neste computador:

```powershell
powershell -File Scripts/Publish-Pilot.ps1 -IncludeLocalConfiguration -IncludeLocalInstallers
```

Nunca anexar o pacote `internal` ao GitHub público. Tokens SentinelOne, certificados
privados e instaladores de segurança não são incluídos por esse script.

## Integração com main

A base desta versão local divergiu da main, que já contém portal, telemetria e
outra organização de serviços. Esta branch preserva a implementação local para
revisão. Não substituir a main nem considerar o merge pronto sem reconciliar
essas funcionalidades e os testes existentes na main.
