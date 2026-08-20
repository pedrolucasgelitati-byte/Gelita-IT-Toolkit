# Gelita Toolkit Portal

Portal interno para inventário de máquinas e download autenticado do Toolkit.

## Recursos

- Login corporativo Microsoft Entra ID.
- Restrição ao tenant configurado e aos domínios `gelita.com` e
  `gelita-subcontractors.com`.
- Inventário com máquina, usuário, unidade, Windows, versão e SentinelOne.
- Pesquisa e indicadores de máquinas online, desatualizadas e com alertas.
- Download autenticado do pacote com `.env` gerado em memória.
- API de heartbeat protegida por chave e limite de requisições.
- Layout responsivo com identidade visual GELITA IT.

## Configuração obrigatória

Registre um aplicativo Web no Microsoft Entra ID e configure o redirect URI
`https://SEU-HOST/signin-oidc`. Defina os valores abaixo por variáveis de ambiente
ou por um cofre de segredos; não grave valores reais no `appsettings.json`:

```text
AzureAd__TenantId
AzureAd__ClientId
AzureAd__ClientSecret
Portal__AgentEnrollmentKey
Portal__EnvironmentVariables__GELITA_TOOLKIT_AGENT_KEY
Portal__EnvironmentVariables__GELITA_TOOLKIT_PORTAL_URL
Portal__PackagePath
```

`Portal__AgentEnrollmentKey` e `GELITA_TOOLKIT_AGENT_KEY` devem ter o mesmo valor,
gerado aleatoriamente (recomendado: 32 bytes ou mais). Coloque o ZIP sem `.env` no caminho configurado em
`Portal__PackagePath`. O portal cria uma cópia em memória e inclui o `.env` somente
para usuários autenticados no momento do download.

O tenant configurado é a fronteira primária de segurança. O portal também rejeita
logins cujo nome principal não termine em `gelita.com` ou
`gelita-subcontractors.com`.

## Execução local

```powershell
dotnet restore
dotnet run --urls https://localhost:7085
```

Sem uma configuração Entra real, use `/preview` somente em `Development` para
visualizar a interface. Essa rota não é criada em produção.

## Produção

- Hospede atrás de HTTPS em IIS, Azure App Service ou contêiner corporativo.
- Armazene `ClientSecret` e chaves em variáveis protegidas ou Azure Key Vault.
- Restrinja o acesso de rede quando o portal for exclusivamente interno.
- Garanta permissão de escrita apenas para a identidade do portal em `App_Data`.
- Use armazenamento SQL antes de executar mais de uma instância do portal.
