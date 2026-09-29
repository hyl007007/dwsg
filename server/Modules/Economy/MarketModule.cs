using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Economy
{
    public sealed class MarketModule : IGameModule
    {
        public IReadOnlyCollection<string> CommandTypes { get; } = new[] { "market.exchange" };

        public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
        {
            if (context?.Actor == null || context.Actor.IsSystem || context.Actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId)
                return GameResult.Reject(GameCodes.Forbidden, "此连接没有可经营的角色");
            if (command.Type != "market.exchange") return GameResult.Reject(GameCodes.NotFound, "命令不存在");
            string id = context.Actor.PlayerId;
            if (string.IsNullOrEmpty(id) || candidate.EntityMappings["humanPlayers"]?[id]?.Type != JTokenType.Boolean ||
                !candidate.EntityMappings["humanPlayers"].Value<bool>(id))
                return GameResult.Reject(GameCodes.Forbidden, "此连接没有真人角色");
            var payload = command.Payload;
            double type, quantity;
            if (payload == null || payload.Properties().Any(p => p.Name != "quoteId" && p.Name != "exchangeType" && p.Name != "quantity") ||
                payload["quoteId"]?.Type != JTokenType.String || payload["exchangeType"]?.Type != JTokenType.Integer ||
                !ShopRules.TryNumber(payload["exchangeType"], out type) || type < 5 || type > 8 ||
                !ShopRules.TryNumber(payload["quantity"], out quantity))
                return GameResult.Reject(GameCodes.InvalidArgument, "市场兑换参数无效");
            JObject player;
            try { player = candidate.RequirePlayer(id); }
            catch (InvalidOperationException) { return GameResult.Reject(GameCodes.Forbidden, "角色不存在于此世界"); }
            var result = MarketRules.Exchange(player, (int)type, quantity, payload.Value<string>("quoteId"));
            if (result.Code == GameCodes.Ok)
                result.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = "market.exchanged", Data = result.Data,
                    ServerUtcMs = context.ServerUtcMs, AudiencePlayerIds = new[] { id } });
            return result;
        }
    }
}
