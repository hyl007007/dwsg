using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Administration;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Administration
{
    public sealed class AdministrationModule : IGameModule, IGameTickModule
    {
        public IReadOnlyCollection<string> CommandTypes { get; } = new[] { "nation.publish", "nation.appoint", "city.repair", "city.collect", "city.apply", "city.appoint", "city.bookmark", "administration.tick" };
        public IEnumerable<GameCommand> CollectDueCommands(WorldState state, long serverUtcMs)
        {
            if (AdministrationRules.HasDue(state, serverUtcMs / 1000))
                yield return new GameCommand { WorldId = state.WorldId, Type = "administration.tick", RequestId = "administration:" + state.Revision + ":" + serverUtcMs / 1000 };
        }
        private static bool Shape(JObject p, params string[] names) { return p != null && p.Properties().Count() == names.Length && p.Properties().All(v => names.Contains(v.Name)); }
        private static bool Text(JObject p, string name, bool nullable = false) { return p[name]?.Type == JTokenType.String || nullable && p[name]?.Type == JTokenType.Null; }
        private static bool Integer(JObject p, string name, out int value) { value = 0; return p[name]?.Type == JTokenType.Integer && int.TryParse(p[name].ToString(), out value); }
        public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
        {
            var actor = context?.Actor;
            if (actor == null || actor.WorldId != candidate.WorldId || command.WorldId != candidate.WorldId) return GameResult.Reject(GameCodes.Forbidden, "无权操作此世界。");
            var p = command.Payload; GameResult result;
            if (command.Type == "administration.tick")
            {
                if (!actor.IsSystem) return GameResult.Reject(GameCodes.Forbidden, "仅服务器可以结算城池内政。");
                if (!Shape(p)) return GameResult.Reject(GameCodes.InvalidArgument, "结算参数无效。");
                if (!AdministrationRules.HasDue(candidate, context.ServerUtcMs / 1000)) return GameResult.Reject(GameCodes.Conflict, "尚未到结算时间。");
                return AdministrationRules.Settle(candidate, context.ServerUtcMs / 1000);
            }
            if (actor.IsSystem || string.IsNullOrEmpty(actor.PlayerId) || candidate.EntityMappings["humanPlayers"]?[actor.PlayerId]?.Type != JTokenType.Boolean || !candidate.EntityMappings["humanPlayers"].Value<bool>(actor.PlayerId))
                return GameResult.Reject(GameCodes.Forbidden, "此连接没有可操作的真人角色。");
            if (command.Type == "nation.publish")
            {
                if (!Shape(p, "tag", "kind", "text", "expectedText") || !Text(p, "tag") || !Text(p, "kind") || !Text(p, "text") || !Text(p, "expectedText")) return Invalid();
                result = AdministrationRules.Publish(candidate, actor.PlayerId, p.Value<string>("tag"), p.Value<string>("kind"), p.Value<string>("text"), p.Value<string>("expectedText"));
            }
            else if (command.Type == "nation.appoint")
            {
                if (!Shape(p, "tag", "office", "memberId", "expectedHolder") || !Text(p, "tag") || !Text(p, "office") || !Text(p, "memberId", true) || !Text(p, "expectedHolder", true)) return Invalid();
                result = AdministrationRules.AppointNation(candidate, actor.PlayerId, p.Value<string>("tag"), p.Value<string>("office"), p.Value<string>("memberId"), p.Value<string>("expectedHolder"));
            }
            else
            {
                int x, y, kind;
                if (p == null || !Integer(p, "x", out x) || !Integer(p, "y", out y) || x < 1 || y < 1) return Invalid();
                if (command.Type == "city.bookmark")
                {
                    if (!Shape(p, "x", "y", "add") || p["add"]?.Type != JTokenType.Boolean) return Invalid();
                    result = AdministrationRules.Bookmark(candidate, actor.PlayerId, x, y, p.Value<bool>("add"));
                }
                else if (command.Type == "city.apply")
                {
                    if (!Shape(p, "x", "y", "nation") || !Text(p, "nation")) return Invalid();
                    result = AdministrationRules.Apply(candidate, actor.PlayerId, x, y, p.Value<string>("nation"));
                }
                else if (command.Type == "city.appoint")
                {
                    if (!Shape(p, "x", "y", "memberId", "owner", "nation") || !Text(p, "memberId") || !Text(p, "owner", true) || !Text(p, "nation")) return Invalid();
                    result = AdministrationRules.AppointCity(candidate, actor.PlayerId, x, y, p.Value<string>("memberId"), p.Value<string>("owner"), p.Value<string>("nation"));
                }
                else if (command.Type == "city.repair")
                {
                    double before;
                    if (!Shape(p, "x", "y", "kind", "before", "owner", "nation") || !Integer(p, "kind", out kind) || !AdministrationRules.Number(p["before"], out before) || !Text(p, "owner", true) || !Text(p, "nation")) return Invalid();
                    result = AdministrationRules.Repair(candidate, actor.PlayerId, x, y, kind, before, p.Value<string>("owner"), p.Value<string>("nation"), context.ServerUtcMs / 1000);
                }
                else if (command.Type == "city.collect")
                {
                    double copper, food;
                    if (!Shape(p, "x", "y", "kind", "owner", "nation", "copper", "food") || !Integer(p, "kind", out kind) || !AdministrationRules.Number(p["copper"], out copper) || !AdministrationRules.Number(p["food"], out food) || !Text(p, "owner", true) || !Text(p, "nation")) return Invalid();
                    result = AdministrationRules.Collect(candidate, actor.PlayerId, x, y, kind, p.Value<string>("owner"), p.Value<string>("nation"), copper, food, context.ServerUtcMs / 1000);
                }
                else return GameResult.Reject(GameCodes.NotFound, "命令不存在。");
            }
            if (result.Code == GameCodes.Ok) result.Events.Add(new GameEvent { WorldId = candidate.WorldId, Type = command.Type + ".completed", ServerUtcMs = context.ServerUtcMs, AudiencePlayerIds = new[] { actor.PlayerId } });
            return result;
        }
        private static GameResult Invalid() { return GameResult.Reject(GameCodes.InvalidArgument, "国家或城池操作参数无效。"); }

        // Called by the host's authorized projection after its original public rows exist.
        public static void EnrichProjection(WorldState state, AuthenticatedActor actor, JObject world, JObject privatePlayer)
        {
            var players = (JArray)state.Data["玩家列表"];
            foreach (JObject row in (JArray)world["玩家列表"])
            {
                int index = AdministrationRules.PlayerIndex(state, row.Value<string>("playerId"));
                var original = players[index];
                foreach (string field in new[] { "战功", "贡献" }) row["基础信息"][field] = original["基础信息"]?[field]?.DeepClone();
                row["公开封地数"] = (original["封地信息表"] as JArray)?.Count ?? 0;
                row["公开将领数"] = (original["封地信息表"] as JArray)?.OfType<JObject>().Sum(f => (f["将领信息表"] as JArray)?.Count ?? 0) ?? 0;
            }
            foreach (JObject row in (JArray)world["国家列表"])
            {
                var original = TerritoryRules.Nation(state, row.Value<string>("国号"));
                foreach (string field in new[] { "上次轮选时间", "轮选时间间隔" }) row[field] = original[field]?.DeepClone();
            }
            int actorIndex = actor == null ? -1 : AdministrationRules.PlayerIndex(state, actor.PlayerId);
            var originalCities = ((JArray)state.Data["城池列表"]).OfType<JObject>().ToDictionary(c => c.Value<int>("坐标x") + ":" + c.Value<int>("坐标y"));
            foreach (JObject row in (JArray)world["城池列表"])
            {
                var original = originalCities[row.Value<int>("坐标x") + ":" + row.Value<int>("坐标y")];
                if (!AdministrationRules.Friendly(state, original, actorIndex)) continue;
                foreach (string field in new[] { "城主征收_铜", "城主征收_粮", "国家征收_铜", "国家征收_粮" }) row[field] = original[field]?.DeepClone();
                row["居民封地"] = new JArray((original["城池封地列表"] as JArray ?? new JArray()).OfType<JObject>().Select(f => new JObject {
                    ["playerId"] = AdministrationRules.StablePlayer(state, f.Value<int>("第几个玩家")), ["fiefId"] = f.Value<int>("封地ID标识"),
                    ["name"] = (players[f.Value<int>("第几个玩家")]["封地信息表"] as JArray)?.OfType<JObject>().FirstOrDefault(v => v.Value<int>("ID") == f.Value<int>("封地ID标识"))?.Value<string>("封地名字") ?? "封地" }));
                var defenders = new JArray();
                foreach (var reference in (original["城池驻防列表"] as JArray ?? new JArray()).OfType<JObject>())
                {
                    int index = reference.Value<int>("第几个玩家");
                    if (index < 0 || index >= players.Count) continue;
                    var general = (players[index]["封地信息表"] as JArray ?? new JArray()).OfType<JObject>()
                        .SelectMany(f => (f["将领信息表"] as JArray ?? new JArray()).OfType<JObject>())
                        .FirstOrDefault(g => g["将领属性"]?["初始属性"]?.Value<int>("ID") == reference.Value<int>("将领ID标识"));
                    if (general != null) defenders.Add(Defender(general, players[index]["基础信息"]?.Value<string>("名字") ?? "君主"));
                }
                foreach (var general in (original["城池玩家驻防列表"] as JArray ?? new JArray()).OfType<JObject>()) defenders.Add(Defender(general, "参战部队"));
                row["驻防摘要"] = defenders;
            }
            var projected = new JObject { ["Repairs"] = new JArray(), ["Taxes"] = new JArray(), ["Candidates"] = new JArray(), ["Bookmarks"] = new JArray() };
            var stateRows = state.EntityMappings["administration"] as JObject;
            if (actor != null && stateRows != null)
                foreach (string kind in new[] { "Repairs", "Taxes", "Candidates", "Bookmarks" })
                    foreach (var row in (stateRows[kind] as JArray ?? new JArray()).OfType<JObject>())
                    {
                        bool own = row.Value<string>("PlayerId") == actor.PlayerId;
                        if (kind == "Bookmarks" ? own : own || AdministrationRules.Friendly(state, TerritoryRules.City(state, row.Value<int>("X"), row.Value<int>("Y")), actorIndex))
                            ((JArray)projected[kind]).Add(row.DeepClone());
                    }
            privatePlayer["administration"] = projected;
        }
        private static JObject Defender(JObject general, string owner)
        {
            return new JObject { ["name"] = general["将领属性"]?["初始属性"]?.Value<string>("名字") ?? "将领", ["troops"] = general["将领配兵"]?.Value<double>("数量") ?? 0, ["owner"] = owner };
        }
    }
}
