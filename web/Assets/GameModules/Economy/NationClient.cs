using System;
using System.Collections.Generic;
using System.Linq;
using Dwsg.Shared;
using Dwsg.Shared.Economy;
using Newtonsoft.Json.Linq;
using 玩家数据结构;

public static class NationClient
{
    public static void Donate(string tag, long copper, long grain, Action<GameResult> completed)
    {
        if (Dwsg.Network.GameNetwork.Enabled)
        {
            Dwsg.Network.GameNetwork.SendCommand("nation.donate", new JObject { ["tag"] = tag, ["copper"] = copper, ["grain"] = grain }, completed);
            return;
        }
        int identity = 全局变量.本机身份;
        if (identity < 0 || identity >= 全局变量.所有玩家数据表.Count)
        { completed(GameResult.Reject(GameCodes.RoleRequired, "原角色尚未进入游戏")); return; }
        var world = OfflineWorld(identity);
        var result = NationRules.Donate(world, "offline", tag, copper, grain);
        if (result.Code == GameCodes.Ok)
        {
            var wallet = world.RequirePlayer("offline")["财产信息"];
            var player = 全局变量.所有玩家数据表[identity];
            var nation = 全局方法类.获取指定名字的国家(tag);
            var current = TerritoryRules.Nation(world, tag);
            player.财产信息.铜钱 = wallet.Value<double>("铜钱"); player.财产信息.粮食 = wallet.Value<double>("粮食");
            nation.铜钱 = current.Value<double>("铜钱"); nation.粮食 = current.Value<double>("粮食");
        }
        completed(result);
    }
    public static void ClaimSalary(Action<GameResult> completed)
    {
        int identity = 全局变量.本机身份;
        if (identity < 0 || identity >= 全局变量.所有玩家数据表.Count)
        { completed(GameResult.Reject(GameCodes.RoleRequired, "原角色尚未进入游戏")); return; }
        var player = 全局变量.所有玩家数据表[identity]; int office;
        if (!NationSalaryRules.TryOffice(player.基础信息.战功, out office))
        { completed(GameResult.Reject(GameCodes.Unavailable, "原战功数据无效")); return; }
        if (Dwsg.Network.GameNetwork.Enabled)
        { Dwsg.Network.GameNetwork.SendCommand("nation.salary", new JObject { ["expectedOffice"] = office }, completed); return; }
        var state = OfflineWorld(identity); long utc = TIME.getTime() * 1000;
        state.EntityMappings["nationSalary"] = new JObject { ["offline"] = new JObject { ["readyUtcMs"] = utc + Math.Max(0, 全局变量.领取倒计时) * 1000L } };
        var result = NationSalaryRules.Claim(state, "offline", office, utc);
        if (result.Code == GameCodes.Ok)
        {
            var updated = state.RequirePlayer("offline"); var nation = 全局方法类.获取指定名字的国家(player.基础信息.国家);
            var current = TerritoryRules.Nation(state, player.基础信息.国家);
            player.财产信息.黄金 = updated["财产信息"].Value<double>("黄金"); player.财产信息.铜钱 = updated["财产信息"].Value<double>("铜钱"); player.财产信息.粮食 = updated["财产信息"].Value<double>("粮食");
            player.基础信息.战功 = updated["基础信息"].Value<double>("战功"); player.基础信息.官职 = (官职信息)updated["基础信息"].Value<int>("官职");
            nation.铜钱 = current.Value<double>("铜钱"); nation.粮食 = current.Value<double>("粮食"); 全局变量.领取倒计时 = 300;
        }
        completed(result);
    }

