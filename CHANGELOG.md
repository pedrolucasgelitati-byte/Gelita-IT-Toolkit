# Histórico de versões

## 1.0.2 — 2026-07-30

- Configuração e validação de scanners Epson Scan 2 e perfis NAPS2 para usuários locais/AD.
- Configuração conjunta das contas CitrixBR (principal) e CitrixEB.
- Central de reparos, diagnóstico de segurança, backup e testes de conectividade.
- Atualização assistida do Windows 11 24H2 para 25H2 com validação SHA-256.
- Atualizador automático do Toolkit com backup, reinício e restauração em caso de falha.
- Interface de Ferramentas reorganizada e inicialização otimizada.
- Novo ícone com cantos arredondados e transparência.
- Proteção de dados sensíveis nos logs e validação ampliada dos arquivos JSON.

## 1.0.1 — 2026-07-29

- Corrige a configuração do primeiro scanner em máquinas sem `ConnectInfo.dat`.
- Aplica a preferência do Epson Scan 2 aos perfis locais existentes e ao perfil padrão do Windows.
- Adiciona automaticamente os perfis ao NAPS2 de todos os usuários que já possuem perfil na máquina.
- Prepara Epson Scan 2 e NAPS2 para novos usuários de domínio no primeiro logon.

## 1.0.0 — 2026-07-29

Primeira versão pública portátil do Gelita IT Toolkit.

- Dashboard de sistema, hardware e rede.
- Instalação e gerenciamento de impressoras por unidade.
- Configuração de múltiplos scanners Epson e perfis NAPS2.
- Instalações corporativas com validação de integridade.
- Central de reparos do Windows.
- Configurações externas em JSON com validação automática.
- Barra de progresso, estados de instalação, pesquisa e execução em lote.
- Logs e histórico persistente.
