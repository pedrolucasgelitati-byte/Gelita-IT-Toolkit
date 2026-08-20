using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Gelita.Toolkit.Portal.Models;
using Gelita.Toolkit.Portal.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
var allowedDomains = builder.Configuration.GetSection("Portal:AllowedDomains").Get<string[]>()
    ?? ["gelita.com", "gelita-subcontractors.com"];
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.Cookie.Name = "__Host-GelitaToolkitPortal";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
})
.AddOpenIdConnect(options =>
{
    var tenantId = builder.Configuration["AzureAd:TenantId"]
        ?? throw new InvalidOperationException("AzureAd:TenantId não foi configurado.");
    options.Authority = $"https://login.microsoftonline.com/{tenantId}/v2.0";
    options.ClientId = builder.Configuration["AzureAd:ClientId"]
        ?? throw new InvalidOperationException("AzureAd:ClientId não foi configurado.");
    options.ClientSecret = builder.Configuration["AzureAd:ClientSecret"];
    options.ResponseType = "code";
    options.CallbackPath = "/signin-oidc";
    options.SignedOutCallbackPath = "/signout-callback-oidc";
    options.SaveTokens = false;
    options.GetClaimsFromUserInfoEndpoint = false;
    options.Scope.Clear();
    options.Scope.Add("openid");
    options.Scope.Add("profile");
    options.Scope.Add("email");
    options.Events.OnTokenValidated = context =>
    {
        var tenant = context.Principal?.FindFirstValue("tid");
        var login = context.Principal?.FindFirstValue("preferred_username")
            ?? context.Principal?.FindFirstValue(ClaimTypes.Upn)
            ?? context.Principal?.FindFirstValue(ClaimTypes.Email);
        var domain = login?.Split('@').LastOrDefault();
        if (!string.Equals(tenant, tenantId, StringComparison.OrdinalIgnoreCase) ||
            domain == null || !allowedDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
            context.Fail("Esta conta não pertence a um domínio autorizado da GELITA.");
        return Task.CompletedTask;
    };
});

var keyDirectory = new DirectoryInfo(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "DataProtection"));
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("Gelita.Toolkit.Portal");
if (builder.Environment.IsDevelopment())
    dataProtection.UseEphemeralDataProtectionProvider();
else
    dataProtection.PersistKeysToFileSystem(keyDirectory);

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
});
builder.Services.AddSingleton<MachineStore>();
builder.Services.AddSingleton<PackageDownloadService>();
builder.Services.AddRateLimiter(options => options.AddPolicy("agents", context =>
    RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 120,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        })));

var app = builder.Build();
app.UseExceptionHandler("/error");
app.UseHsts();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/login", () => Results.Challenge(new AuthenticationProperties { RedirectUri = "/" }, [OpenIdConnectDefaults.AuthenticationScheme])).AllowAnonymous();
app.MapGet("/logout", () => Results.SignOut(new AuthenticationProperties { RedirectUri = "/" }, [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]));

app.MapGet("/", async (ClaimsPrincipal user, MachineStore store, CancellationToken token) =>
{
    var machines = await store.GetAllAsync(token);
    return Results.Content(Dashboard(user, machines), "text/html; charset=utf-8");
});

app.MapGet("/download", async (PackageDownloadService packages, CancellationToken token) =>
{
    var package = await packages.BuildAsync(token);
    return Results.File(package.Content, "application/zip", package.FileName);
});

app.MapGet("/api/v1/machines", (MachineStore store, CancellationToken token) => store.GetAllAsync(token));