    public static void Research(string technology, Action<GameResult> completed)
    {
        int identity = 全局变量.本机身份;
        if (identity < 0 || identity >= 全局变量.所有玩家数据表.Count)
        { completed(GameResult.Reject(GameCodes.RoleRequired, "原角色尚未进入游戏")); return; }
        var player = 全局变量.所有玩家数据表[identity];
        var nation = 全局方法类.获取指定名字的国家(player.基础信息.国家);
        int level, total;
        if (nation == null || !NationTechnologyRules.TryLevels(JObject.FromObject(nation), technology, out level, out total))
        { completed(GameResult.Reject(GameCodes.Unavailable, "原国家科技尚未同步")); return; }
        if (Dwsg.Network.GameNetwork.Enabled)
        {
            Dwsg.Network.GameNetwork.SendCommand("nation.research", new JObject { ["technology"] = technology,
                ["expectedLevel"] = level, ["expectedTotal"] = total }, completed);
            return;
        }
        var world = OfflineWorld(identity);
        var result = NationTechnologyRules.Upgrade(world, "offline", technology, level, total);
        if (result.Code == GameCodes.Ok)
        {
            var wallet = world.RequirePlayer("offline")["财产信息"];
            player.财产信息.黄金 = wallet.Value<double>("黄金"); player.财产信息.粮食 = wallet.Value<double>("粮食"); player.财产信息.铜钱 = wallet.Value<double>("铜钱");
            var current = TerritoryRules.Nation(world, player.基础信息.国家);
            nation.攻击科技 = current.Value<double>("攻击科技"); nation.防御科技 = current.Value<double>("防御科技"); nation.资源科技 = current.Value<double>("资源科技");
            nation.刷新科技信息();
        }
        completed(result);
    }

    public static void Create(string name, string tag, string declaration, 城池信息库类 city, Action<GameResult> completed)
    {
        if (city == null) { completed(GameResult.Reject(GameCodes.InvalidArgument, "请选择国都城池")); return; }
        if (Dwsg.Network.GameNetwork.Enabled)
        {
            Dwsg.Network.GameNetwork.SendCommand("nation.create", new JObject { ["name"] = name, ["tag"] = tag,
                ["declaration"] = declaration, ["cityX"] = city.坐标x, ["cityY"] = city.坐标y }, completed);
            return;
        }
        int index = 全局变量.本机身份;
        if (index < 0 || index >= 全局变量.所有玩家数据表.Count)
        { completed(GameResult.Reject(GameCodes.RoleRequired, "原角色尚未进入游戏")); return; }
        const string owner = "offline";
        var world = OfflineWorld(index);
        var result = NationRules.Create(world, owner, name, tag, declaration, city.坐标x, city.坐标y, TIME.getTime());
        if (result.Code == GameCodes.Ok) ApplyOffline(world, index, true);
        completed(result);
    }

    public static void Join(string tag, Action<GameResult> completed)
    {
        if (Dwsg.Network.GameNetwork.Enabled)
        { Dwsg.Network.GameNetwork.SendCommand("nation.join", new JObject { ["tag"] = tag }, completed); return; }
        int index = 全局变量.本机身份;
        if (index < 0 || index >= 全局变量.所有玩家数据表.Count)
        { completed(GameResult.Reject(GameCodes.RoleRequired, "原角色尚未进入游戏")); return; }
        var world = OfflineWorld(index);
        var result = NationRules.Join(world, "offline", tag);
        if (result.Code == GameCodes.Ok) ApplyOffline(world, index, false);
        completed(result);
    }

    private static WorldState OfflineWorld(int index)
    {
        return new WorldState { Data = new JObject { ["玩家列表"] = JArray.FromObject(全局变量.所有玩家数据表),
            ["国家列表"] = JArray.FromObject(全局变量.所有国家列表), ["城池列表"] = JArray.FromObject(全局变量.所有城池列表),
            ["道具配置"] = JArray.FromObject(全局道具库.道具列表), ["国家ID记录"] = 全局变量.国家ID记录 },
            EntityMappings = new JObject { ["players"] = new JObject { ["offline"] = index } } };
    }

