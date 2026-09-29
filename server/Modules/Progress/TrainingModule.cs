using System;
using System.Collections.Generic;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Dwsg.Shared.Generals;
using Dwsg.Server.Modules.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Progress
{
    // 原窗口4修炼/活血丹规则的服务器入口；客户端只给稳定将领编号。
    public sealed class TrainingModule : IGameModule
    {
        public IReadOnlyCollection<string> CommandTypes { get; } = new[] { "generals.train", "generals.restoreStamina" };
        public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
        {
            var actor = context?.Actor;
            if (actor == null || actor.IsSystem || actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId ||
                candidate.EntityMappings["humanPlayers"]?[actor.PlayerId]?.Type != JTokenType.Boolean || !candidate.EntityMappings["humanPlayers"].Value<bool>(actor.PlayerId))
                return GameResult.Reject(GameCodes.Forbidden, "请先登录真人角色");
            if (command.Payload == null || command.Payload.Count != 1 || command.Payload["generalId"]?.Type != JTokenType.String)
                return GameResult.Reject(GameCodes.InvalidArgument, "将领参数无效");
            try
            {
                string id = command.Payload.Value<string>("generalId");
                JObject original = GeneralsModule.ResolveGeneral(candidate, actor.PlayerId, id, out _);
                LegacyGenerals.RequireIdle(original);
                if (candidate.EntityMappings["generalOccupancy"]?[id] != null) return GameResult.Reject(GameCodes.Conflict, "将领已在出征或驻防军队中");
                var player = (JObject)candidate.RequirePlayer(actor.PlayerId).DeepClone();
                var general = LegacyGenerals.General(player, LegacyGenerals.Integer(original["ID"]), out _);
                var details = LegacyGenerals.Object(general["详细信息"]);
                double stamina = LegacyGenerals.Number(details["剩余体力"]);
                if (stamina < 0) return GameResult.Reject(GameCodes.InvalidArgument, "体力数据无效");
                GameResult result;
                if (command.Type == "generals.train")
                {
                    var points = LegacyGenerals.Object(general["将领属性"]["成长点数"]);
                    double level = LegacyGenerals.Number(points["等级"]), experience = LegacyGenerals.Number(details["经验"]);
                    double balance = LegacyGenerals.Number(player["财产信息"]["铜钱"]);
                    if (level < 1 || level >= 99 || level != Math.Floor(level) || experience < 0 || experience >= GeneralExperienceRules.RequiredForLevel(level) || LegacyGenerals.Number(points["总分配点数"]) < 0)
                        return GameResult.Reject(GameCodes.Conflict, "将领已满级或经验数据无效");
                    double cost = level * 100, gain = Math.Ceiling(GeneralExperienceRules.RequiredForLevel(level) / 10);
                    if (balance < cost || stamina < 10) return GameResult.Reject(GameCodes.InsufficientFunds, "铜钱或体力不足，未开始训练");
                    player["财产信息"]["铜钱"] = balance - cost; details["剩余体力"] = stamina - 10;
                    GeneralExperienceRules.Add(general, gain);
                    GeneralAttributeRules.Recalculate(player, context.ServerUtcMs / 1000);
                    result = GameResult.Success(new JObject { ["generalId"] = id, ["experience"] = gain, ["copper"] = cost });
                    result.Message = "修炼完成：经验+" + gain + "，体力-10，铜钱-" + cost + "。";
                }
                else if (command.Type == "generals.restoreStamina")
                {
                    double maximum = LegacyGenerals.Number(general["将领属性"]["最终属性"]["体力上限"]);
                    if (maximum <= 0 || stamina >= maximum) return GameResult.Reject(GameCodes.Conflict, "体力已满或数据无效，不消耗道具");
                    result = InventoryRules.ConsumeOne(player, candidate.Data["道具配置"] as JArray, "活血丹");
                    if (result.Code != GameCodes.Ok) return result;
                    double gain = Math.Min(50, maximum - stamina); details["剩余体力"] = stamina + gain;
                    result.Data["generalId"] = id; result.Data["stamina"] = gain;
                    result.Message = "使用活血丹1个，体力+" + gain + "。";
                }
                else return GameResult.Reject(GameCodes.InvalidArgument, "将领命令不存在");
                ((JArray)candidate.Data["玩家列表"])[candidate.ResolvePlayerIndex(actor.PlayerId)] = player;
                result.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = command.Type, ServerUtcMs = context.ServerUtcMs, Data = result.Data, AudiencePlayerIds = new[] { actor.PlayerId } });
                return result;
            }
            catch (GeneralRuleException error)
            { return GameResult.Reject(error.Failure == GeneralFailure.NotFound ? GameCodes.NotFound : error.Failure == GeneralFailure.Conflict ? GameCodes.Conflict : GameCodes.InvalidArgument, error.Message); }
        }
    }
}
