using System;

namespace Dwsg.Shared.Combat
{
    // 原城池信息界面的我方按钮、选择出征index1及Ai军情检测身份78。
    // 常驻列表没有原写入实现；本规则只允许向现有城战加入守方。
    public static class CityPlayerGarrisonRules
    {
        public const int LegacyMarchIdentity = 78;
        public const int BattleType = 1;
        public const int Side = 1;

        public static long ArrivalUtcMs(long serverUtcMs)
        {
            if (serverUtcMs < 0) throw new ArgumentOutOfRangeException(nameof(serverUtcMs));
            // TIME.getTime()先截断为秒，再加5秒，不读取攻城的精确到达输入框。
            return checked((serverUtcMs / 1000 + 5) * 1000);
        }

        public static bool CanDispatch(string playerNation, string cityNation, bool fighting, int routeCost, string nickname)
        {
            return playerNation == cityNation && fighting && (routeCost != -1 || nickname == "997788");
        }

        public static bool CanJoinExistingBattle(BanditBattle battle, int x, int y, long arrivalUtcMs, long serverUtcMs)
        {
            // 原分支在战场消失时漏加驻防队列；服务器不得借此重建AI空战场。
            return battle != null && battle.Kind == "city" && battle.Phase == "fighting"
                && battle.X == x && battle.Y == y && arrivalUtcMs <= serverUtcMs;
        }
    }
}
