using Dwsg.Shared;
using Dwsg.Social;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Social;

// 沿用世界事务和请求回执；规则直接编译窗口5的纯 C# 实现，不维护第二套关系规则。
public sealed class SocialModule : IGameModule
{
    public IReadOnlyCollection<string> CommandTypes { get; } = new[] { "social.execute" };

    public GameResult Execute(WorldState candidate, CommandContext context, GameCommand command)
    {
        var actor = context?.Actor;
        if (!IsHuman(candidate, actor) || command.WorldId != candidate.WorldId)
            return GameResult.Reject(GameCodes.Forbidden, "请使用本世界的已登录玩家角色。");
        var payload = command.Payload;
        if (command.Type != "social.execute" || payload == null || payload["kind"]?.Type != JTokenType.String ||
            payload.Properties().Any(p => !new[] { "kind", "target", "entity", "name", "text", "accept" }.Contains(p.Name)) ||
            new[] { "target", "entity", "name", "text" }.Any(k => payload[k] != null && payload[k].Type != JTokenType.String) ||
            payload["accept"] != null && payload["accept"].Type != JTokenType.Boolean ||
            !Enum.TryParse<SocialCommandKind>(payload.Value<string>("kind"), false, out var kind) || !Enum.IsDefined(kind) ||
            kind.ToString() != payload.Value<string>("kind"))
            return GameResult.Reject(GameCodes.InvalidArgument, "社交命令参数无效。");
        if (new[] { "target", "entity" }.Any(k => payload.Value<string>(k)?.Length > 48) ||
            payload.Value<string>("name")?.Length > 20 || payload.Value<string>("text")?.Length > 200)
            return GameResult.Reject(GameCodes.InvalidArgument, "社交内容超过长度限制。");
        var state = Read(candidate, actor.PlayerId);
        var problem = LocalSocialAdapter.Validate(state);
        if (problem != null) return GameResult.Reject(GameCodes.Unavailable, "社交记录无法读取。");
        var store = new SocialLocalStore(state.Players.First(p => p.Id == actor.PlayerId), candidate.WorldId) { State = state };
        var rules = new LocalSocialAdapter(store, actor.PlayerId, true, context.ServerUtcMs / 1000);
        var result = rules.Execute(new SocialCommand { Kind = kind, Target = payload.Value<string>("target"),
            Entity = payload.Value<string>("entity"), Name = payload.Value<string>("name"), Text = payload.Value<string>("text"),
            Accept = payload.Value<bool?>("accept") ?? false });
        if (!result.Succeeded)
            return GameResult.Reject(result.Code == "permission" ? GameCodes.Forbidden : GameCodes.InvalidArgument, result.Message);
        if (LocalSocialAdapter.Validate(state) != null)
            return GameResult.Reject(GameCodes.Unavailable, "社交记录未保存，请重试。");
        candidate.Data["社交状态"] = JObject.FromObject(state);
        result.Code = GameCodes.Ok;
        return GameResult.Success(new JObject { ["socialResult"] = JObject.FromObject(result) });
    }

    public static JObject Project(WorldState world, AuthenticatedActor actor)
    {
        if (!IsHuman(world, actor)) return null;
        var state = Read(world, actor.PlayerId);
        string id = actor.PlayerId;
        state.OwnerId = id;
        state.Friends.RemoveAll(f => f.A != id && f.B != id);
        state.Blocks.RemoveAll(b => b.Owner != id);
        state.FriendRequests.RemoveAll(r => r.From != id && r.To != id);
        var ledGuilds = new HashSet<string>(state.Guilds.Where(g => g.Leader == id).Select(g => g.Id));
        state.GuildApplications.RemoveAll(a => a.Applicant != id && !ledGuilds.Contains(a.Guild));
        state.Invitations.RemoveAll(i => i.From != id && i.To != id);
        state.Mentors.RemoveAll(m => m.Mentor != id && m.Apprentice != id);
        state.Brotherhoods.RemoveAll(b => !b.Members.Contains(id));
        state.Drafts.RemoveAll(d => d.Owner != id);
        state.Messages.RemoveAll(m => m.From != id && m.To != id);
        foreach (var message in state.Messages) message.Delivery = message.To == id ? MessageDelivery.Received : MessageDelivery.Sent;
        return JObject.FromObject(state);
    }

    private static bool IsHuman(WorldState world, AuthenticatedActor actor) => actor != null && !actor.IsSystem &&
        actor.WorldId == world.WorldId && world.EntityMappings["humanPlayers"]?[actor.PlayerId]?.Type == JTokenType.Boolean &&
        world.EntityMappings["humanPlayers"].Value<bool>(actor.PlayerId);

    private static SocialStateDto Read(WorldState world, string owner)
    {
        var state = world.Data["社交状态"]?.ToObject<SocialStateDto>() ?? new SocialStateDto { WorldKey = world.WorldId, OwnerId = owner };
        // 名片仅来自经过 PHP 账户绑定的角色，客户端不能登记、改名或冒充 NPC。
        state.Players = (world.EntityMappings["humanPlayers"] as JObject ?? new JObject()).Properties()
            .Where(p => p.Value.Type == JTokenType.Boolean && p.Value.Value<bool>()).Select(p => {
                var basic = world.RequirePlayer(p.Name)["基础信息"];
                return new SocialPlayerDto { Id = p.Name, Name = basic.Value<string>("名字"), Verified = true,
                    Country = basic.Value<string>("国家") ?? "无", Level = Math.Clamp((int)(basic.Value<double?>("等级") ?? 1), 1, 999),
                    Portrait = Math.Max(0, basic.Value<int?>("头像") ?? 0) };
            }).ToList();
        return state;
    }
}
