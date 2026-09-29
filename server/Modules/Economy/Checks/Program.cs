using Dwsg.Persistence;
using Dwsg.Runtime;
using Dwsg.Server.Economy;
using Dwsg.Server.World;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

if (args.Length != 2) throw new ArgumentException("Pass the actual Unity-exported world-seed.json and an audit database path.");
var seed = JObject.Parse(File.ReadAllText(args[0]));
string db = Path.GetFullPath(args[1]);
if (!db.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains("audit", StringComparer.OrdinalIgnoreCase))
    throw new ArgumentException("Checks must write only to audit.");
if (File.Exists(db)) throw new ArgumentException("Use a new disposable check database.");
int checks = 0;
void Check(bool valid, string name)
{
    if (!valid) throw new InvalidOperationException(name);
    checks++;
    Console.WriteLine("PASS " + name);
}
Check(MonarchRules.RequiredExperience(62) == 2848782, "original Unity level 62 double-Pow float-cast threshold");
JObject Product(JObject data, string name) => data["商城商品"].OfType<JObject>().First(item => item.Value<string>("道具名") == name);
JObject Definition(string name) => seed["道具配置"].OfType<JObject>().First(item => item.Value<string>("名字") == name);
JObject Player(int slots, double capacity = 300, params double[] stacks)
{
    var player = (JObject)seed["新角色模板"].DeepClone();
    foreach (var bag in new[] { "背包道具列表", "背包装备列表" })
        foreach (var array in ((JObject)player[bag]).Properties()) ((JArray)array.Value).Clear();
    player["基础信息"]["背包容量上限"] = capacity;
    var items = (JArray)player["背包道具列表"]["宝物道具列表"];
    foreach (double quantity in stacks) items.Add(new JObject { ["名字"] = "将神魂", ["ID"] = 0, ["数量"] = quantity });
    while (items.Count < slots) items.Add(new JObject { ["名字"] = "皇榜", ["ID"] = 0, ["数量"] = 1.0 });
    return player;
}
void PurchaseCheck(string name, JObject player, string itemName, string currency, double quantity, bool success)
{
    var item = (JObject)Product(seed, itemName).DeepClone();
    string before = new JObject { ["player"] = player.DeepClone(), ["product"] = item.DeepClone() }.ToString(Formatting.None);
    var result = ShopRules.Purchase(player, item, Definition(itemName).Value<string>("分类"), currency, quantity);
    Check((result.Code == GameCodes.Ok) == success, name);
    if (!success) Check(before == new JObject { ["player"] = player, ["product"] = item }.ToString(Formatting.None), name + " unchanged");
}
PurchaseCheck("full bag rejects new stack", Player(300), "将神魂", "黄金", 1, false);
var merge = Player(300, 300, 998);
PurchaseCheck("full bag merges 998+1", merge, "将神魂", "黄金", 1, true);
Check(merge["背包道具列表"]["宝物道具列表"][0].Value<double>("数量") == 999 && ShopRules.UsedSlots(merge) == 300, "merge preserves slots");
PurchaseCheck("full bag rejects 998+2", Player(300, 300, 998), "将神魂", "黄金", 2, false);
var split = Player(299, 300, 998);
PurchaseCheck("spare slot splits 998+2", split, "将神魂", "黄金", 2, true);
Check(split["背包道具列表"]["宝物道具列表"][0].Value<double>("数量") == 999 &&
    split["背包道具列表"]["宝物道具列表"].Last.Value<double>("数量") == 1, "original overflow quantities");
var smallest = Player(300, 300, 999, 998);
PurchaseCheck("chooses minimum stack", smallest, "将神魂", "黄金", 1, true);
Check(smallest["背包道具列表"]["宝物道具列表"][1].Value<double>("数量") == 999, "minimum stack changed");
PurchaseCheck("expanded capacity", Player(300, 301), "将神魂", "黄金", 1, true);
var equipment = Player(296);
foreach (var array in ((JObject)equipment["背包装备列表"]).Properties()) ((JArray)array.Value).Add(new JObject());
PurchaseCheck("all four equipment lists count", equipment, "将神魂", "黄金", 1, false);
foreach (double quantity in new[] { 0, -1, 101, 1.5, double.NaN, double.PositiveInfinity })
    PurchaseCheck("invalid quantity " + quantity, Player(0), "将神魂", "黄金", quantity, false);