app.MapPost("/api/v1/heartbeat", async (HttpRequest request, MachineHeartbeat heartbeat, MachineStore store, IConfiguration configuration, CancellationToken token) =>
{
    var expected = configuration["Portal:AgentEnrollmentKey"];
    var supplied = request.Headers.Authorization.ToString();
    if (string.IsNullOrWhiteSpace(expected) || !FixedEquals(supplied, "Bearer " + expected))
        return Results.Unauthorized();
    if (string.IsNullOrWhiteSpace(heartbeat.MachineName) || heartbeat.MachineName.Length > 63 ||
        string.IsNullOrWhiteSpace(heartbeat.Version) || heartbeat.Version.Length > 32 ||
        IsTooLong(heartbeat.UserName, 128) || IsTooLong(heartbeat.Unit, 128) ||
        IsTooLong(heartbeat.OperatingSystem, 256))
        return Results.BadRequest(new { error = "MachineName ou Version inválido." });
    await store.UpsertAsync(heartbeat, token);
    return Results.NoContent();
}).AllowAnonymous().RequireRateLimiting("agents");

app.MapGet("/error", () => Results.Content(Page("Erro", "<section class=\"panel\"><h2>Não foi possível concluir a solicitação.</h2><p>Verifique a configuração do portal ou tente novamente.</p></section>"), "text/html; charset=utf-8")).AllowAnonymous();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/preview", async (MachineStore store, CancellationToken token) => Results.Content(
        Dashboard(new ClaimsPrincipal(), await store.GetAllAsync(token), preview: true),
        "text/html; charset=utf-8")).AllowAnonymous();
    app.MapGet("/preview-download", async (PackageDownloadService packages, CancellationToken token) =>
    {
        var package = await packages.BuildAsync(token);
        return Results.File(package.Content, "application/zip", package.FileName);
    }).AllowAnonymous();
}

app.Run();

static string H(string? value) => HtmlEncoder.Default.Encode(value ?? "-");
static bool FixedEquals(string supplied, string expected) =>
    System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
        System.Text.Encoding.UTF8.GetBytes(supplied), System.Text.Encoding.UTF8.GetBytes(expected));