    private static void ApplyOffline(WorldState world, int index, bool founding)
    {
        var player = 全局变量.所有玩家数据表[index];
        var updated = world.Data["玩家列表"][index];
        var first = player.封地信息表[0];
        foreach (var fief in player.封地信息表.Skip(1)) first.将领信息表.AddRange(fief.将领信息表);
        ApplyTroops(first.闲兵信息表, updated["封地信息表"][0]["闲兵信息表"] as JArray, t => t.ID, (t, n) => t.数量 = n);
        ApplyTroops(first.伤兵信息表, updated["封地信息表"][0]["伤兵信息表"] as JArray, t => t.ID, (t, n) => t.数量 = n);
        player.封地信息表.RemoveRange(1, player.封地信息表.Count - 1);
        first.所在城池.x = updated["封地信息表"][0]["所在城池"].Value<int>("x");
        first.所在城池.y = updated["封地信息表"][0]["所在城池"].Value<int>("y");
        player.基础信息.国家 = updated["基础信息"].Value<string>("国家");
        player.基础信息.官阶 = updated["基础信息"].Value<string>("官阶");
        player.基础信息.战功 = updated["基础信息"].Value<double>("战功");
        player.财产信息.铜钱 = updated["财产信息"].Value<double>("铜钱");
        if (founding)
            foreach (var material in new[] { new { Name = "玉玺", Count = 1 }, new { Name = "虎符", Count = 10 }, new { Name = "印绶", Count = 100 }, new { Name = "令牌", Count = 1000 } })
                for (int i = 0; i < material.Count; i++)
                    ItemStackRules.ConsumeOne(player.背包道具列表.获取道具分类列表(material.Name), material.Name, item => item.名字, item => item.数量, (item, count) => item.数量 = count);
        for (int p = 0; p < 全局变量.所有玩家数据表.Count; p++)
        {
            var states = ((JArray)world.Data["玩家列表"][p]["封地信息表"]).OfType<JObject>()
                .SelectMany(f => ((JArray)f["将领信息表"]).OfType<JObject>()).ToDictionary(g => g.Value<int>("ID"));
            foreach (var fief in 全局变量.所有玩家数据表[p].封地信息表)
                foreach (var general in fief.将领信息表) general.详细信息.状态 = states[general.ID]["详细信息"].Value<double>("状态");
        }
        foreach (var nation in 全局变量.所有国家列表)
        {
            var current = ((JArray)world.Data["国家列表"]).OfType<JObject>().Single(n => n.Value<int>("ID") == nation.ID);
            nation.成员列表.Clear(); nation.成员列表.AddRange(current["成员列表"].ToObject<List<int>>());
            nation.城池列表.Clear(); nation.城池列表.AddRange(current["城池列表"].ToObject<List<坐标>>());
        }
        if (founding) 全局变量.所有国家列表.Add(world.Data["国家列表"].Last.ToObject<国家信息库类>());
        全局变量.国家ID记录 = world.Data.Value<int>("国家ID记录");
        var registrations = 全局变量.所有城池列表.SelectMany(c => c.城池封地列表).ToDictionary(r => r.第几个玩家 + ":" + r.封地ID标识);
        foreach (var city in 全局变量.所有城池列表)
        {
            var current = TerritoryRules.City(world, city.坐标x, city.坐标y);
            city.城主 = current.Value<int>("城主"); city.国家 = current.Value<string>("国家"); city.规模 = current.Value<int>("规模");
            city.城池封地列表.Clear();
            foreach (var r in (JArray)current["城池封地列表"]) city.城池封地列表.Add(registrations[r.Value<int>("第几个玩家") + ":" + r.Value<int>("封地ID标识")]);
        }
    }

    private static void ApplyTroops<T>(List<T> target, JArray current, Func<T, int> id, Action<T, double> quantity)
    {
        var original = target.ToDictionary(id);
        target.Clear();
        foreach (var item in current)
        {
            T troop;
            if (original.TryGetValue(item.Value<int>("ID"), out troop)) quantity(troop, item.Value<double>("数量")); else troop = item.ToObject<T>();
            target.Add(troop);
        }
    }
}
