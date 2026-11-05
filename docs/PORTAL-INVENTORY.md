# Inventário no portal

Para hospedagem gratuita somente no GitHub, use [GITHUB-INVENTORY.md](GITHUB-INVENTORY.md).
As instruções abaixo se aplicam ao backend ASP.NET, que requer servidor próprio.

O Toolkit envia um resumo ao iniciar e a cada cinco minutos enquanto aberto.
Não instala serviço nem tarefa agendada e não altera o agente Snow.

Configure no `.env` ao lado do executável ou nas variáveis de ambiente do Windows:

```dotenv
GELITA_TOOLKIT_PORTAL_URL=https://ENDERECO-DO-PORTAL
GELITA_TOOLKIT_AGENT_KEY=CHAVE-DE-CADASTRO
GELITA_TOOLKIT_UNIT=UNIDADE
```

A URL deve ser a raiz HTTPS do portal. A chave precisa ser igual a
`Portal__AgentEnrollmentKey` no servidor. A unidade é opcional.
Não publique a chave no Git. Sem URL ou chave o envio fica desativado.
O ZIP baixado pelo portal já recebe URL e chave no `.env` quando o servidor
está configurado corretamente; distribua uma compilação que inclua este serviço.

Dados enviados: nome da máquina, versão do Toolkit, conta Windows que executa o
processo, unidade configurada, descrição do Windows e presença do SentinelOne.
O resultado de SentinelOne indica instalação, não saúde da proteção.
Uma sessão elevada pode informar a conta administrativa usada para abrir o app.
Não são enviados documentos, senhas, dumps ou arquivos do Snow.

O envio usa `POST /api/v1/heartbeat`, com chave Bearer, timeout de 20 segundos,
sem seguir redirecionamentos. Falhas não bloqueiam a interface; a tentativa é
repetida no próximo intervalo. O último status fica em
`%LOCALAPPDATA%\GelitaITToolkit\portal-status.log`.

Após iniciar uma compilação atualizada com a configuração válida, confira o log
e a máquina no painel autenticado do portal. Ao fechar o app o envio termina;
o painel deixa de contar a máquina como online após 15 minutos sem contato.
Esse indicador representa contato do Toolkit, não disponibilidade geral da máquina.