static bool IsTooLong(string? value, int maximumLength) => value?.Length > maximumLength;
static string Dashboard(ClaimsPrincipal user, IReadOnlyList<MachineRecord> machines, bool preview = false)
{
    var now = DateTimeOffset.UtcNow;
    var online = machines.Count(machine => now - machine.LastSeenUtc < TimeSpan.FromMinutes(15));
    var outdated = machines.Count == 0 ? 0 : machines.Count - machines.GroupBy(machine => machine.Version)
        .OrderByDescending(group => Version.TryParse(group.Key, out var parsed) ? parsed : new Version()).First().Count();
    var sentinelAlerts = machines.Count(machine => machine.SentinelOneInstalled == false);
    var rows = string.Join("", machines.Select(machine =>
    {
        var isOnline = now - machine.LastSeenUtc < TimeSpan.FromMinutes(15);
        var sentinelClass = machine.SentinelOneInstalled == false ? "badge danger" : "badge ok";
        var sentinelText = machine.SentinelOneInstalled == true ? "Ativo" : machine.SentinelOneInstalled == false ? "Ausente" : "Não informado";
        return $"""
        <tr data-machine="{H(machine.MachineName)}" data-unit="{H(machine.Unit)}" data-version="{H(machine.Version)}">
          <td><div class="machine"><span class="presence {(isOnline ? "online" : "offline")}"></span><strong>{H(machine.MachineName)}</strong></div></td>
          <td><span class="version">v{H(machine.Version)}</span></td><td>{H(machine.UserName)}</td><td>{H(machine.Unit)}</td>
          <td>{H(machine.OperatingSystem)}</td><td><span class="{sentinelClass}">{sentinelText}</span></td>
          <td><time>{machine.LastSeenUtc.ToLocalTime():dd/MM/yyyy HH:mm}</time></td></tr>
        """;
    }));
    var displayName = user.Identity?.Name ?? "Prévia local";
    var downloadLink = preview ? "/preview-download" : "/download";
    var unitOptions = string.Join("", machines
        .Select(machine => machine.Unit?.Trim())
        .Where(unit => !string.IsNullOrWhiteSpace(unit))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(unit => unit, StringComparer.CurrentCultureIgnoreCase)
        .Select(unit => $"<option value=\"{H(unit)}\">{H(unit)}</option>"));
    return Page("Painel", $$"""
      <aside class="sidebar">
        <a class="brand" href="/"><img src="/images/gelita-it-logo.png" alt="Gelita IT"><div><strong>Toolkit</strong><span>Portal de gestão</span></div></a>
        <nav class="side-nav"><a class="active" href="/"><span>▦</span>Visão geral</a><a href="#machines"><span>▤</span>Máquinas</a><a href="{{downloadLink}}"><span>⇩</span>Download</a></nav>
        <div class="sidebar-foot"><span>Ambiente corporativo</span><strong>GELITA IT</strong></div>
      </aside>
      <main class="content">
        <header class="topbar"><button class="menu" aria-label="Abrir menu">☰</button><div><p>Central de gerenciamento</p><h1>Visão geral</h1></div><div class="user"><span class="avatar">{{H(displayName[..1].ToUpperInvariant())}}</span><div><strong>{{H(displayName)}}</strong><span>Conta corporativa</span></div><a href="/logout">Sair</a></div></header>
        {{(preview ? "<div class=\"preview-note\"><strong>Prévia visual</strong><span>Os dados reais aparecerão depois que o Toolkit conectar ao portal.</span></div>" : "")}}
        <section class="welcome"><div><span class="kicker">GELITA IT TOOLKIT</span><h2>Ambiente sob controle.</h2><p>Acompanhe versões, integridade e atividade das estações em um único lugar.</p></div><a class="primary" href="{{downloadLink}}"><span>↓</span> Baixar aplicativo</a></section>
        <section class="cards">
          <article><span class="card-icon blue">▦</span><div><small>Total monitorado</small><strong>{{machines.Count}}</strong><em>estações registradas</em></div></article>
          <article><span class="card-icon green">●</span><div><small>Online agora</small><strong>{{online}}</strong><em>últimos 15 minutos</em></div></article>
          <article><span class="card-icon amber">↻</span><div><small>Versão anterior</small><strong>{{outdated}}</strong><em>requerem atualização</em></div></article>
          <article><span class="card-icon red">!</span><div><small>Alertas SentinelOne</small><strong>{{sentinelAlerts}}</strong><em>pedem atenção</em></div></article>
        </section>
        <section class="table-panel" id="machines"><div class="panel-head"><div><span class="kicker">INVENTÁRIO</span><h3>Máquinas conectadas</h3></div><div class="inventory-filters"><label class="unit-filter"><span>Unidade</span><select id="unit-filter" aria-label="Filtrar por unidade"><option value="">Todas as unidades</option>{{unitOptions}}</select></label><label class="search"><span>⌕</span><input id="machine-search" type="search" placeholder="Buscar máquina, unidade ou versão"></label></div></div>
          <div class="table-scroll"><table><thead><tr><th>Máquina</th><th>Versão</th><th>Usuário</th><th>Unidade</th><th>Sistema</th><th>SentinelOne</th><th>Último contato</th></tr></thead>
          <tbody id="machine-rows">{{(rows.Length == 0 ? "<tr class=\"empty-row\"><td colspan=\"7\"><img src=\"/images/gelita-it-logo.png\" alt=\"\"><strong>Nenhuma máquina conectada</strong><span>Instale o Toolkit configurado para começar o inventário.</span></td></tr>" : rows)}}</tbody></table></div>
        </section><footer>GELITA IT Toolkit Portal <span>•</span> Uso interno</footer>
      </main>
    """);
}

static string Page(string title, string body) => $$"""
<!doctype html><html lang="pt-BR"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>{{H(title)}} · GELITA Toolkit</title><link rel="icon" href="/images/gelita-it-logo.png"><link rel="stylesheet" href="/css/site.css"><link rel="stylesheet" href="/css/filters.css"></head>
<body>{{body}}<script src="/js/site.js"></script></body></html>
""";
