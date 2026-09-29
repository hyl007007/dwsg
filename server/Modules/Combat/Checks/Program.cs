using Dwsg.Persistence;
using Dwsg.Runtime;
using Dwsg.Server.Modules.Combat;
using Dwsg.Server.Modules.Generals;
using Dwsg.Server.World;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Dwsg.Shared.Generals;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

if (args.Length != 2) throw new ArgumentException("Pass the actual Unity export and an ignored audit output directory.");
string directory = Path.GetFullPath(args[1]);
if (!directory.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Contains("audit", StringComparer.OrdinalIgnoreCase)) throw new ArgumentException("Check output must stay in audit.");
directory = Path.Combine(directory, Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
JObject seed = JObject.Parse(File.ReadAllText(args[0]));
long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
int checks = 0;
void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); checks++; Console.WriteLine("PASS " + name); }
var world = new WorldState { WorldId = "combat-check-" + Guid.NewGuid().ToString("N"), Data = (JObject)seed.DeepClone(), EntityMappings = new JObject { ["players"] = new JObject() } };
JObject players = (JObject)world.EntityMappings["players"];
for (int index = 0; index < ((JArray)world.Data["玩家列表"]).Count; index++) players[Guid.NewGuid().ToString("N")] = index;
var actors = new List<AuthenticatedActor>();
var bindings = new List<RoleBinding>();
var originalRandom = new CombatRandom(123456789);
for (int account = 0; account < 2; account++)
{
    Check(LegacyWorldModule.CreatePlayer(world, "战斗君主" + account, "汉", now, out int index).Code == GameCodes.Ok, "actual original player creation " + account);
    string id = Guid.NewGuid().ToString("N"); players[id] = index;
    var player = world.RequirePlayer(id);
    var fief = (JObject)player["封地信息表"][0];
    int generalCount = account == 0 ? 25 : 5;
    for (int generalIndex = 0; generalIndex < generalCount; generalIndex++)
    {
        JObject configuration = ((JArray)seed["将领配置"]).OfType<JObject>().Single(item => item.Value<int>("ID") == generalIndex % 4 + 1);
        JObject general = GeneralCreationRules.CreateBanditGeneral(configuration, originalRandom.Next);
        general["ID"] = generalIndex + 1;
        GeneralExperienceRules.Add(general, GeneralExperienceRules.TotalForLevel(20));
        ((JArray)fief["将领信息表"]).Add(general);
    }
    player["将领ID标识"] = generalCount + 1;
    GeneralAttributeRules.Recalculate(player, now / 1000);
    ((JArray)fief["闲兵信息表"]).Add(new JObject { ["ID"] = 104, ["数量"] = 5000.0 });
    ((JArray)fief["闲兵信息表"]).Add(new JObject { ["ID"] = 403, ["数量"] = 50000.0 });
    actors.Add(new AuthenticatedActor("combat-account-" + account, id, world.WorldId, "combat-connection-" + account));
    bindings.Add(new RoleBinding { WorldId = world.WorldId, AccountId = actors.Last().AccountId, PlayerId = id, LegacyPlayerIndex = index });
}
GeneralsModule.EnsureMappings(world);
string GeneralId(AuthenticatedActor actor, int legacyId) => ((JObject)world.EntityMappings["generals"]).Properties().Single(entry => entry.Value.Value<string>("playerId") == actor.PlayerId && entry.Value.Value<int>("legacyId") == legacyId).Name;
JObject lowCamp = ((JArray)world.Data["山贼列表"]).OfType<JObject>().First(camp => camp.Value<int>("等级") <= 2);
JObject highCamp = ((JArray)world.Data["山贼列表"]).OfType<JObject>().First(camp => camp.Value<int>("等级") == 10);
string db = Path.Combine(directory, "world.sqlite3");
SqliteWorldStore store = new SqliteWorldStore(db); store.ImportWorld(world, bindings);
WorldRuntime Runtime()
{
    var runtime = new WorldRuntime(store, actor => actor.IsSystem || actors.Any(real => real.AccountId == actor.AccountId && real.PlayerId == actor.PlayerId && real.ConnectionId == actor.ConnectionId), () => now);
    runtime.Register(new GeneralsModule()); runtime.Register(new CombatModule()); return runtime;
}
WorldRuntime runtime = Runtime();
GameCommand Command(string type, JObject payload, string requestId = null) => new GameCommand { WorldId = world.WorldId, RequestId = requestId ?? Guid.NewGuid().ToString("N"), Type = type, Payload = payload };
GameCommand Dispatch(AuthenticatedActor actor, JObject camp, params int[] generals) => Command("combat.bandit.dispatch", new JObject { ["x"] = camp["坐标x"], ["y"] = camp["坐标y"], ["generalIds"] = new JArray(generals.Select(id => GeneralId(actor, id))) });
BanditBattle Battle(string id) => store.Load(world.WorldId).Data["战斗运行"][id].ToObject<BanditBattle>();
JObject General(AuthenticatedActor actor, int id) { JObject fief; return GeneralsModule.ResolveGeneral(store.Load(world.WorldId), actor.PlayerId, GeneralId(actor, id), out fief); }
for (int account = 0; account < 2; account++)
for (int id = 1; id <= 5; id++)
{
    var allocation = runtime.Execute(actors[account], Command("generals.allocateTroops", new JObject { ["generalId"] = GeneralId(actors[account], id), ["troopTypeId"] = 104, ["count"] = 300 }));
    Check(allocation.Code == GameCodes.Ok, "actual authenticated allocation " + account + ":" + id);
}
void RejectedUnchanged(AuthenticatedActor actor, GameCommand command, string name)
{
    var before = store.Load(world.WorldId); string value = before.Data.ToString(Formatting.None);
    Check(runtime.Execute(actor, command).Code != GameCodes.Ok, name + " rejected");
    var after = store.Load(world.WorldId);
    Check(before.Revision == after.Revision && value == after.Data.ToString(Formatting.None), name + " has no domain writes");
}
RejectedUnchanged(actors[1], Dispatch(actors[0], lowCamp, 1), "foreign general");
RejectedUnchanged(actors[0], Command("combat.bandit.dispatch", new JObject { ["x"] = lowCamp["坐标x"], ["y"] = lowCamp["坐标y"], ["generalIds"] = new JArray(GeneralId(actors[0], 1), GeneralId(actors[0], 1)) }), "duplicate selection");
RejectedUnchanged(actors[0], Command("combat.bandit.advance", new JObject { ["battleId"] = "forged", ["tickUtcMs"] = now }), "client simulation");
var dispatch = Dispatch(actors[0], lowCamp, 1, 2, 3, 4, 5);
int published = 0;
runtime.Committed += result => published += result.Events.Count;
var started = runtime.Execute(actors[0], dispatch);
Check(started.Code == GameCodes.Ok, "real Runtime dispatch commits");
string battleId = started.Data.Value<string>("battleId");
BanditBattle march = Battle(battleId);
Check(march.Phase == "marching" && march.ArrivalUtcMs == now + 10000 && march.Frame == 0, "original ten second march");
Check(march.Attackers.Count == 5 && store.Load(world.WorldId).EntityMappings["generalOccupancy"].Count() == 5, "all five generals persist as occupied");
long revision = store.Load(world.WorldId).Revision;
Check(JsonConvert.SerializeObject(runtime.Execute(actors[0], dispatch)) == JsonConvert.SerializeObject(started) && store.Load(world.WorldId).Revision == revision && published == 1, "same request replays receipt without events or second army");
var changed = Command(dispatch.Type, (JObject)dispatch.Payload.DeepClone(), dispatch.RequestId); changed.Payload["x"] = 999;
Check(runtime.Execute(actors[0], changed).Code == GameCodes.RequestConflict, "changed retry conflicts");
RejectedUnchanged(actors[1], Command("combat.bandit.withdraw", new JObject { ["battleId"] = battleId }), "foreign retreat");
now += 9999; runtime.Tick(world.WorldId); Check(Battle(battleId).Phase == "marching", "early Tick cannot arrive");
now++; runtime.Tick(world.WorldId); Check(Battle(battleId).Phase == "fighting" && Battle(battleId).Frame == 0, "server Tick arrives exactly once");
store.Dispose(); store = new SqliteWorldStore(db); runtime = Runtime();
Check(Battle(battleId).RandomState == march.RandomState && Battle(battleId).Phase == "fighting", "arrival and random state survive process store restart");
now += 100; runtime.Tick(world.WorldId); Check(Battle(battleId).Frame == 6, "same original progress rule advances six real simulation frames");
File.WriteAllText(Path.Combine(directory, "fighting-battle.json"), JObject.FromObject(Battle(battleId)).ToString());
var single = Command("combat.bandit.withdraw", new JObject { ["battleId"] = battleId, ["generalId"] = GeneralId(actors[0], 1) });
Check(runtime.Execute(actors[0], single).Code == GameCodes.Ok, "one general retreats through candidate transaction");
Check(Battle(battleId).Attackers.Count(unit => unit.Retired) == 1 && store.Load(world.WorldId).EntityMappings["generalOccupancy"].Count() == 4 && General(actors[0], 1)["详细信息"].Value<int>("状态") == 0, "single retreat releases only selected general");
JObject otherCamp = ((JArray)seed["山贼列表"]).OfType<JObject>().First(camp => camp.Value<int>("坐标x") != lowCamp.Value<int>("坐标x"));
var newArmy = runtime.Execute(actors[0], Dispatch(actors[0], otherCamp, 1));
Check(newArmy.Code == GameCodes.Ok, "retreated general can march again");
string newBattleId = newArmy.Data.Value<string>("battleId");
string newArmyId = newArmy.Data.Value<string>("armyId");
double copper = ((JArray)store.Load(world.WorldId).Data["国家列表"]).OfType<JObject>().Single(item => item.Value<string>("国号") == "汉").Value<double>("铜钱");
double grain = ((JArray)store.Load(world.WorldId).Data["国家列表"]).OfType<JObject>().Single(item => item.Value<string>("国号") == "汉").Value<double>("粮食");
for (int advance = 0; advance < 50 && !Battle(battleId).SettlementApplied; advance++) { now += 10000; runtime.Tick(world.WorldId); }
BanditBattle won = Battle(battleId);
Check(won.SettlementApplied && won.Phase == "won", "actual original battle reaches victory");
Check(won.Defenders.Sum(unit => unit.Remaining) == 0 && won.Reward.声望 > 0, "real enemy casualties produce original rewards");
var current = store.Load(world.WorldId);
var nation = ((JArray)current.Data["国家列表"]).OfType<JObject>().Single(item => item.Value<string>("国号") == "汉");
// 新军队可能同时战斗；战斗运行中每个实际已结算的奖励总和与国库入账一致。
var settlements = ((JObject)current.Data["战斗运行"]).Properties().Select(entry => entry.Value.ToObject<BanditBattle>()).Where(battle => battle.SettlementApplied).ToArray();
Check(nation.Value<double>("铜钱") == copper + settlements.Sum(battle => battle.Reward.国库铜钱) && nation.Value<double>("粮食") == grain + settlements.Sum(battle => battle.Reward.国库粮食), "actual treasury gains match each single settlement");
if (!Battle(newBattleId).SettlementApplied)
    Check(current.EntityMappings["generalOccupancy"][GeneralId(actors[0], 1)].Value<string>("armyId") == newArmyId && General(actors[0], 1)["详细信息"].Value<int>("状态") == 1, "old battle cannot release new army occupation");
