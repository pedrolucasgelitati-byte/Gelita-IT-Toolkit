# Evidências de preparação do piloto

Data: 4 de novembro de 2026.

## Executado

- Compilação Windows .NET 8: sem erros; 92 avisos do projeto permanecem.
- 32 verificações locais: integridade de payloads, limites de diretórios, autorização
  Windows, elevação via UAC, persistência de configuração de scanner, URLs e aliases
  Citrix, comportamento da notificação e estrutura dos dois pacotes Epson.
- Hashes SHA-256 dos dois pacotes Epson conferidos. Ambos contêm driver INF e um
  Setup.exe que reconhece `/NODISP`. Nenhum driver foi instalado por esses testes.
- Gitleaks 8.30.1: 26 commits de todas as referências locais, sem detecções.
- A cópia preparada do código também passou no Gitleaks. O valor do token local
  conhecido foi comparado, sem exibição, com o conteúdo preparado e com os diffs
  do histórico disponível; nenhuma correspondência foi encontrada.
- Auditoria NuGet, incluindo dependências transitivas: nenhuma vulnerabilidade
  conhecida reportada pelas fontes consultadas nesta execução.
- Arquivos de token, artefatos de validação, snapshots de checkout, instaladores
  e inventário local retirados do índice, com cópias locais preservadas.
- Uma instalação sem templates também inicia com inventário vazio. Foram removidos
  os inventários fictícios/corporativos embutidos e os avisos modais de inicialização vazia.

Os testes com payloads Epson são opcionais em CI porque esses arquivos não são
publicados no código-fonte. Os demais testes executam sem instalar drivers ou
alterar contas Citrix.

## Limites

Não foi executada instalação real dos dois drivers, digitalização, autenticação
Citrix, abertura de aplicativos publicados, reinício ou validação interativa do
UAC. A lista de aceite está em [PILOTO.md](PILOTO.md).

A varredura automática não prova ausência de todo segredo. Ela cobre as regras
conhecidas do scanner e as referências disponíveis localmente. Dados de
infraestrutura podem existir em versões já publicadas do histórico; esta
preparação não reescreve nem força atualização do histórico remoto.

Não há certificado de assinatura de código no armazenamento pessoal deste
computador. A distribuição criada é uma prévia sem assinatura, com checksums.

A branch local divergiu da main. A revisão de integração deve preservar portal,
telemetria e a arquitetura presente na main antes de promover a versão.