PurchaseCheck("unsupported silver", Player(0), "将神魂", "白银", 1, false);
var poor = Player(0); poor["财产信息"]["黄金"] = 0;
PurchaseCheck("insufficient funds", poor, "将神魂", "黄金", 1, false);
var silver = Player(0); silver["财产信息"]["白银"] = Product(seed, "疾风符").Value<double>("白银售价");
PurchaseCheck("exact silver balance", silver, "疾风符", "白银", 1, true);
Check(silver["财产信息"].Value<double>("白银") == 0, "silver debited exactly");
var sellStacks = Player(2, 300, 2, 3);
double oldGold = sellStacks["财产信息"].Value<double>("黄金");
var sellListing = (JObject)Product(seed, "将神魂").DeepClone();
var sellResult = ShopRules.Sell(sellStacks, sellListing, "宝物", "黄金", 4);
Check(sellResult.Code == GameCodes.Ok && sellStacks["背包道具列表"]["宝物道具列表"].Count() == 1 &&
    sellStacks["背包道具列表"]["宝物道具列表"][0].Value<double>("数量") == 1 &&
    sellStacks["财产信息"].Value<double>("黄金") == oldGold + sellListing.Value<double>("黄金售价") * 4,
    "sale consumes original reverse stacks and credits original full price");
string beforeOversell = sellStacks.ToString(Formatting.None);
Check(ShopRules.Sell(sellStacks, sellListing, "宝物", "黄金", 2).Code == GameCodes.Conflict && beforeOversell == sellStacks.ToString(Formatting.None),
    "oversell leaves inventory and wallet unchanged");
foreach (string name in new[] { "招贤令", "招贤金榜", "皇榜" })
{
    var itemOwner = Player(0);
    var items = (JArray)itemOwner["背包道具列表"]["宝物道具列表"];
    items.Add(new JObject { ["名字"] = name, ["ID"] = 0, ["数量"] = 999.0 });
    items.Add(new JObject { ["名字"] = name, ["ID"] = 0, ["数量"] = 1.0 });
    Check(InventoryRules.ConsumeOne(itemOwner, (JArray)seed["道具配置"], name).Code == GameCodes.Ok &&
        itemOwner["背包道具列表"]["宝物道具列表"].Count() == 1 &&
        itemOwner["背包道具列表"]["宝物道具列表"][0].Value<double>("数量") == 999, "original refresh consumes minimum stack " + name);
}
var starter = (JObject)seed["新角色模板"].DeepClone();
var starterWallet = (JObject)starter["财产信息"].DeepClone();
Check(StarterPackRules.Use(starter, (JArray)seed["道具配置"]).Code == GameCodes.Ok &&
    starter["财产信息"].Value<double>("铜钱") == starterWallet.Value<double>("铜钱") + 500000 &&
    starter["财产信息"].Value<double>("粮食") == starterWallet.Value<double>("粮食") + 1000000 &&
    starter["财产信息"].Value<double>("黄金") == starterWallet.Value<double>("黄金") + 100000 &&
    !starter["背包道具列表"]["宝箱道具列表"].Any(item => item.Value<string>("名字") == "新手礼包"), "original starter rewards and single consumption");
string noStarter = starter.ToString(Formatting.None);
Check(StarterPackRules.Use(starter, (JArray)seed["道具配置"]).Code == GameCodes.Conflict && noStarter == starter.ToString(Formatting.None),
    "missing starter gives no partial rewards");

