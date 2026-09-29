using System.Linq;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;

namespace Dwsg.Server.Modules.Combat
{
    public sealed partial class CombatModule
    {
        private static GameResult SetStrategy(WorldState world, CommandContext context, JObject payload)
        {
            if (!Keys(payload, "battleId", "mode") || !Id(payload["battleId"], out string id) || !Coordinate(payload["mode"], out int mode) || mode > 2)
                return GameResult.Reject(GameCodes.InvalidArgument, "请选择有效战场和攻击策略");
            var battle = Load(world, id);
            if (battle == null) return GameResult.Reject(GameCodes.NotFound, "战场不存在");
            if (battle.SettlementApplied || battle.Phase != "fighting") return GameResult.Reject(GameCodes.Conflict, "战场尚未开始或已经结束");
            var units = CurrentGenerals(world, battle).Where(u => (u.GeneralOwnerId ?? battle.PlayerId) == context.Actor.PlayerId && u.Remaining > 0).ToArray();
            if (units.Length == 0) return GameResult.Reject(GameCodes.Forbidden, "当前角色没有本场参战部队");
            RefreshCurrentGenerals(world, battle);
            foreach (var unit in units)
            {
                unit.General["详细信息"]["攻击模式"] = mode;
                var original = Generals.GeneralsModule.ResolveGeneral(world, context.Actor.PlayerId, unit.GeneralId, out _);
                original["详细信息"]["攻击模式"] = mode;
            }
            PublishCurrentGenerals(world, battle);
            Save(world, battle);
            return Updated(world, context, battle, "combat.bandit.updated");
        }
    }
}
