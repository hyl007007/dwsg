using Dwsg.Shared;
using Dwsg.Shared.Economy;

namespace Dwsg.Server.World
{
    public static class LegacyWorldModule
    {
        public static GameResult CreatePlayer(WorldState candidate, string nickname, string nation,
            long serverUtcMs, out int legacyPlayerIndex)
        {
            return LegacyWorldRules.CreatePlayer(candidate, nickname, nation, serverUtcMs, out legacyPlayerIndex);
        }
    }
}