string worldId = Guid.NewGuid().ToString("N");
var world = new WorldState { WorldId = worldId, Data = (JObject)seed.DeepClone(), EntityMappings = new JObject { ["players"] = new JObject() } };
var mappings = (JObject)world.EntityMappings["players"];
for (int i = 0; i < ((JArray)world.Data["玩家列表"]).Count; i++) mappings[Guid.NewGuid().ToString("N")] = i;
var failedWorld = world.Clone();
string oldWorld = failedWorld.Data.ToString(Formatting.None);
var badRole = LegacyWorldModule.CreatePlayer(failedWorld, "<bad>", "汉", 0, out int badIndex);
Check(badRole.Code == GameCodes.InvalidArgument && badIndex == -1 && oldWorld == failedWorld.Data.ToString(Formatting.None), "invalid role leaves original world unchanged");
Check(LegacyWorldModule.CreatePlayer(failedWorld, "合法君主", "不存在的国号", 0, out _).Code == GameCodes.NotFound &&
    oldWorld == failedWorld.Data.ToString(Formatting.None), "missing original nation rejects without mutation");
var fullWorld = world.Clone();
var han = fullWorld.Data["国家列表"].OfType<JObject>().First(item => item.Value<string>("国号") == "汉");
var capital = fullWorld.Data["城池列表"].OfType<JObject>().First(item => item.Value<int>("坐标x") == han.Value<int>("国都x") && item.Value<int>("坐标y") == han.Value<int>("国都y"));
int capitalCapacity = fullWorld.Data["城池容量配置"].OfType<JObject>().First(item => item.Value<int>("规模") == capital.Value<int>("规模")).Value<int>("容量");
var originalFiefIndex = capital["城池封地列表"].First;
capital["城池封地列表"] = new JArray(Enumerable.Range(0, capitalCapacity).Select(_ => originalFiefIndex.DeepClone()));
string fullBefore = fullWorld.Data.ToString(Formatting.None);
Check(LegacyWorldModule.CreatePlayer(fullWorld, "满城君主", "汉", 0, out _).Code == GameCodes.WorldFull && fullBefore == fullWorld.Data.ToString(Formatting.None),
    "original capital capacity rejects without player or membership mutation");
