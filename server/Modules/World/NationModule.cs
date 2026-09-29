using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.World
{
    public sealed class NationModule : IGameModule
    {
        private readonly Action<WorldState> initializeNation;
        public NationModule(Action<WorldState> initializeNation = null) { this.initializeNation = initializeNation; }

        public IReadOnlyCollection<string> CommandTypes { get; } = new[] { "nation.create", "nation.join", "nation.research", "nation.salary" };

        public static void InitializeSalary(WorldState candidate, string stablePlayerId, long serverUtcMs)
        {
            if (serverUtcMs < 0 || candidate.EntityMappings["humanPlayers"]?[stablePlayerId]?.Type != JTokenType.Boolean ||
                !candidate.EntityMappings["humanPlayers"].Value<bool>(stablePlayerId)) throw new InvalidOperationException("Salary requires a real human role binding.");
            candidate.RequirePlayer(stablePlayerId);
            var clocks = candidate.EntityMappings["nationSalary"] as JObject;
            if (clocks == null)
            {
                if (candidate.EntityMappings["nationSalary"] != null) throw new InvalidOperationException("Original salary clocks invalid.");
                candidate.EntityMappings["nationSalary"] = clocks = new JObject();
            }
            if (clocks[stablePlayerId] == null) clocks[stablePlayerId] = new JObject { ["readyUtcMs"] = checked(serverUtcMs + NationSalaryRules.IntervalMs) };
        }

        public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
        {
            var actor = context?.Actor;
            if (actor == null || actor.IsSystem || string.IsNullOrEmpty(actor.PlayerId) || actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId ||
                candidate.EntityMappings["humanPlayers"]?[actor.PlayerId]?.Type != JTokenType.Boolean || !candidate.EntityMappings["humanPlayers"].Value<bool>(actor.PlayerId))
                return GameResult.Reject(GameCodes.Forbidden, "此连接没有可操作国家的真人角色");
            if (command.Type == "nation.salary")
            {
                var salary = command.Payload; int office;
                if (salary == null || salary.Properties().Any(p => p.Name != "expectedOffice") || salary["expectedOffice"]?.Type != JTokenType.Integer ||
                    !int.TryParse(salary["expectedOffice"].ToString(), out office) || office < 0 || office > 6)
                    return GameResult.Reject(GameCodes.InvalidArgument, "俸禄领取参数无效");
                var claimed = NationSalaryRules.Claim(candidate, actor.PlayerId, office, context.ServerUtcMs);
                if (claimed.Code == GameCodes.Ok) claimed.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = "nation.salaryClaimed", ServerUtcMs = context.ServerUtcMs, Data = claimed.Data, AudiencePlayerIds = new[] { actor.PlayerId } });
                return claimed;
            }
            if (command.Type == "nation.research")
            {
                var research = command.Payload; int level, total;
                if (research == null || research.Properties().Any(p => p.Name != "technology" && p.Name != "expectedLevel" && p.Name != "expectedTotal") ||
                    research["technology"]?.Type != JTokenType.String || research["expectedLevel"]?.Type != JTokenType.Integer || research["expectedTotal"]?.Type != JTokenType.Integer ||
                    !int.TryParse(research["expectedLevel"].ToString(), out level) || !int.TryParse(research["expectedTotal"].ToString(), out total) || level < 0 || total < 0)
                    return GameResult.Reject(GameCodes.InvalidArgument, "国家科技参数无效");
                var upgraded = NationTechnologyRules.Upgrade(candidate, actor.PlayerId, research.Value<string>("technology"), level, total);
                if (upgraded.Code == GameCodes.Ok) upgraded.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = "nation.researched", ServerUtcMs = context.ServerUtcMs, Data = upgraded.Data });
                return upgraded;
            }
            if (command.Type == "nation.join")
            {
                var join = command.Payload;
                if (join == null || join.Properties().Any(p => p.Name != "tag") || join["tag"]?.Type != JTokenType.String)
                    return GameResult.Reject(GameCodes.InvalidArgument, "换国参数无效");
                var joined = NationRules.Join(candidate, actor.PlayerId, join.Value<string>("tag"));
                if (joined.Code == GameCodes.Ok) joined.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = "nation.joined", ServerUtcMs = context.ServerUtcMs, Data = joined.Data });
                return joined;
            }
            if (command.Type != "nation.create") return GameResult.Reject(GameCodes.NotFound, "命令不存在");
            var payload = command.Payload;
            int x, y;
            if (payload == null || payload.Properties().Any(p => p.Name != "name" && p.Name != "tag" && p.Name != "declaration" && p.Name != "cityX" && p.Name != "cityY") ||
                payload["name"]?.Type != JTokenType.String || payload["tag"]?.Type != JTokenType.String || payload["declaration"]?.Type != JTokenType.String ||
                payload["cityX"]?.Type != JTokenType.Integer || payload["cityY"]?.Type != JTokenType.Integer ||
                !int.TryParse(payload["cityX"].ToString(), out x) || !int.TryParse(payload["cityY"].ToString(), out y) || x < 1 || y < 1)
                return GameResult.Reject(GameCodes.InvalidArgument, "建国参数无效");
            var working = candidate.Clone();
            var result = NationRules.Create(working, actor.PlayerId, payload.Value<string>("name"), payload.Value<string>("tag"), payload.Value<string>("declaration"), x, y, context.ServerUtcMs / 1000);
            if (result.Code == GameCodes.Ok)
            {
                try { initializeNation?.Invoke(working); }
                catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Unavailable, "原国家稳定映射无效，建国未提交"); }
                candidate.Data = working.Data; candidate.EntityMappings = working.EntityMappings;
                result.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = "nation.created", ServerUtcMs = context.ServerUtcMs, Data = result.Data });
            }
            return result;
        }
    }
}
