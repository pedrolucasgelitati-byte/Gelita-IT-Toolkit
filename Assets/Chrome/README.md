# Google Chrome

A opção **Google Chrome (64 bits)** da aba **Instalações** utiliza o pacote MSI oficial, disponível em:

https://chromeenterprise.google/download/

Mantenha o instalador ao lado deste arquivo, com o nome:

`googlechromestandaloneenterprise64.msi`

O pacote local é copiado para a pasta de saída junto com os demais recursos. O MSI não é versionado no Git devido ao tamanho; ao preparar uma cópia do repositório, obtenha o instalador oficial e verifique sua assinatura digital (Google LLC).

A instalação utiliza `msiexec /i <pacote> /qn /norestart`, solicita elevação pelo Windows e não reinicia o computador automaticamente. Os códigos 0 e 3010 (sucesso com reinicialização pendente) são aceitos como sucesso.

O Toolkit verifica o SHA-256 antes de executar. SHA-256 do pacote validado:

`ECF1B28BF4E8E8B3434A844EB915118935E6525768EFE3DBE622C2A20CBA9EC9`

Para atualizar o pacote, baixe novamente do site oficial, confirme a assinatura Google LLC e atualize a constante `ChromeSha256` em `Forms/MainForm.cs` com o hash do novo MSI antes de compilar. Não remova a verificação de integridade.
