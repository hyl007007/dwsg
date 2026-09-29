using Dwsg.Persistence;
using Dwsg.Runtime;
using Dwsg.Server.Modules.Generals;
using Dwsg.Server.World;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

if (args.Length != 2 && !(args.Length == 3 && args[2] == "--technology"))
    throw new ArgumentException("Pass actual original seed, a new audit database path, and optionally --technology.");
string db = Path.GetFullPath(args[1]);
if (!db.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Any(p => p.Equals("audit", StringComparison.OrdinalIgnoreCase)) || File.Exists(db))
    throw new ArgumentException("Use a new disposable database under audit.");
var seed = JObject.Parse(File.ReadAllText(args[0]));
int checks = 0;
void Check(bool success, string name)
{
    if (!success) throw new InvalidOperationException(name);
    checks++; Console.WriteLine("PASS " + name);
}
var world = new WorldState { WorldId = Guid.NewGuid().ToString("N"), Data = seed,
    EntityMappings = new JObject { ["players"] = new JObject(), ["humanPlayers"] = new JObject() } };
for (int i = 0; i < world.Data["玩家列表"].Count(); i++) world.EntityMappings["players"][Guid.NewGuid().ToString("N")] = i;
var bindings = new List<RoleBinding>();
for (int i = 0; i < 2; i++)
{
    Check(LegacyWorldModule.CreatePlayer(world, "领地君主" + i, "汉", 0, out int index).Code == GameCodes.Ok, "actual original role creation " + i);
    string id = Guid.NewGuid().ToString("N");
    world.EntityMappings["players"][id] = index; world.EntityMappings["humanPlayers"][id] = true;
    bindings.Add(new RoleBinding { WorldId = world.WorldId, PlayerId = id, AccountId = Guid.NewGuid().ToString("N"), LegacyPlayerIndex = index });
}
GeneralsModule.EnsureMappings(world);
if (args.Length == 3)
{
    TechnologyChecks.Run(world, bindings, db);
    return;
}
string owner = bindings[0].PlayerId;
JObject Player(WorldState state) => state.RequirePlayer(owner);
string Json(JToken token) => token.ToString(Formatting.None);
var towns = world.Data["城池列表"].OfType<JObject>().Where(city => city.Value<string>("国家") == "汉" && city.Value<int>("规模") != 4).Take(3).ToArray();
Check(towns.Length == 3, "actual original nation provides towns");
int X(int town) => towns[town].Value<int>("坐标x");
int Y(int town) => towns[town].Value<int>("坐标y");
string firstFiefId = ((JObject)world.EntityMappings["fiefs"]).Properties().Single(p => p.Value.Value<string>("playerId") == owner && p.Value.Value<int>("legacyId") == 1).Name;
var pure = world.Clone(); var purePlayer = Player(pure); var firstFief = (JObject)purePlayer["封地信息表"][0];
string fiefBefore = Json(firstFief);
string walletBefore = Json(purePlayer["财产信息"]);
Check(TerritoryRules.CreateFief(pure, owner, X(0), Y(0)).Code == GameCodes.Ok && purePlayer["封地信息表"].Count() == 2 &&
    purePlayer.Value<int>("封地ID标识") == 3 && walletBefore == Json(purePlayer["财产信息"]), "free second fief with original next ID");
var created = purePlayer["封地信息表"][1];
Check(created["建筑信息表"].Count() == 13 && created["建筑信息表"][0].Value<int>("等级") == 10 &&
    created["伤兵信息表"][0].Value<int>("ID") == 104 && created["伤兵信息表"][0].Value<double>("数量") == 1 &&
    created.Value<string>("封地名字") == towns[0].Value<string>("名称") + "封地", "original buildings wound and city fief name");
string duplicateBefore = Json(pure.Data);
Check(TerritoryRules.CreateFief(pure, owner, X(0), Y(0)).Code == GameCodes.Conflict && duplicateBefore == Json(pure.Data), "same owner cannot open second fief in city");
Check(TerritoryRules.MoveFief(pure, owner, firstFiefId, X(1), Y(1)).Code == GameCodes.Ok &&
    firstFief["所在城池"].Value<int>("x") == X(1) && firstFief["所在城池"].Value<int>("y") == Y(1), "original migration updates actual location");
var oldLocation = JObject.Parse(fiefBefore)["所在城池"];
Check(!TerritoryRules.City(pure, oldLocation.Value<int>("x"), oldLocation.Value<int>("y"))["城池封地列表"].Any(r => r.Value<int>("第几个玩家") == bindings[0].LegacyPlayerIndex && r.Value<int>("封地ID标识") == 1) &&
    TerritoryRules.City(pure, X(1), Y(1))["城池封地列表"].Count(r => r.Value<int>("第几个玩家") == bindings[0].LegacyPlayerIndex && r.Value<int>("封地ID标识") == 1) == 1,
    "migration removes source and registers destination once");
var preserved = (JObject)firstFief.DeepClone(); preserved["所在城池"] = oldLocation.DeepClone();
Check(fiefBefore == Json(preserved) && ReferenceEquals(firstFief, purePlayer["封地信息表"][0]) &&
    JToken.DeepEquals(world.EntityMappings["fiefs"][firstFiefId], pure.EntityMappings["fiefs"][firstFiefId]), "migration preserves buildings troops fief object and stable mapping");
