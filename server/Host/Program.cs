using System.Text;
using System.Globalization;
using System.IO.Compression;
using Dwsg.Host;
using Dwsg.Persistence;
using Dwsg.Runtime;
using Dwsg.Shared;
using Dwsg.Server.Economy;
using Dwsg.Server.World;
using Dwsg.Server.Modules.Generals;
using Dwsg.Server.Chat;
using Dwsg.Server.Modules.Combat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.ResponseCompression;

var database = Environment.GetEnvironmentVariable("DWSG_WORLD_DB") ?? throw new InvalidOperationException("DWSG_WORLD_DB is required");
var worldId = Environment.GetEnvironmentVariable("DWSG_WORLD_ID") ?? "main";
if (string.IsNullOrWhiteSpace(worldId) || worldId.Length > 128) throw new InvalidOperationException("Invalid world ID");
if (args.Length == 2 && args[0] == "--backup")
{
    if (!File.Exists(database)) throw new InvalidOperationException("Backup source does not exist");
    using var backupStore = new SqliteWorldStore(database);
    if (backupStore.Load(worldId) == null) throw new InvalidOperationException("Backup world does not exist");
    backupStore.BackupTo(args[1]);
    Console.WriteLine("SQLite world backup completed.");
    return;
}
var maxOnlinePlayers = 5;
var configuredMaxOnlinePlayers = Environment.GetEnvironmentVariable("DWSG_MAX_ONLINE_PLAYERS");
if (configuredMaxOnlinePlayers != null &&
    (!int.TryParse(configuredMaxOnlinePlayers, NumberStyles.None, CultureInfo.InvariantCulture, out maxOnlinePlayers) || maxOnlinePlayers <= 0))
    throw new InvalidOperationException("DWSG_MAX_ONLINE_PLAYERS must be a positive Int32.");
var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 65536);
builder.Services.AddResponseCompression(options => options.Providers.Add<GzipCompressionProvider>());
builder.Services.Configure<GzipCompressionProviderOptions>(options => options.Level = CompressionLevel.Fastest);
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options => { options.SingleLine = true; options.TimestampFormat = "HH:mm:ss "; });
var authUrl = Environment.GetEnvironmentVariable("DWSG_AUTH_URL") ?? throw new InvalidOperationException("DWSG_AUTH_URL is required");
using var store = new SqliteWorldStore(database);
if (store.Load(worldId) == null)
{
    var seedPath = Environment.GetEnvironmentVariable("DWSG_WORLD_SEED") ?? throw new InvalidOperationException("Initial world requires DWSG_WORLD_SEED");
    var data = JObject.Parse(File.ReadAllText(seedPath, Encoding.UTF8));
    foreach (var key in new[] { "玩家列表", "国家列表", "城池列表", "商城商品", "道具配置" })
        if (!(data[key] is JArray || data[key] is JObject)) throw new InvalidDataException("Original world seed is incomplete");
    var mappings = new JObject();
    foreach (var pair in new[] { ("players", "玩家列表"), ("cities", "城池列表") })
    {
        var map = new JObject();
        for (var i = 0; i < ((JArray)data[pair.Item2]).Count; i++) map[Guid.NewGuid().ToString("N")] = i;
        mappings[pair.Item1] = map;
    }
    var imported = new WorldState { WorldId = worldId, Data = data, EntityMappings = mappings };
    StableNationIds.EnsureMappings(imported);
    GeneralsModule.EnsureMappings(imported);
    store.ImportWorld(imported);
}
WorldRoleRegistration.Initialize(store, worldId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), PrepareNations);
using var authentication = new PhpAuthentication(new Uri(authUrl, UriKind.Absolute));
var leaseMs = 30000L;
if (long.TryParse(Environment.GetEnvironmentVariable("DWSG_SESSION_LEASE_MS"), out var configuredLease) && configuredLease >= 1000 && configuredLease <= 300000)
    leaseMs = configuredLease;
var sessions = new GameSessions(authentication, new AuthorizedWorldProjection(), LegacyWorldModule.CreatePlayer, worldId, leaseMs, maxOnlinePlayers);
var runtime = new WorldRuntime(store, sessions.Authorize, initializeEntities: GeneralsModule.EnsureMappings,
    preparePlayer: (state, playerId, now) => {
        ProductionModule.InitializePlayer(state, playerId, now);
        NationModule.InitializeSalary(state, playerId, now);
        Dwsg.Server.Progress.ProgressModule.RefreshPlayer(state, playerId, now);
        Dwsg.Server.Auxiliary.AuxiliaryModule.InitializePlayer(state, playerId, now);
    }, prepareCommit: StableNationIds.EnsureMappings,
    prepareTimedCommit: Dwsg.Server.Progress.ProgressModule.RefreshAll);
