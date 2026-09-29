using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Dwsg.Shared.Generals;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.World
{
    public sealed class TechnologyModule : IGameModule
    {
        public IReadOnlyCollection<string> CommandTypes { get; } = new[] { "technology.upgrade" };

        public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
        {
            if (context?.Actor == null || context.Actor.IsSystem || context.Actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId)
                return GameResult.Reject(GameCodes.Forbidden, "此连接没有可研究科技的角色");
            if (command.Type != "technology.upgrade") return GameResult.Reject(GameCodes.NotFound, "命令不存在");
            string owner = context.Actor.PlayerId;
            if (string.IsNullOrEmpty(owner) || candidate.EntityMappings["humanPlayers"]?[owner]?.Type != JTokenType.Boolean ||
                !candidate.EntityMappings["humanPlayers"].Value<bool>(owner))
                return GameResult.Reject(GameCodes.Forbidden, "此连接没有真人角色");
            var payload = command.Payload;
            double plot, level;
            if (payload == null || payload.Properties().Any(p => p.Name != "fiefId" && p.Name != "plot" && p.Name != "technology" && p.Name != "expectedLevel") ||
                payload["fiefId"]?.Type != JTokenType.String || payload["technology"]?.Type != JTokenType.String ||
                payload["plot"]?.Type != JTokenType.Integer || payload["expectedLevel"]?.Type != JTokenType.Integer ||
                !ShopRules.TryNumber(payload["plot"], out plot) || plot < 1 || plot > 12 ||
                !ShopRules.TryNumber(payload["expectedLevel"], out level) || level < 0 || level > 15)
                return GameResult.Reject(GameCodes.InvalidArgument, "科技升级参数无效");
            var mapping = candidate.EntityMappings["fiefs"]?[payload.Value<string>("fiefId")] as JObject;
            if (mapping == null || mapping.Value<string>("playerId") != owner)
                return GameResult.Reject(GameCodes.Forbidden, "封地不属于此角色");
            JObject player;
            try { player = candidate.RequirePlayer(owner); }
            catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Forbidden, "原角色身份无效"); }
            var fief = (player["封地信息表"] as JArray)?.OfType<JObject>().SingleOrDefault(f => f.Value<int>("ID") == mapping.Value<int>("legacyId"));
            if (fief == null) return GameResult.Reject(GameCodes.NotFound, "原封地不存在");
            var upgraded = (JObject)player.DeepClone();
            var upgradedFief = (JObject)upgraded["封地信息表"].Single(f => f.Value<int>("ID") == mapping.Value<int>("legacyId"));
            var result = TechnologyRules.Upgrade(upgraded, upgradedFief, (int)plot, payload.Value<string>("technology"), (int)level);
            if (result.Code == GameCodes.Ok)
            {
                if (payload.Value<string>("technology") == "统帅能力")
                {
                    // Canonical recalculation also resets stamina. Derive on a copy and take only
                    // the technology-dependent capacity, preserving every troop and army state.
                    var derived = (JObject)upgraded.DeepClone();
                    GeneralAttributeRules.Recalculate(derived, context.ServerUtcMs / 1000);
                    for (int f = 0; f < upgraded["封地信息表"].Count(); f++)
                        for (int g = 0; g < upgraded["封地信息表"][f]["将领信息表"].Count(); g++)
                            upgraded["封地信息表"][f]["将领信息表"][g]["将领属性"]["最终属性"]["统兵"] =
                                derived["封地信息表"][f]["将领信息表"][g]["将领属性"]["最终属性"]["统兵"].DeepClone();
                }
                ((JArray)candidate.Data["玩家列表"])[candidate.ResolvePlayerIndex(owner)] = upgraded;
                result.Data["fiefId"] = payload.Value<string>("fiefId"); result.Data["plot"] = (int)plot;
                result.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = "technology.upgraded", Data = result.Data,
                    ServerUtcMs = context.ServerUtcMs, AudiencePlayerIds = new[] { owner } });
            }
            return result;
        }
    }
}