string sameBefore = Json(pure.Data);
Check(TerritoryRules.MoveFief(pure, owner, firstFiefId, X(1), Y(1)).Code == GameCodes.Ok && sameBefore == Json(pure.Data), "same-city migration cannot duplicate registration");
Check(TerritoryRules.MoveFief(pure, bindings[1].PlayerId, firstFiefId, X(2), Y(2)).Code == GameCodes.Forbidden && sameBefore == Json(pure.Data), "other player's stable fief cannot move");
var boundary = world.Clone(); var bag = (JArray)Player(boundary)["封地信息表"];
for (int i = 2; i <= 10; i++) { var copy = (JObject)bag[0].DeepClone(); copy["ID"] = i; bag.Add(copy); }
Player(boundary)["封地ID标识"] = 11;
Check(TerritoryRules.CreateFief(boundary, owner, X(0), Y(0)).Code == GameCodes.Ok && bag.Count == 11, "original Count<=10 permits eleventh fief");
string limitBefore = Json(boundary.Data);
Check(TerritoryRules.CreateFief(boundary, owner, X(1), Y(1)).Code == GameCodes.Conflict && limitBefore == Json(boundary.Data), "original eleventh fief blocks next construction");
var full = world.Clone(); var fullTown = TerritoryRules.City(full, X(0), Y(0));
int capacity = full.Data["城池容量配置"].OfType<JObject>().First(row => row.Value<int>("规模") == fullTown.Value<int>("规模")).Value<int>("容量");
fullTown["城池封地列表"] = new JArray(Enumerable.Range(0, capacity).Select(_ => new JObject { ["第几个玩家"] = bindings[1].LegacyPlayerIndex, ["封地ID标识"] = 1 }));
string fullBefore = Json(full.Data);
Check(TerritoryRules.CreateFief(full, owner, X(0), Y(0)).Code == GameCodes.WorldFull && fullBefore == Json(full.Data), "actual original city capacity rejects unchanged");
var enemy = world.Data["城池列表"].OfType<JObject>().First(city => city.Value<string>("国家") != "汉");
Check(TerritoryRules.CreateFief(world, owner, enemy.Value<int>("坐标x"), enemy.Value<int>("坐标y")).Code == GameCodes.Forbidden, "enemy city is outside original allied construction flow");

GameCommand Command(int town = 0) => new GameCommand { WorldId = world.WorldId, RequestId = Guid.NewGuid().ToString("N"), Type = "fief.create",
    Payload = new JObject { ["cityX"] = X(town), ["cityY"] = Y(town) } };
var actor = new AuthenticatedActor(bindings[0].AccountId, owner, world.WorldId, "actual-world-check");
string receipt; GameCommand durable;
using (var store = new SqliteWorldStore(db))
{
    store.ImportWorld(world, bindings);
    var runtime = new WorldRuntime(store, a => a.IsSystem || store.ResolveRole(a.WorldId, a.AccountId)?.PlayerId == a.PlayerId);
    runtime.Register(new TerritoryModule(GeneralsModule.EnsureMappings));
    durable = Command(); var result = runtime.Execute(actor, durable); receipt = JsonConvert.SerializeObject(result);
    Check(result.Code == GameCodes.Ok && result.Data.Value<string>("fiefId") != null && result.Data["legacyFiefId"] == null,
        "actual Runtime SQL and frozen Generals create stable fief together");
    var saved = store.Load(world.WorldId); string newStableId = result.Data.Value<string>("fiefId");
    Check(saved.EntityMappings["fiefs"][newStableId].Value<string>("playerId") == owner &&
        saved.EntityMappings["fiefs"][newStableId].Value<int>("legacyId") == 2 && result.Events.Single().AudiencePlayerIds.SequenceEqual(new[] { owner }),
        "actual stable mapping and private event committed atomically");
    Check(receipt == JsonConvert.SerializeObject(runtime.Execute(actor, durable)) && Player(store.Load(world.WorldId))["封地信息表"].Count() == 2, "actual construction receipt replay is once");
    var forged = Command(1); forged.Payload["playerId"] = bindings[1].PlayerId;
    string before = Json(store.Load(world.WorldId).Data);
    Check(runtime.Execute(actor, forged).Code == GameCodes.InvalidArgument && before == Json(store.Load(world.WorldId).Data), "actor injection cannot create another role's fief");
    var large = Command(1); large.Payload["cityX"] = JToken.Parse("9223372036854775808");
    Check(runtime.Execute(actor, large).Code == GameCodes.InvalidArgument, "oversized integer coordinate rejected");
    var tasks = Enumerable.Range(0, 2).Select(_ => Task.Run(() => runtime.Execute(actor, Command(1)))).ToArray(); Task.WaitAll(tasks);
    Check(tasks.Count(t => t.Result.Code == GameCodes.Ok) == 1 && tasks.Count(t => t.Result.Code == GameCodes.Conflict) == 1 &&
        Player(store.Load(world.WorldId))["封地信息表"].Count() == 3, "actual concurrent requests build city fief once");
}
using (var store = new SqliteWorldStore(db))
{
    var runtime = new WorldRuntime(store, a => a.IsSystem || store.ResolveRole(a.WorldId, a.AccountId)?.PlayerId == a.PlayerId);
    runtime.Register(new TerritoryModule(GeneralsModule.EnsureMappings));
    Check(receipt == JsonConvert.SerializeObject(runtime.Execute(actor, durable)) && Player(store.Load(world.WorldId))["封地信息表"].Count() == 3,
        "actual reopened database preserves fief receipt and maps");
}
checks += CityVictoryChecks.Run(world, bindings, db + ".victory");
Console.WriteLine("TERRITORY_CHECKS_PASS " + checks);
