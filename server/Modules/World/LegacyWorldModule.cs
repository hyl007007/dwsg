using System;
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

        // Called only by the server after its original city battle has established victory.
        public static GameResult ApplyCityVictory(WorldState candidate, string authenticatedAttackerPlayerId,
            int cityX, int cityY, string battleId, Func<int, int, int> random)
        {
            return CityVictoryRules.Apply(candidate, authenticatedAttackerPlayerId, cityX, cityY, battleId, random);
        }
    }
}