var bindings = new List<RoleBinding>();
for (int i = 0; i < 2; i++)
{
    var role = LegacyWorldModule.CreatePlayer(world, "联网君主" + i, "汉", 0, out int index);
    Check(role.Code == GameCodes.Ok && index == ((JArray)seed["玩家列表"]).Count + i, "role appends behind original NPCs " + i);
    string playerId = Guid.NewGuid().ToString("N");
    mappings[playerId] = index;
    var player = world.RequirePlayer(playerId);
    player["财产信息"]["黄金"] = 1000000.0;
    Check(player["封地信息表"][0]["建筑信息表"].Count() == 13 &&
        player["封地信息表"][0]["伤兵信息表"][0].Value<int>("ID") == 104 &&
        player["背包道具列表"]["宝箱道具列表"].Any(item => item.Value<string>("名字") == "新手礼包"), "original initial fief and starter item " + i);
    bindings.Add(new RoleBinding { WorldId = worldId, AccountId = Guid.NewGuid().ToString("N"), PlayerId = playerId, LegacyPlayerIndex = index });
}
GameCommand Command(string item, string currency = "黄金", int quantity = 1, string id = null) => new GameCommand
{
    WorldId = worldId, RequestId = id ?? Guid.NewGuid().ToString("N"), Type = "shop.purchase",
    Payload = new JObject { ["itemName"] = item, ["currency"] = currency, ["quantity"] = quantity, ["catalogVersion"] = 1 }
};
var actors = bindings.Select(binding => new AuthenticatedActor(binding.AccountId, binding.PlayerId, worldId, Guid.NewGuid().ToString("N"))).ToArray();
string savedResult;
GameCommand durable;
string savedSaleResult;
GameCommand durableSale;
string savedStarterResult;
GameCommand durableStarter;
using (var store = new SqliteWorldStore(db))
{
    store.ImportWorld(world, bindings);
    var runtime = new WorldRuntime(store, actor => actor.IsSystem || store.ResolveRole(actor.WorldId, actor.AccountId)?.PlayerId == actor.PlayerId);
    runtime.Register(new EconomyModule());
    int publications = 0;
    runtime.Committed += _ => publications++;
    durable = Command("将神魂", quantity: 2);
    var bought = runtime.Execute(actors[0], durable);
    Check(bought.Code == GameCodes.Ok && publications == 1, "actual Runtime and SQLite purchase committed before publication");
    savedResult = JsonConvert.SerializeObject(bought);
    var state = store.Load(worldId);
    double expectedGold = 1000000 - Product(seed, "将神魂").Value<double>("黄金售价") * 2;
    Check(state.RequirePlayer(actors[0].PlayerId)["财产信息"].Value<double>("黄金") == expectedGold, "actual store balance");
    Check(savedResult == JsonConvert.SerializeObject(runtime.Execute(actors[0], durable)) && publications == 1, "same request returns identical receipt once");
    var changed = Command("将神魂", quantity: 3, id: durable.RequestId);
    Check(runtime.Execute(actors[0], changed).Code == GameCodes.RequestConflict, "changed payload cannot reuse request ID");
    string before = store.Load(worldId).Data.ToString(Formatting.None);
    var forged = Command("将神魂"); forged.Payload["playerId"] = actors[1].PlayerId;
    Check(runtime.Execute(actors[0], forged).Code == GameCodes.InvalidArgument && before == store.Load(worldId).Data.ToString(Formatting.None), "forged payload cannot change another player");
    var wrong = new AuthenticatedActor(actors[0].AccountId, actors[1].PlayerId, worldId, "wrong");
    Check(runtime.Execute(wrong, Command("将神魂")).Code == GameCodes.Unauthenticated, "account and player ownership checked");
    Check(runtime.Execute(actors[0], Command("疾风符", "白银")).Code == GameCodes.Ok, "actual Runtime silver purchase");
    durableSale = Command("将神魂"); durableSale.Type = "shop.sell";
    var sold = runtime.Execute(actors[0], durableSale);
    savedSaleResult = JsonConvert.SerializeObject(sold);
    Check(sold.Code == GameCodes.Ok && savedSaleResult == JsonConvert.SerializeObject(runtime.Execute(actors[0], durableSale)), "actual Runtime gold sale receipt replays without second payment");
    var silverSale = Command("疾风符", "白银"); silverSale.Type = "shop.sell";
    Check(runtime.Execute(actors[0], silverSale).Code == GameCodes.Ok &&
        store.Load(worldId).RequirePlayer(actors[0].PlayerId)["财产信息"].Value<double>("白银") ==
            seed["新角色模板"]["财产信息"].Value<double>("白银"), "actual silver sale restores original full price");
    var oversell = Command("将神魂", quantity: 2); oversell.Type = "shop.sell";
    string beforeSaleFailure = store.Load(worldId).Data.ToString(Formatting.None);
    Check(runtime.Execute(actors[0], oversell).Code == GameCodes.Conflict && beforeSaleFailure == store.Load(worldId).Data.ToString(Formatting.None), "actual oversell rejects without persistent mutation");
    var tasks = Enumerable.Range(0, 6).Select(i => Task.Run(() => runtime.Execute(actors[i % 2], Command("交易品1")))).ToArray();
    Task.WaitAll(tasks);
    Check(tasks.Count(task => task.Result.Code == GameCodes.Ok) == 5 && tasks.Count(task => task.Result.Code == GameCodes.Conflict) == 1, "six concurrent buyers consume five actual stock units");
    Check(Product(store.Load(worldId).Data, "交易品1").Value<int>("限购数量") == 0, "concurrent stock persists zero");
    var refresh = new GameCommand { WorldId = worldId, RequestId = Guid.NewGuid().ToString("N"), Type = "shop.refresh" };
    Check(runtime.Execute(actors[0], refresh).Code == GameCodes.Forbidden, "client cannot refresh authoritative prices");
    runtime.Tick(worldId);
    var refreshed = store.Load(worldId);
    Check(refreshed.Data.Value<long>("商城下次刷新UTC") > DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), "server schedules original five-minute refresh durably");
    Check(refreshed.Data["商城轮换商品"].All(name =>
    {
        var product = Product(refreshed.Data, name.Value<string>());
        return product.Value<double>("黄金售价") >= 1011 && product.Value<double>("黄金售价") < 1326 &&
            product.Value<int>("限购数量") >= 1 && product.Value<int>("限购数量") < 10;
    }), "server uses original random price and stock ranges");
    durableStarter = new GameCommand { WorldId = worldId, RequestId = Guid.NewGuid().ToString("N"), Type = "item.use", Payload = new JObject { ["itemName"] = "新手礼包" } };
    var oldWallet = (JObject)store.Load(worldId).RequirePlayer(actors[0].PlayerId)["财产信息"].DeepClone();
    var opened = runtime.Execute(actors[0], durableStarter);
    savedStarterResult = JsonConvert.SerializeObject(opened);
    var afterOpen = store.Load(worldId).RequirePlayer(actors[0].PlayerId);
    Check(opened.Code == GameCodes.Ok && afterOpen["财产信息"].Value<double>("黄金") == oldWallet.Value<double>("黄金") + 100000 &&
        afterOpen["财产信息"].Value<double>("铜钱") == oldWallet.Value<double>("铜钱") + 500000 &&
        afterOpen["财产信息"].Value<double>("粮食") == oldWallet.Value<double>("粮食") + 1000000 &&
        !afterOpen["背包道具列表"]["宝箱道具列表"].Any(item => item.Value<string>("名字") == "新手礼包"), "actual Runtime atomically persists starter consumption and exact rewards");
    Check(savedStarterResult == JsonConvert.SerializeObject(runtime.Execute(actors[0], durableStarter)), "starter retry returns original result without repeating rewards");
    var secondStarter = new GameCommand { WorldId = worldId, RequestId = Guid.NewGuid().ToString("N"), Type = "item.use", Payload = new JObject { ["itemName"] = "新手礼包" } };
    string beforeSecond = store.Load(worldId).Data.ToString(Formatting.None);
    Check(runtime.Execute(actors[0], secondStarter).Code == GameCodes.Conflict && beforeSecond == store.Load(worldId).Data.ToString(Formatting.None), "actual exhausted starter leaves durable state unchanged");
}
using (var store = new SqliteWorldStore(db))
{
    var runtime = new WorldRuntime(store, actor => actor.IsSystem || store.ResolveRole(actor.WorldId, actor.AccountId)?.PlayerId == actor.PlayerId);
    runtime.Register(new EconomyModule());
    Check(savedResult == JsonConvert.SerializeObject(runtime.Execute(actors[0], durable)), "restart retains original receipt and prevents second charge");
    Check(savedSaleResult == JsonConvert.SerializeObject(runtime.Execute(actors[0], durableSale)), "restart retains sale receipt and prevents second payment");
    Check(savedStarterResult == JsonConvert.SerializeObject(runtime.Execute(actors[0], durableStarter)), "restart retains starter receipt and prevents duplicate rewards");
    var state = store.Load(worldId);
    double balance = state.RequirePlayer(actors[0].PlayerId)["财产信息"].Value<double>("黄金");
    var combat = CombatEconomy.ApplyCombatRewards(state, actors[0].PlayerId, "test-battle", 1000, 1000, 1000);
    Check(combat.Code == GameCodes.Ok && state.RequirePlayer(actors[0].PlayerId)["财产信息"].Value<double>("黄金") == balance,
        "combat changes original national treasury and experience only");
    string unchanged = state.Data.ToString(Formatting.None);
    Check(CombatEconomy.ApplyCombatRewards(state, actors[0].PlayerId, "test-battle", double.NaN, 0, 0).Code == GameCodes.InvalidArgument &&
        unchanged == state.Data.ToString(Formatting.None), "invalid combat reward leaves candidate unchanged");
}
Console.WriteLine("ECONOMY_CHECKS_PASS " + checks);