else Check(Battle(newBattleId).SettlementApplied, "new army has its own settlement");
foreach (CombatUnit unit in won.Attackers.Where(unit => !unit.Retired))
{
    JObject result = General(actors[0], unit.General.Value<int>("ID"));
    Check(result["将领配兵"].Value<double>("数量") == unit.Remaining && result["详细信息"].Value<int>("状态") == 0, "original army quantity and idle state written back " + unit.GeneralId);
}
Check(Battle(battleId).RandomState != march.RandomState, "combat and original bandit regeneration consume persisted random stream");
string settledState = current.Data.ToString(Formatting.None);
var duplicateSettle = Command("combat.bandit.advance", new JObject { ["battleId"] = battleId, ["tickUtcMs"] = won.NextTickUtcMs });
Check(runtime.Execute(AuthenticatedActor.System(world.WorldId), duplicateSettle).Code == GameCodes.Ok && store.Load(world.WorldId).Data.ToString(Formatting.None) == settledState, "new request cannot pay settled battle twice");
store.Dispose(); store = new SqliteWorldStore(db); runtime = Runtime();
Check(store.Load(world.WorldId).Data.ToString(Formatting.None) == settledState && runtime.Execute(actors[0], dispatch).Data.Value<string>("battleId") == battleId, "settlement and command dedup survive restart");
// 独立真实失败战：少量兵力对原十级山贼，伤兵仍按原山贼100%规则回到同一封地。
if (!Battle(newBattleId).SettlementApplied) runtime.Execute(actors[0], Command("combat.bandit.withdraw", new JObject { ["battleId"] = newBattleId }));
Check(runtime.Execute(actors[0], Command("generals.allocateTroops", new JObject { ["generalId"] = GeneralId(actors[0], 1), ["troopTypeId"] = 104, ["count"] = 1 })).Code == GameCodes.Ok, "allocate original one soldier for defeat check");
var losing = runtime.Execute(actors[0], Dispatch(actors[0], highCamp, 1)); Check(losing.Code == GameCodes.Ok, "defeat march starts");
string losingId = losing.Data.Value<string>("battleId");
for (int advance = 0; advance < 100 && !Battle(losingId).SettlementApplied; advance++) { now += 10000; runtime.Tick(world.WorldId); }
var lost = Battle(losingId);
Check(lost.Phase == "lost" && lost.SettlementApplied && lost.Attackers[0].Remaining == 0 && lost.Attackers[0].Wounded == 1, "original defeat and 100 percent bandit wounded");
Check(General(actors[0], 1)["将领配兵"].Value<double>("数量") == 0 && General(actors[0], 1)["将领配兵"].Value<int>("ID") == 104, "defeat preserves original zero quantity troop identifier");
// 原冲城车在山贼战不攻击，保留真实弱山贼和原兵种，让十五坑排队持续可检验。
for (int id = 1; id <= 25; id++)
    Check(runtime.Execute(actors[0], Command("generals.allocateTroops", new JObject { ["generalId"] = GeneralId(actors[0], id), ["troopTypeId"] = 403, ["count"] = 300 })).Code == GameCodes.Ok, "actual queue fixture allocation " + id);