sessions.Runtime = runtime;
runtime.Register(new EconomyModule(GeneralsModule.EnsureMappings));
runtime.Register(new ProductionModule());
runtime.Register(new TerritoryModule(GeneralsModule.EnsureMappings));
runtime.Register(new MarketModule());
runtime.Register(new TechnologyModule());
runtime.Register(new NationModule(state => StableNationIds.RegisterCreated(state, state.Data.Value<int>("国家ID记录"))));
runtime.Register(new GeneralsModule());
runtime.Register(new ChatModule());
runtime.Register(new CombatModule());
runtime.Register(new Dwsg.Server.Social.SocialModule());
runtime.Register(new Dwsg.Server.Administration.AdministrationModule());
runtime.Register(new Dwsg.Server.Progress.ProgressModule());
runtime.Register(new Dwsg.Server.Progress.TrainingModule());
runtime.Register(new Dwsg.Server.Auxiliary.AuxiliaryModule());
runtime.Committed += sessions.Publish;
var app = builder.Build();
app.UseWhen(context => context.Request.Path == "/connect", branch => branch.UseResponseCompression());
app.MapGet("/health", () => Results.Json(new { protocolVersion = 1, worldId }));
app.MapPost("/connect", ConnectRequest);
app.MapPost("/command", CommandRequest);
app.MapPost("/poll", PollRequest);
app.MapPost("/disconnect", DisconnectRequest);
app.Lifetime.ApplicationStarted.Register(() => _ = TickAsync(app.Lifetime.ApplicationStopping));
await app.RunAsync();

// The administrator pins an already qualified import proof. No current array index is evidence.
void PrepareNations(WorldState state)
{
    if (state.EntityMappings["nationMappingVersion"] != null)
    {
        StableNationIds.EnsureMappings(state);
        return;
    }
    var path = Environment.GetEnvironmentVariable("DWSG_NATION_IMPORT_PROOF")
        ?? throw new InvalidDataException("Legacy nation mappings require a qualified original import proof");
    var expectedHash = Environment.GetEnvironmentVariable("DWSG_NATION_IMPORT_PROOF_SHA256");
    var bytes = File.ReadAllBytes(path);
    if (string.IsNullOrWhiteSpace(expectedHash) || !Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes))
        .Equals(expectedHash, StringComparison.OrdinalIgnoreCase))
        throw new InvalidDataException("Original nation import proof hash mismatch");
    var proof = JObject.Parse(Encoding.UTF8.GetString(bytes));
    if (proof.Value<string>("worldId") != state.WorldId)
        throw new InvalidDataException("Original nation import proof belongs to another world");
    StableNationIds.MigrateLegacy(state, proof["verifiedLegacyIdsByGuid"] as JObject);
}

Task ConnectRequest(HttpContext context) => Serve(context, body => sessions.ConnectAsync(body, context.RequestAborted));
Task CommandRequest(HttpContext context) => Serve(context, body => sessions.CommandAsync(context.Request.Headers["X-Dwsg-Connection"].ToString(), body, context.RequestAborted));
Task PollRequest(HttpContext context) => Serve(context, body => sessions.PollAsync(context.Request.Headers["X-Dwsg-Connection"].ToString(), body, context.RequestAborted));
Task DisconnectRequest(HttpContext context) => Serve(context, body => Task.FromResult(sessions.Disconnect(context.Request.Headers["X-Dwsg-Connection"].ToString())));

async Task TickAsync(CancellationToken stop)
{
    using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(100));
    try
    {
        while (await timer.WaitForNextTickAsync(stop))
            try { runtime.Tick(worldId); }
            catch (Exception ex) { Console.Error.WriteLine("World tick failed: " + ex.GetType().Name); }
    }
    catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
}

static async Task Serve(HttpContext context, Func<JObject, Task<JObject>> action)
{
    JObject response;
    try
    {
        using var reader = new StreamReader(context.Request.Body, Encoding.UTF8);
        var text = await reader.ReadToEndAsync(context.RequestAborted);
        if (text.Length > 65536) throw new JsonException("Request too large");
        response = await action(JObject.Parse(text));
    }
    catch (PhpSessionRejected ex) { response = Rejected(GameCodes.Unauthenticated, ex.Message); }
    catch (JsonException) { response = Rejected(GameCodes.InvalidArgument, "请求格式无效。"); }
    catch (ArgumentException) { response = Rejected(GameCodes.InvalidArgument, "请求参数无效。"); }
    catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { return; }
    catch (Exception ex)
    {
        Console.Error.WriteLine("Game request failed: " + ex.GetType().Name);
        response = Rejected(GameCodes.Unavailable, "服务器暂时不可用，请使用同一请求ID重试。");
    }
    context.Response.ContentType = "application/json; charset=utf-8";
    await context.Response.WriteAsync(response.ToString(Formatting.None), context.RequestAborted);
}
static JObject Rejected(string code, string message) => new JObject { ["result"] = JObject.FromObject(GameResult.Reject(code, message)) };
