using Dwsg.Client.Chat;
using Dwsg.Persistence;
using Dwsg.Runtime;
using Dwsg.Server.Chat;
using Dwsg.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

if (args.Length != 2) throw new ArgumentException("Expected actual Unity export and ignored audit directory.");
var data = JObject.Parse(File.ReadAllText(args[0]));
var directory = Path.Combine(Path.GetFullPath(args[1]), Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(directory);
var players = (JArray)data["玩家列表"];
var countries = ((JArray)data["国家列表"]).OfType<JObject>().Select(n => n.Value<string>("国号")).ToHashSet();
var groups = players.Select((p, i) => new { Index = i, Nation = p["基础信息"].Value<string>("国家") })
    .Where(p => countries.Contains(p.Nation)).GroupBy(p => p.Nation).ToArray();
var sameNation = groups.First(g => g.Count() >= 2).Take(2).ToArray();
int[] indexes = { sameNation[0].Index, sameNation[1].Index, groups.First(g => g.Key != sameNation[0].Nation).First().Index };
var mappings = new JObject();
for (int i = 0; i < players.Count; i++) mappings["check-player-" + i] = i;
var state = new WorldState { WorldId = "chat-check", Data = data, EntityMappings = new JObject { ["players"] = mappings } };
var bindings = indexes.Select((i, n) => new RoleBinding { WorldId = state.WorldId, AccountId = "check-account-" + n,
    PlayerId = "check-player-" + i, LegacyPlayerIndex = i }).ToArray();
var actors = bindings.Select((b, n) => new AuthenticatedActor(b.AccountId, b.PlayerId, state.WorldId, "check-connection-" + n)).ToArray();
string database = Path.Combine(directory, "chat.sqlite");
int assertions = 0;
var published = new List<GameEvent>();
GameResult worldMessage, nationMessage;
using (var store = new SqliteWorldStore(database))
{
    store.ImportWorld(state, bindings);
    var runtime = new WorldRuntime(store, actor => actors.Any(a => a.ConnectionId == actor.ConnectionId && a.AccountId == actor.AccountId && a.PlayerId == actor.PlayerId));
    runtime.Register(new ChatModule());
    runtime.Committed += result => published.AddRange(result.Events);
    worldMessage = runtime.Execute(actors[0], Command("world-message", "world", "你好，世界"));
    Check(worldMessage.Code == GameCodes.Ok && published.Count == 1, "committed world message is published");
    var first = (JObject)worldMessage.Data["chatMessage"];
    Check(first.Value<string>("senderPlayerId") == actors[0].PlayerId && first.Value<string>("senderName") ==
        state.RequirePlayer(actors[0].PlayerId)["基础信息"].Value<string>("名字"), "sender identity comes from actual original server player");
    var retry = runtime.Execute(actors[0], Command("world-message", "world", "你好，世界"));
    Check(JsonConvert.SerializeObject(retry) == JsonConvert.SerializeObject(worldMessage) && published.Count == 1,
        "same request returns exact receipt without publishing again");
    Check(runtime.Execute(actors[0], Command("world-message", "world", "改了内容")).Code == GameCodes.RequestConflict, "same id with changed chat text conflicts");
    Check(runtime.Execute(actors[0], Command("rate-limit", "world", "太快了")).Code == GameCodes.Conflict && published.Count == 1,
        "server rate limit rejects without publication");
    nationMessage = runtime.Execute(actors[1], Command("nation-message", "nation", "同国可以看见"));
    Check(nationMessage.Code == GameCodes.Ok && published.Count == 2, "another authenticated runtime actor may speak immediately");
    Check(published[1].AudiencePlayerIds.Contains(actors[0].PlayerId) && published[1].AudiencePlayerIds.Contains(actors[1].PlayerId) &&
        !published[1].AudiencePlayerIds.Contains(actors[2].PlayerId), "national event recipient whitelist excludes other country");
    Check(ChatModule.ReadHistory(store.Load(state.WorldId), actors[2]).Count == 1 && ChatModule.ReadHistory(store.Load(state.WorldId), actors[0]).Count == 2,
        "authorized history filters national messages");
    Check(ChatModule.ReadHistory(store.Load(state.WorldId), null).Count == 1, "authenticated roleless projection exposes only world chat");
    var spoof = Command("spoof", "world", "尝试冒充"); spoof.Payload["senderName"] = "管理员"; spoof.Payload["nation"] = sameNation[0].Nation;
    Check(runtime.Execute(actors[2], spoof).Code == GameCodes.InvalidArgument, "client supplied sender and nation are rejected");
    Check(runtime.Execute(actors[2], Command("long", "world", new string('字', 41))).Code == GameCodes.InvalidArgument, "server enforces original forty character limit");
    Check(runtime.Execute(actors[2], Command("empty", "world", "  \n ")).Code == GameCodes.InvalidArgument, "empty message rejected");
    Check(runtime.Execute(actors[2], Command("city", "city", "缺少当前城池")).Code == GameCodes.InvalidArgument, "city channel requires explicit valid coordinates");
    Check(runtime.Execute(actors[2], Command("private", "private", "未开放")).Code == GameCodes.InvalidArgument, "unsupported online channel rejected explicitly");
    Check(runtime.Execute(new AuthenticatedActor("unbound", actors[2].PlayerId, state.WorldId, "forged"), Command("unauthorized", "world", "越权")).Code ==
        GameCodes.Unauthenticated, "inactive or forged connection cannot send");
    Check(store.Load(state.WorldId).Revision == 2 && ((JArray)store.Load(state.WorldId).Data["聊天消息"]).Count == 2,
        "all rejected messages preserve committed chat and revision");

    var inbox = new ChatInbox();
    var snapshot = Snapshot(store.Load(state.WorldId), actors[0]);
    inbox.SetSession(snapshot);
    Check(inbox.TryReceive(state.WorldId, first, out bool self) && self, "client recognizes own committed server message");
    Check(!inbox.TryReceive(state.WorldId, first, out _) && !inbox.TryReceive("another-world", first, out _), "client de-duplicates receipt event history and rejects other world");
    inbox.SetSession(Snapshot(store.Load(state.WorldId), actors[2]));
    Check(!inbox.TryReceive(state.WorldId, (JObject)nationMessage.Data["chatMessage"], out _), "client also checks national event visibility");
    Check(inbox.TryReceive(state.WorldId, first, out bool otherSelf) && !otherSelf, "client session change resets message identity");
}
using (var restarted = new SqliteWorldStore(database))
{
    var runtime = new WorldRuntime(restarted, actor => actors.Any(a => a.ConnectionId == actor.ConnectionId));
    runtime.Register(new ChatModule()); runtime.Committed += result => published.AddRange(result.Events);
    Check(JsonConvert.SerializeObject(runtime.Execute(actors[1], Command("nation-message", "nation", "同国可以看见"))) == JsonConvert.SerializeObject(nationMessage),
        "chat receipt survives actual SQLite restart");
    Check(published.Count == 2 && ChatModule.ReadHistory(restarted.Load(state.WorldId), actors[2]).Count == 1, "restart replay does not rebroadcast or leak national history");
}
var capacity = state.Clone();
var module = new ChatModule();
for (int i = 0; i <= ChatModule.HistoryLimit; i++)
    CheckResult(module.Execute(capacity, new CommandContext(actors[0], 100000L + i * ChatModule.MinimumIntervalMs), Command("capacity-" + i, "world", "第" + i + "条")));
Check(((JArray)capacity.Data["聊天消息"]).Count == ChatModule.HistoryLimit && ((JArray)capacity.Data["聊天消息"])[0].Value<string>("content") == "第1条",
    "server history is bounded by original three hundred message limit");
CityChecks.Run(JObject.Parse(File.ReadAllText(args[0])), directory, Check);
Console.WriteLine($"PASS {assertions} chat/runtime/SQLite/client checks. Two real PHP HTTP connections remain a Host integration check. Audit: {directory}");

GameCommand Command(string id, string channel, string content) => new() { WorldId = state.WorldId, RequestId = id, Type = "chat.send",
    Payload = new JObject { ["channel"] = channel, ["content"] = content } };
static WorldSnapshot Snapshot(WorldState state, AuthenticatedActor actor) => new() { WorldId = state.WorldId, PlayerId = actor.PlayerId,
    WorldRevision = state.Revision, PrivatePlayer = (JObject)state.RequirePlayer(actor.PlayerId).DeepClone() };
void Check(bool pass, string description) { if (!pass) throw new Exception("FAIL: " + description); assertions++; Console.WriteLine("PASS " + description); }
static void CheckResult(GameResult result) { if (result.Code != GameCodes.Ok) throw new Exception("History append failed: " + result.Message); }