JObject queueCamp = ((JArray)seed["山贼列表"]).OfType<JObject>().First(camp => camp.Value<int>("等级") == 1
    && camp.Value<int>("坐标x") != lowCamp.Value<int>("坐标x") && camp.Value<int>("坐标x") != otherCamp.Value<int>("坐标x"));
var queueStart = runtime.Execute(actors[0], Dispatch(actors[0], queueCamp, 1, 2, 3, 4, 5));
Check(queueStart.Code == GameCodes.Ok, "real multi-army march starts");
string queueBattleId = queueStart.Data.Value<string>("battleId");
now += 10000; runtime.Tick(world.WorldId);
GameCommand Reinforce(params int[] ids) => Command("combat.bandit.reinforce", new JObject { ["battleId"] = queueBattleId, ["generalIds"] = new JArray(ids.Select(id => GeneralId(actors[0], id))) });
var reinforcement = Reinforce(11, 12, 13, 14, 15);
var reinforced = runtime.Execute(actors[0], reinforcement);
Check(reinforced.Code == GameCodes.Ok && Battle(queueBattleId).Attackers.Count == 10 && Battle(queueBattleId).Frame == 0, "original immediate reinforcement starts independent formation");
string reinforcementArmyId = reinforced.Data.Value<string>("armyId");
Check(runtime.Execute(actors[0], reinforcement).Data.Value<string>("armyId") == reinforcementArmyId && Battle(queueBattleId).Attackers.Count == 10, "reinforcement request retry does not add units twice");
var followup = Dispatch(actors[0], queueCamp, 6, 7, 8, 9, 10);
now += 37; // 行军截止故意不落在100ms战斗批次边界。
var followupResult = runtime.Execute(actors[0], followup);
Check(followupResult.Code == GameCodes.Ok, "same owner can dispatch again to same original camp");
string followupId = followupResult.Data.Value<string>("battleId");
Check(Battle(followupId).ArrivalUtcMs == now + 10000 && Battle(queueBattleId).Attackers.Count == 10, "followup keeps ten second march before joining");
Check(runtime.Execute(actors[0], Reinforce(16, 17, 18, 19, 20)).Code == GameCodes.Ok, "second independent reinforcement accepted");
RejectedUnchanged(actors[1], Reinforce(21), "foreign reinforcement");
RejectedUnchanged(actors[0], Reinforce(11, 21), "partially occupied reinforcement");
RejectedUnchanged(actors[1], Dispatch(actors[1], queueCamp, 1), "cross owner busy camp");
now += 9999; runtime.Tick(world.WorldId);
Check(Battle(followupId).Phase == "marching" && Battle(queueBattleId).Attackers.Count == 15, "followup cannot join at 9999 milliseconds");
now++; runtime.Tick(world.WorldId);
Check(Battle(followupId).Phase == "joined" && Battle(followupId).SettlementApplied && Battle(queueBattleId).Attackers.Count == 20, "followup hands off existing occupied army at deadline");
Check(Battle(followupId).Reward.声望 == 0 && Battle(followupId).Reward.国库铜钱 == 0, "march handoff cannot become another rewarded settlement");
Check(Battle(queueBattleId).AttackFormations.Count == 4 && store.Load(world.WorldId).EntityMappings["generalOccupancy"].Count() == 20, "four distinct army reservations survive handoff");
for (int advance = 0; advance < 6; advance++) { now += 10000; runtime.Tick(world.WorldId); }
BanditBattle queued = Battle(queueBattleId);
File.WriteAllText(Path.Combine(directory, "queued-world.json"), store.Load(world.WorldId).Data.ToString());
Check(!queued.SettlementApplied && queued.Attackers.Count(unit => unit.Slot >= 0) == 15 && queued.Attackers.Count(unit => unit.Slot < 0) == 5, "original fifteen pits stay full with five generals queued");
Check(queued.Attackers.Where(unit => unit.Slot >= 0).Select(unit => unit.Slot).Distinct().Count() == 15 && queued.Attackers.All(unit => unit.Progress >= 0 && unit.Progress <= 3), "queue never duplicates slots or local attack progress");
var retireAgain = Command("combat.bandit.withdraw", new JObject { ["battleId"] = queueBattleId, ["generalId"] = GeneralId(actors[0], 1) });
Check(runtime.Execute(actors[0], retireAgain).Code == GameCodes.Ok, "retire primary general in shared battlefield");
Check(runtime.Execute(actors[0], Command("generals.refillTroops", new JObject { ["generalId"] = GeneralId(actors[0], 1) })).Code == GameCodes.Ok, "retired general refills from real pool");
var rejoin = runtime.Execute(actors[0], Reinforce(1));
Check(rejoin.Code == GameCodes.Ok, "retired general may reinforce same battle with a new army");
string rejoinArmyId = rejoin.Data.Value<string>("armyId");
Check(Battle(queueBattleId).Attackers.Count(unit => unit.GeneralId == GeneralId(actors[0], 1)) == 2 && Battle(queueBattleId).Attackers.Count(unit => !unit.Retired && unit.GeneralId == GeneralId(actors[0], 1)) == 1, "old retired unit and new live unit have separate instances");
runtime.Execute(actors[0], retireAgain);
Check(store.Load(world.WorldId).EntityMappings["generalOccupancy"][GeneralId(actors[0], 1)].Value<string>("armyId") == rejoinArmyId, "old individual retreat retry cannot release rejoined army");
string queueState = store.Load(world.WorldId).Data.ToString(Formatting.None);
store.Dispose(); store = new SqliteWorldStore(db); runtime = Runtime();
Check(store.Load(world.WorldId).Data.ToString(Formatting.None) == queueState && Battle(queueBattleId).AttackFormations.Count == 5, "all formation queues and rejoined reservation survive restart");
var queueRetreat = Command("combat.bandit.withdraw", new JObject { ["battleId"] = queueBattleId });
Check(runtime.Execute(actors[0], queueRetreat).Code == GameCodes.Ok && Battle(queueBattleId).Phase == "withdrawn", "full retreat commits each independent army outcome");
Check(store.Load(world.WorldId).EntityMappings["generalOccupancy"].Count() == 0 && Enumerable.Range(1, 20).All(id => General(actors[0], id)["详细信息"].Value<int>("状态") == 0), "all active armies release once without touching old retired unit");
string retreatState = store.Load(world.WorldId).Data.ToString(Formatting.None);
runtime.Execute(actors[0], queueRetreat);
Check(store.Load(world.WorldId).Data.ToString(Formatting.None) == retreatState, "multiple army withdrawal retries are idempotent");
// 前战场先结束时，后续军情到达按当时原山贼重建战场，不能丢掉后军或释放它。
var previous = runtime.Execute(actors[0], Dispatch(actors[0], queueCamp, 1));
Check(previous.Code == GameCodes.Ok, "next original battle starts");
string previousId = previous.Data.Value<string>("battleId");
now += 10000; runtime.Tick(world.WorldId);
var delayed = runtime.Execute(actors[0], Dispatch(actors[0], queueCamp, 2));
Check(delayed.Code == GameCodes.Ok, "delayed followup starts");
string delayedId = delayed.Data.Value<string>("battleId");
Check(runtime.Execute(actors[0], Command("combat.bandit.withdraw", new JObject { ["battleId"] = previousId })).Code == GameCodes.Ok, "previous battlefield ends before followup arrives");
now += 10000; runtime.Tick(world.WorldId);
Check(Battle(delayedId).Phase == "fighting" && !Battle(delayedId).SettlementApplied && Battle(delayedId).Attackers.Count == 1, "followup creates its own battlefield after prior one ended");
Check(runtime.Execute(actors[0], Command("combat.bandit.withdraw", new JObject { ["battleId"] = delayedId })).Code == GameCodes.Ok, "remaining followup army can retreat normally");
// 真实骑兵两支army共同击败原山贼，验证胜利结算也按各自占用释放。
var lastVictory = runtime.Execute(actors[1], Dispatch(actors[1], queueCamp, 1, 2));
Check(lastVictory.Code == GameCodes.Ok, "another owner can attack after prior armies end");
string lastVictoryId = lastVictory.Data.Value<string>("battleId");
now += 10000; runtime.Tick(world.WorldId);
Check(runtime.Execute(actors[1], Command("combat.bandit.reinforce", new JObject { ["battleId"] = lastVictoryId,
    ["generalIds"] = new JArray(new[] { 3, 4, 5 }.Select(id => GeneralId(actors[1], id))) })).Code == GameCodes.Ok, "real cavalry victory accepts second army");
