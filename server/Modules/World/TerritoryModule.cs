using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.World
{
    public sealed class TerritoryModule : IGameModule
    {
        private readonly Action<WorldState> initializeEntities;
        public IReadOnlyCollection<string> CommandTypes { get; } = new[] { "fief.create" };

        public TerritoryModule(Action<WorldState> initializeEntities)
        {
            this.initializeEntities = initializeEntities ?? throw new ArgumentNullException(nameof(initializeEntities));
        }

        public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
        {
            if (context?.Actor == null || context.Actor.IsSystem || context.Actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId)
                return GameResult.Reject(GameCodes.Forbidden, "此连接没有可经营的角色");
            if (command.Type != "fief.create") return GameResult.Reject(GameCodes.NotFound, "命令不存在");
            var payload = command.Payload;
            double x, y;
            if (payload == null || payload.Properties().Any(p => p.Name != "cityX" && p.Name != "cityY") ||
                payload["cityX"]?.Type != JTokenType.Integer || payload["cityY"]?.Type != JTokenType.Integer ||
                !ShopRules.TryNumber(payload["cityX"], out x) || !ShopRules.TryNumber(payload["cityY"], out y) ||
                x < 1 || y < 1 || x > int.MaxValue || y > int.MaxValue)
                return GameResult.Reject(GameCodes.InvalidArgument, "城池坐标无效");
            string id = context.Actor.PlayerId;
            if (candidate.EntityMappings["humanPlayers"]?[id]?.Type != JTokenType.Boolean || !candidate.EntityMappings["humanPlayers"].Value<bool>(id))
                return GameResult.Reject(GameCodes.Forbidden, "此连接没有真人角色");
            var result = TerritoryRules.CreateFief(candidate, id, (int)x, (int)y);
            if (result.Code != GameCodes.Ok) return result;
            initializeEntities(candidate);
            int legacyId = result.Data.Value<int>("legacyFiefId");
            var mappings = candidate.EntityMappings["fiefs"] as JObject;
            string stableId = mappings?.Properties().SingleOrDefault(p => p.Value is JObject &&
                p.Value.Value<string>("playerId") == id && p.Value.Value<int>("legacyId") == legacyId)?.Name;
            if (stableId == null) return GameResult.Reject(GameCodes.Unavailable, "新封地身份未生成");
            result.Data = new JObject { ["fiefId"] = stableId, ["cityX"] = (int)x, ["cityY"] = (int)y };
            result.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = "fief.created", Data = result.Data,
                ServerUtcMs = context.ServerUtcMs, AudiencePlayerIds = new[] { id } });
            return result;
        }
    }
}
