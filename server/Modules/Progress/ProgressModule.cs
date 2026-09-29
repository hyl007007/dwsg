using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Window1;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Progress
{
    // 状态与玩家资产在 WorldRuntime 的同一事务里提交；请求回执沿用原运行时。
    public sealed class ProgressModule : IGameModule
    {
        private static readonly SixMinistriesConfig Rules = new SixMinistriesConfig();
        public IReadOnlyCollection<string> CommandTypes { get; } = new[] { "progress.claimGoal", "progress.readMail", "progress.claimMail", "progress.deleteMail", "progress.readNotice",
            "ministries.recruit", "ministries.plant", "ministries.relief", "ministries.study", "ministries.military", "ministries.persuade", "ministries.dismiss", "ministries.claim", "ministries.cancel" };
        private static DateTime Calendar(long utcMs) => DateTimeOffset.FromUnixTimeMilliseconds(utcMs).ToOffset(TimeSpan.FromHours(8)).DateTime;
        private static JObject Journals(WorldState state)
        {
            if (state.EntityMappings["progressJournals"] == null) state.EntityMappings["progressJournals"] = new JObject();
            return state.EntityMappings["progressJournals"] as JObject ?? throw new InvalidOperationException("任务状态无效");
        }
        private static Window1WorldState Journal(WorldState state, string playerId)
        {
            var token = (state.EntityMappings["progressJournals"] as JObject)?[playerId];
            var value = new Window1WorldState { WorldId = state.WorldId };
            if (token != null) value.Players.Add(token.ToObject<PlayerJournal>());
            if (!value.Validate(state.WorldId, out _) || value.Players.Any(p => p.PlayerKey != playerId)) throw new InvalidOperationException("任务状态无效");
            return value;
        }
        private static SixMinistriesState Ministries(WorldState state)
        {
            return state.EntityMappings["progressMinistries"]?.ToObject<SixMinistriesState>() ?? SixMinistriesState.Empty(state.WorldId, Rules);
        }
        public static void RefreshPlayer(WorldState state, string playerId, long serverUtcMs)
        {
            var adapter = new ProgressWorldAdapter(state, playerId);
            if (!adapter.IsCurrent) throw new InvalidOperationException("任务需要真人角色");
            if (!adapter.TryRead(out var current, out var progressError)) throw new InvalidOperationException(progressError);
            var metrics = JObject.FromObject(current);
            string day = Calendar(serverUtcMs).ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
            var observations = state.EntityMappings["progressObservations"] as JObject;
            var observed = observations?[playerId] as JObject;
            var saved = (state.EntityMappings["progressJournals"] as JObject)?[playerId] as JObject;
            // 战斗每帧提交不代表任务进度变更。先检查小型指标缓存，避免反复解析整箱邮件。
            if (saved != null && observed?.Value<string>("day") == day && JToken.DeepEquals(observed["metrics"], metrics) && !HasUndeliveredBattle(state, playerId, saved)) return;
            var journal = Journal(state, playerId);
            var service = new JournalService(journal, adapter, () => Calendar(serverUtcMs));
            if (!service.Refresh(out var error)) throw new InvalidOperationException(error);
            // 只投递实际已结算战斗的战报；战利品已经由战斗事务结算，不再次附奖。
            if (state.Data["战斗运行"] is JObject battles)
                foreach (var entry in battles.Properties())
                {
                    if (!(entry.Value is JObject battle) || battle.Value<string>("PlayerId") != playerId || !battle.Value<bool>("SettlementApplied")) continue;
                    string id = "battle:" + entry.Name;
                    if (journal.Players[0].DeliveredMailIds.Contains(id)) continue;
                    service.ReceiveLocalMail(new LocalMail { Id = id, Sender = "军情驿站", Title = "战斗结算战报",
                        Body = "坐标：" + battle.Value<int>("X") + "," + battle.Value<int>("Y") + "\n战斗已经结算。部队与战利品以服务器当前记录为准，此战报不重复发放奖励。",
                        SentUtcTicks = DateTimeOffset.FromUnixTimeMilliseconds(serverUtcMs).UtcTicks }, out _);
                }
            Journals(state)[playerId] = JObject.FromObject(journal.Players[0]);
            if (observations == null) state.EntityMappings["progressObservations"] = observations = new JObject();
            observations[playerId] = new JObject { ["day"] = day, ["metrics"] = metrics };
        }
        private static bool HasUndeliveredBattle(WorldState state, string playerId, JObject journal)
        {
            var mails = journal["Mails"] as JArray;
            var delivered = journal["DeliveredMailIds"] as JArray;
            if (mails == null || delivered == null) return true;
            // 满箱时保留服务器战斗记录，清理信箱后再投递；不在每个战斗帧构造失败邮件。
            if (mails.Count >= 100 || delivered.Count >= 10000 || !(state.Data["战斗运行"] is JObject battles)) return false;
            var ids = new HashSet<string>(delivered.Values<string>(), StringComparer.Ordinal);
            return battles.Properties().Any(entry => entry.Value is JObject battle && battle.Value<string>("PlayerId") == playerId &&
                battle.Value<bool>("SettlementApplied") && !ids.Contains("battle:" + entry.Name));
        }
        public static void RefreshAll(WorldState state, long serverUtcMs)
        {
            foreach (var entry in (state.EntityMappings["humanPlayers"] as JObject ?? new JObject()).Properties())
                if (entry.Value.Type == JTokenType.Boolean && entry.Value.Value<bool>()) RefreshPlayer(state, entry.Name, serverUtcMs);
        }
        public static JObject Project(WorldState state, string playerId, long serverUtcMs)
        {
            if (string.IsNullOrEmpty(playerId)) return new JObject();
            // 投影是纯读取。未初始化的旧世界由首次 progress 命令/服务器准备阶段持久化。
            var journal = Journal(state, playerId);
            if (journal.Players.Count == 0)
            {
                var view = new JournalService(journal, new ProgressWorldAdapter(state, playerId), () => Calendar(serverUtcMs));
                if (!view.Refresh(out var error)) throw new InvalidOperationException(error);
            }
            var ministries = Ministries(state);
            ministries.Officers.RemoveAll(o => o.Owner != playerId && !Rules.NpcOwner(o.Owner));
            ministries.Jobs.RemoveAll(j => j.Owner != playerId);
            ministries.Buffs.RemoveAll(b => b.Owner != playerId);
            return new JObject { ["journal"] = JObject.FromObject(journal), ["ministries"] = JObject.FromObject(ministries), ["serverUtcMs"] = serverUtcMs };
        }
        public static double ReadBuffPercent(WorldState state, string playerId, bool attack, long serverUtcMs)
        {
            var buffs = state.EntityMappings["progressMinistries"]?["Buffs"] as JArray;
            int kind = (int)(attack ? MinistryJobKind.MilitaryAttack : MinistryJobKind.MilitaryDefense);
            long now = serverUtcMs / 1000;
            var buff = buffs?.OfType<JObject>().SingleOrDefault(b => b.Value<string>("Owner") == playerId && b.Value<int>("Kind") == kind && b.Value<long>("StartedAt") <= now && now < b.Value<long>("ExpiresAt"));
            return buff == null ? 0 : buff.Value<double>("Bonus") * 100;
        }
        public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
        {
            var actor = context?.Actor;
            if (actor == null || actor.IsSystem || actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId ||
                candidate.EntityMappings["humanPlayers"]?[actor.PlayerId]?.Type != JTokenType.Boolean || !candidate.EntityMappings["humanPlayers"].Value<bool>(actor.PlayerId))
                return GameResult.Reject(GameCodes.Forbidden, "此连接没有可操作政务的真人角色");
            string field = command.Type.StartsWith("progress.", StringComparison.Ordinal) ? "id" :
                command.Type == "ministries.recruit" ? "candidate" : command.Type == "ministries.plant" ? "plot" : command.Type == "ministries.military" ? "attack" : "id";
            var payload = command.Payload;
            if (payload == null || payload.Count != 1 || payload[field] == null) return GameResult.Reject(GameCodes.InvalidArgument, "政务参数无效");
            if (command.Type.StartsWith("progress.", StringComparison.Ordinal))
            {
                if (payload[field].Type != JTokenType.String || !SixMinistriesConfig.Text(payload.Value<string>(field), 160)) return GameResult.Reject(GameCodes.InvalidArgument, "任务邮件标识无效");
                RefreshPlayer(candidate, actor.PlayerId, context.ServerUtcMs);
                var journal = Journal(candidate, actor.PlayerId);
                var service = new JournalService(journal, new ProgressWorldAdapter(candidate, actor.PlayerId), () => Calendar(context.ServerUtcMs));
                string id = payload.Value<string>(field), error = null; bool ok;
                switch (command.Type)
                {
                    case "progress.claimGoal": ok = service.ClaimGoal(id, out error); break;
                    case "progress.readMail": ok = service.ReadMail(id); error = "信件不存在"; break;
                    case "progress.claimMail": ok = service.ClaimMail(id, out error); break;
                    case "progress.deleteMail": ok = service.DeleteMail(id, out error); break;
                    case "progress.readNotice":
                        ok = id == "builtin.offline" || id == "builtin.progress" || id == "builtin.rules";
                        if (ok) service.ReadNotice(id); else error = "告示不存在";
                        break;
                    default: return GameResult.Reject(GameCodes.InvalidArgument, "政务命令不存在");
                }
                if (!ok) return GameResult.Reject(GameCodes.Conflict, error ?? "操作未完成");
                Journals(candidate)[actor.PlayerId] = JObject.FromObject(journal.Players[0]);
                var result = GameResult.Success(new JObject { ["id"] = id });
                result.Message = command.Type == "progress.claimGoal" || command.Type == "progress.claimMail" ? "已领取，奖励已到账。" : "操作完成。";
                result.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = command.Type + "ed", ServerUtcMs = context.ServerUtcMs, AudiencePlayerIds = new[] { actor.PlayerId } });
                return result;
            }
            long number = 0;
            if (field == "attack" ? payload[field].Type != JTokenType.Boolean : payload[field].Type != JTokenType.Integer || !long.TryParse(payload[field].ToString(), out number) || number < 0 || number >= Rules.MaxIdentity)
                return GameResult.Reject(GameCodes.InvalidArgument, "六部参数无效");
            var model = new SixMinistriesService(Ministries(candidate), Rules, new ProgressWorldAdapter(candidate, actor.PlayerId), () => context.ServerUtcMs / 1000);
            MinistryResult changed;
            switch (command.Type)
            {
                case "ministries.recruit": changed = number <= int.MaxValue ? model.StartRecruit((int)number) : MinistryResult.Fail("候选不存在"); break;
                case "ministries.plant": changed = number <= int.MaxValue ? model.StartPlant((int)number) : MinistryResult.Fail("地块不存在"); break;
                case "ministries.relief": changed = model.StartRelief(model.Roster.FirstOrDefault(o => o.Id == number)); break;
                case "ministries.study": changed = model.StartStudy(model.Roster.FirstOrDefault(o => o.Id == number)); break;
                case "ministries.dismiss": changed = model.Dismiss(model.Roster.FirstOrDefault(o => o.Id == number)); break;
                case "ministries.persuade": changed = model.StartPersuasion(model.NpcTargets.FirstOrDefault(o => o.Id == number)); break;
                case "ministries.military": changed = model.StartMilitary(payload.Value<bool>(field)); break;
                case "ministries.claim": changed = model.Claim(number); break;
                case "ministries.cancel": changed = model.Cancel(number); break;
                default: return GameResult.Reject(GameCodes.InvalidArgument, "六部命令不存在");
            }
            if (!changed.Success) return GameResult.Reject(GameCodes.Conflict, changed.Message);
            candidate.EntityMappings["progressMinistries"] = JObject.FromObject(model.Snapshot());
            var answer = GameResult.Success(new JObject { ["jobId"] = changed.JobId }); answer.Message = changed.Message;
            answer.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = "ministries.changed", ServerUtcMs = context.ServerUtcMs, AudiencePlayerIds = new[] { actor.PlayerId } });
            return answer;
        }
    }
}
