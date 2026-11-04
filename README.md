# Gelita IT Toolkit

Ferramenta desenvolvida para automatizar tarefas do Service Desk da Gelita.

## Piloto 1.0.7-pilot.1

Esta branch contém a versão local para revisão e teste em uma máquina piloto.
Não substitui a versão estável nem deve ser distribuída em massa antes dos
testes descritos em [docs/PILOTO.md](docs/PILOTO.md).

- Citrix: seleção de múltiplas contas na área administrativa, remoção das
  desmarcadas e nomes CitrixBRInternal e CitrixEB.
- Epson: extração dos pacotes verificados e execução do Setup.exe interno com
  `/NODISP`, em vez de `/S` no pacote externo.
- Notificações compactas no canto inferior direito.
- Separação de configuração local, instaladores de terceiros e código público.

Os arquivos reais de inventário e `.env` não são publicados. Uma instalação
limpa usa os exemplos vazios em `Config`; preencher os endereços Citrix em
`.env`, usando `.env.example`. Os instaladores devem ser fornecidos por canal
corporativo nos caminhos descritos no guia do piloto.

```powershell
dotnet run --project Tests/PilotChecks/PilotChecks.csproj --configuration Release -- .
powershell -File Scripts/Publish-Pilot.ps1
```

O pacote público é portátil e inclui o runtime .NET para Windows x64. Não contém
inventário corporativo, tokens ou instaladores de terceiros. A assinatura de
código não está disponível nesta preparação. As evidências e limitações estão
em [docs/VALIDACAO-1.0.7-pilot.1.md](docs/VALIDACAO-1.0.7-pilot.1.md).

## Funcionalidades previstas

- Instalação de impressoras
- Instalação do Epson Scan 2
- Instalação do NAPS
- Configuração automática de scanners Epson
- Ferramentas de rede
- Ferramentas de Windows
- Gerenciamento de impressoras
- Logs
- Dashboard
- Configurações
- Futuras integrações