double beforeVictoryCopper = ((JArray)store.Load(world.WorldId).Data["国家列表"]).OfType<JObject>().Single(item => item.Value<string>("国号") == "汉").Value<double>("铜钱");
for (int advance = 0; advance < 50 && !Battle(lastVictoryId).SettlementApplied; advance++) { now += 10000; runtime.Tick(world.WorldId); }
BanditBattle finalVictory = Battle(lastVictoryId);
Check(finalVictory.Phase == "won" && finalVictory.SettlementApplied && finalVictory.AttackFormations.Count == 2, "two independent real armies settle victory once");
Check(store.Load(world.WorldId).EntityMappings["generalOccupancy"].Count() == 0 && Enumerable.Range(1, 5).All(id => General(actors[1], id)["详细信息"].Value<int>("状态") == 0), "victory releases both matched armies");
Check(((JArray)store.Load(world.WorldId).Data["国家列表"]).OfType<JObject>().Single(item => item.Value<string>("国号") == "汉").Value<double>("铜钱") == beforeVictoryCopper + finalVictory.Reward.国库铜钱, "multiple army victory pays one original treasury reward");
File.WriteAllText(Path.Combine(directory, "queued-battle.json"), JObject.FromObject(queued).ToString());
File.WriteAllText(Path.Combine(directory, "settled-battle.json"), JObject.FromObject(won).ToString());
File.WriteAllText(Path.Combine(directory, "result.json"), new JObject { ["checks"] = checks, ["worldId"] = world.WorldId, ["battleId"] = battleId, ["status"] = "passed" }.ToString());
Console.WriteLine("COMBAT_RUNTIME_PASSED checks=" + checks + " output=" + directory);
store.Dispose();
