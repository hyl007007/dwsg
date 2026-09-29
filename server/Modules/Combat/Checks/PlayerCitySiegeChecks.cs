using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Dwsg.Server.Modules.Combat;
using Dwsg.Server.Modules.Generals;
using Dwsg.Server.World;
using Dwsg.Shared;
using Dwsg.Shared.Combat;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

internal static class PlayerCitySiegeChecks
{
    public static void RunMilitiaIsolation(string seedPath)
    {
        var world = new WorldState { Data = JObject.Parse(File.ReadAllText(seedPath)),
            EntityMappings = new JObject { ["players"] = new JObject { ["original-2"] = 2 } } };
        JObject original = world.RequirePlayer("original-2"); string before = original.ToString(Formatting.None);
        JObject scratch = (JObject)typeof(CombatModule).GetMethod("CityMilitiaPlayer", BindingFlags.NonPublic | BindingFlags.Static)
            .Invoke(null, new object[] { world, "original-2" });
        var random = new CombatRandom(391020);
        JObject unit = CityGarrisonRules.CreateMilitiaGeneral(99, scratch, (JArray)world.Data["将领配置"], (JObject)world.Data["姓名配置"],
            world.Data.Value<double>("难度"), DateTimeOffset.UtcNow.ToUnixTimeSeconds(), random.Next);
        if (ReferenceEquals(original, scratch) || original.ToString(Formatting.None) != before || unit["将领配兵"].Value<double>("数量") <= 0)
            throw new InvalidOperationException("Original militia generation changed a formal NPC or produced an invalid original army.");
        Console.WriteLine("PASS original level99 temporary militia generated without altering formal NPC technology equipment or generals");
    }

    public static void Run(string seedPath)
    {
        // Native原数据测试角色；不代表PHP真人账号或自然取得的玩家城。
        var world = new WorldState { WorldId = "siege-native-gates", Data = JObject.Parse(File.ReadAllText(seedPath)),
            EntityMappings = new JObject { ["players"] = new JObject(), ["humanPlayers"] = new JObject() } };
        var players = (JObject)world.EntityMappings["players"];
        for (int index = 0; index < ((JArray)world.Data["玩家列表"]).Count; index++) players["original-" + index] = index;
        long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        int checks = 0;
        void Check(bool value, string name) { if (!value) throw new InvalidOperationException(name); checks++; Console.WriteLine("PASS " + name); }
        string Role(string name, string nation)
        {
            Check(LegacyWorldModule.CreatePlayer(world, name, nation, now, out int index).Code == GameCodes.Ok, "original role " + name);
            string id = "native-" + name; players[id] = index; world.EntityMappings["humanPlayers"][id] = true; return id;
        }
        string attacker = Role("siege-attacker", "汉"), defender = Role("siege-defender", "魏");
        GeneralsModule.EnsureMappings(world);
        var validate = typeof(CombatModule).GetMethod("ValidateCitySiegeTarget", BindingFlags.NonPublic | BindingFlags.Static);
        var militia = typeof(CombatModule).GetMethod("CityMilitiaPlayer", BindingFlags.NonPublic | BindingFlags.Static);
        GameResult Gate(JObject city) => (GameResult)validate.Invoke(null, new object[] { world, attacker, city });
        Check(((JArray)world.Data["城池列表"]).Count == 942 && ((JArray)world.Data["山贼列表"]).Count == 400, "actual original 942 city and 400 bandit export");
        JObject target = ((JArray)world.Data["城池列表"]).OfType<JObject>().First(city => city.Value<string>("国家") == "魏");
        Check(Gate(target).Code == GameCodes.Ok, "existing NPC enemy city remains accepted");
        target["城主"] = world.ResolvePlayerIndex(defender);
        Check(Gate(target).Code == GameCodes.Ok, "valid bound human enemy city accepted");
        string before = world.Data.ToString(Formatting.None), bindings = world.EntityMappings.ToString(Formatting.None);
        Check(Gate(target).Code == GameCodes.Ok && before == world.Data.ToString(Formatting.None) && bindings == world.EntityMappings.ToString(Formatting.None), "target checks have no candidate writes");
        target["国家"] = "汉";
        Check(Gate(target).Code == GameCodes.Conflict, "same nation friendly city rejected");
        target["国家"] = "魏"; target["城主"] = world.ResolvePlayerIndex(attacker);
        Check(Gate(target).Code == GameCodes.Conflict, "own city rejected even under another nation");
        target["城主"] = 999999;
        Check(Gate(target).Code == GameCodes.Unavailable, "invalid owner index rejected");
        target["城主"] = 0;
        Check(Gate(target).Code == GameCodes.Unavailable, "unbound original local player is not a server human owner");
        target["城主"] = world.ResolvePlayerIndex(defender); world.EntityMappings["humanPlayers"][defender] = false;
        Check(Gate(target).Code == GameCodes.Unavailable, "unbound human mapping rejected");
        world.EntityMappings["humanPlayers"][defender] = true;
        target["国家"] = "不存在的国家";
        Check(Gate(target).Code == GameCodes.Unavailable, "missing owning nation rejected");
        target["国家"] = "魏";
        JObject nation = ((JArray)world.Data["国家列表"]).OfType<JObject>().Single(item => item.Value<string>("国号") == "魏");
        JToken king = nation["国王"].DeepClone(); nation["国王"] = -1;
        Check(Gate(target).Code == GameCodes.Unavailable, "invalid nation king rejected");
        nation["国王"] = king;
        Check(Gate(target).Code == GameCodes.Ok, "current valid owner recovered");
        target["城主"] = world.ResolvePlayerIndex(attacker);
        Check(Gate(target).Code == GameCodes.Conflict, "arrival observes ownership changed since dispatch");
        target["城主"] = -1; target["国家"] = "";
        Check(Gate(target).Code == GameCodes.Ok, "original neutral city remains accepted");
        Check(Gate(null).Code == GameCodes.NotFound, "missing city rejected");
        JObject human = world.RequirePlayer(defender); before = human.ToString(Formatting.None);
        JObject scratch = (JObject)militia.Invoke(null, new object[] { world, defender });
        scratch["科技信息"]["统帅能力"] = 65.0;
        Check(!ReferenceEquals(scratch, human) && human.ToString(Formatting.None) == before, "temporary militia cannot overwrite real king technology");
        JObject npc = world.RequirePlayer("original-2"); before = npc.ToString(Formatting.None);
        scratch = (JObject)militia.Invoke(null, new object[] { world, "original-2" }); scratch["科技信息"]["统帅能力"] = 65.0;
        Check(!ReferenceEquals(scratch, npc) && npc.ToString(Formatting.None) == before, "temporary militia cannot overwrite formal NPC technology either");
        Console.WriteLine("PLAYER CITY SIEGE TARGET CHECKS " + checks + " PASSED; Native test identities only");
    }
}
