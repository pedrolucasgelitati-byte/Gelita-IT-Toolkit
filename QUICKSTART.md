# Quick Start - Gelita IT Toolkit

## 🚀 Início Rápido

### Requisitos do Sistema
- Windows 10 ou 11 (x64)
- Visual Studio 2022 (Community, Professional ou Enterprise)
- .NET 8.0 SDK instalado
- Privilégios de administrador para executar

### Passo 1: Abrir o Projeto

1. Abra Visual Studio 2022
2. Clique em **File > Open > Project/Solution**
3. Navegue até o arquivo `Gelita-IT-Toolkit.csproj`
4. Clique em **Open**

### Passo 2: Restaurar Dependências

O Visual Studio restaurará automaticamente os pacotes NuGet. Se necessário:

1. **Tools > NuGet Package Manager > Package Manager Console**
2. Execute: `Update-Package`

### Passo 3: Compilar o Projeto

1. **Build > Build Solution** (ou `Ctrl+Shift+B`)
2. Aguarde até que compile com sucesso

### Passo 4: Explorar a Estrutura

```
📁 Models/           → Veja as classes de dados
📁 Services/         → Veja os stubs dos serviços
📁 Helpers/          → Veja as classes auxiliares
📁 Forms/            → Veja os formulários
📁 Config/           → Veja as configurações JSON
```

## 📚 Documentação Importante

Leia nesta ordem:

1. **README.md** - Visão geral do projeto
2. **ARCHITECTURE.md** - Design e estrutura
3. **DEVELOPMENT.md** - Guia de implementação
4. **STRUCTURE.md** - Detalhes de organização

## 🔧 Próximas Ações

### Se você é desenvolvedor backend:
1. Comece com **Fase 2** em DEVELOPMENT.md (Helpers)
2. Implemente `FileHelper.cs` primeiro
3. Depois implemente os demais helpers
4. Em seguida, implemente os Services

### Se você é desenvolvedor frontend:
1. Comece com **Fase 4** em DEVELOPMENT.md (Forms)
2. Implemente `MainForm.cs` primeiro
3. Adicione os controles necessários
4. Crie eventos básicos (placeholder)

### Se você é arquiteto/líder:
1. Revise **ARCHITECTURE.md**
2. Revise **DEVELOPMENT.md**
3. Ajuste o timeline conforme necessário
4. Distribua tarefas aos desenvolvedores

## 📝 Estrutura de Arquivos Rápida

```
├── Assets/              ← Instadores e recursos (a serem adicionados)
├── Config/              ← JSONs de configuração (exemplos inclusos)
├── Models/              ← Classes de dados (4 arquivos - PRONTO)
├── Services/            ← Lógica de negócio (8 arquivos - STUBS)
├── Helpers/             ← Auxiliares (5 arquivos - STUBS)
├── Forms/               ← Interface (4 arquivos - STUBS)
├── Logs/                ← Arquivos de log (criados em runtime)
└── Config/              ← Arquivo .csproj configurado (PRONTO)
```

## ✅ Checklist de Setup

- [ ] Visual Studio 2022 instalado
- [ ] .NET 8.0 SDK instalado
- [ ] Projeto aberto em VS
- [ ] Compilação sem erros
- [ ] Leitura de README.md
- [ ] Leitura de ARCHITECTURE.md

## 🎯 Objetivos Iniciais

**Semana 1:**
- Entender a arquitetura (1 dia)
- Implementar Helpers (3 dias)
- Fazer commit de código (1 dia)
- Code review (1 dia)

**Semana 2:**
- Implementar Services (4 dias)
- Fazer commit e testes (1 dia)
- Code review (1 dia)

**Semana 3:**
- Implementar Forms (3 dias)
- Integração completa (1 dia)
- Testes e ajustes (1 dia)
- Deploy e documentação (1 dia)

## 💡 Dicas de Desenvolvimento

### Compilação Rápida
```
Ctrl+Shift+B = Build Solution
Ctrl+B = Build Project
```

### Debug
```
F5 = Iniciar com Debug
Ctrl+F5 = Iniciar sem Debug
F10 = Step over
F11 = Step into
```

### Navegação
```
Ctrl+, = Go to All (encontrar arquivos, tipos, símbolos)
F12 = Go to Definition
Ctrl+- = Navigate back
Ctrl+Shift+- = Navigate forward
```

### IntelliSense
```
Ctrl+Space = Show IntelliSense
Ctrl+Shift+Space = Show Parameter Info
```

## 🐛 Troubleshooting

### "Compilation failed"
1. Certifique-se de que .NET 8 SDK está instalado
2. Execute: `dotnet --version`
3. Se necessário, reinstale .NET 8 SDK

### "NuGet packages missing"
1. **Tools > NuGet Package Manager > Manage NuGet Packages for Solution**
2. Clique em "Restore"

### "IntelliSense not working"
1. **Edit > IntelliSense > Rescan Solution**
2. Se necessário, feche e reabra o VS

## 📞 Referências Rápidas

### Documentação
- [Microsoft Docs .NET 8](https://docs.microsoft.com/dotnet/core)
- [Windows Forms Documentation](https://docs.microsoft.com/dotnet/desktop/winforms)
- [System.Diagnostics](https://docs.microsoft.com/dotnet/api/system.diagnostics)
- [Registry Access](https://docs.microsoft.com/dotnet/api/microsoft.win32.registry)

### Projetos de Exemplo
- Gelita IT Toolkit (este projeto)
- Outros projetos internos da Gelita

## 🎓 Aprendizado

Caso seja novo em C# ou Windows Forms:

1. Assista tutorials de C# básico
2. Leia documentação Microsoft
3. Comece implementando `FileHelper`
4. Progrida para helpers mais complexos
5. Então vá para Services

## ✨ Próximos Passos

1. **Hoje**: Abrir projeto e compilar
2. **Amanhã**: Ler arquivos de documentação
3. **Próxima semana**: Começar desenvolvimento em Fase 2

## 📊 Status Atual

```
Fase 1: Setup ✅ (100%)
├─ Estrutura ✅
├─ Models ✅
├─ Services (stubs) ✅
├─ Helpers (stubs) ✅
├─ Forms (stubs) ✅
├─ Config JSONs ✅
└─ Documentação ✅

Fase 2: Helpers ⏳ (0%)
Fase 3: Services ⏳ (0%)
Fase 4: Forms ⏳ (0%)
Fase 5: Testes ⏳ (0%)
```

---

Pronto para começar? 🚀

Qualquer dúvida, consulte:
- DEVELOPMENT.md (guia completo)
- ARCHITECTURE.md (design patterns)
- Código comentado nas classes
