using System;
using System.Linq;
using Dwsg.Administration;
using Dwsg.Network;
using Dwsg.Shared;
using Newtonsoft.Json.Linq;

namespace Dwsg.Window3
{
    public sealed partial class CityLocalAdapter
    {
        private object networkSnapshot;
        private long networkRevision = -1;
        private void ReadOnlineState()
        {
            var snapshot = GameNetwork.CurrentSnapshot;
            if (ReferenceEquals(networkSnapshot, snapshot) && networkRevision == (snapshot == null ? -1 : snapshot.WorldRevision)) return;
            networkSnapshot = snapshot; networkRevision = snapshot == null ? -1 : snapshot.WorldRevision;
            var projected = snapshot?.PrivatePlayer["administration"] as JObject ?? new JObject();
            state = DecodeState(projected, AdministrationClient.PlayerIndex, AdministrationClient.PlayerIndex);
        }
        private static CityModuleDto DecodeState(JObject source, Func<string, int> player, Func<string, int> owner)
        {
            var data = (JObject)source.DeepClone();
            foreach (string field in new[] { "Repairs", "Candidates", "Bookmarks" })
                foreach (JObject row in data[field] as JArray ?? new JArray())
                {
                    row["PlayerId"] = player(row.Value<string>("PlayerId"));
                    if (field == "Repairs") row["Owner"] = owner(row.Value<string>("Owner"));
                }
            return data.ToObject<CityModuleDto>();
        }
        private JObject EncodeState(WorldState world)
        {
            var data = JObject.FromObject(state);
            foreach (string field in new[] { "Repairs", "Candidates", "Bookmarks" })
                foreach (JObject row in (JArray)data[field])
                {
                    var original = PlayerId(row.Value<int>("PlayerId"));
                    int index = original == null ? -1 : 全局变量.所有玩家数据表.IndexOf(original);
                    row["PlayerId"] = AdministrationRules.StablePlayer(world, index);
                    if (field == "Repairs") row["Owner"] = AdministrationRules.StablePlayer(world, row.Value<int>("Owner"));
                }
            return data;
        }
        // Small rule inputs only. Never serialize general rosters, sprites or the entire world for a civic click.
        private WorldState RuleWorld()
        {
            var mapping = new JObject(); var players = new JArray();
            for (int i = 0; i < 全局变量.所有玩家数据表.Count; i++)
            {
                var original = 全局变量.所有玩家数据表[i]; mapping["local-" + i] = i;
                players.Add(new JObject { ["基础信息"] = JObject.FromObject(original.基础信息), ["财产信息"] = JObject.FromObject(original.财产信息),
                    ["封地信息表"] = new JArray(original.封地信息表.Select(f => new JObject { ["ID"] = f.ID, ["所在城池"] = f.所在城池 == null ? null : JObject.FromObject(f.所在城池) })) });
            }
            var cities = new JArray(全局变量.所有城池列表.Select(c => new JObject { ["坐标x"] = c.坐标x, ["坐标y"] = c.坐标y, ["规模"] = c.规模,
                ["国家"] = c.国家, ["城主"] = c.城主, ["正在交战"] = c.正在交战, ["城墙"] = c.城墙, ["道路"] = c.道路,
                ["城主征收_铜"] = c.城主征收_铜, ["城主征收_粮"] = c.城主征收_粮, ["国家征收_铜"] = c.国家征收_铜, ["国家征收_粮"] = c.国家征收_粮 }));
            var world = new WorldState { Data = new JObject { ["玩家列表"] = players, ["国家列表"] = JArray.FromObject(全局变量.所有国家列表), ["城池列表"] = cities }, EntityMappings = new JObject { ["players"] = mapping } };
            world.EntityMappings["administration"] = EncodeState(world);
            return world;
        }
        private void ApplyRules(WorldState world)
        {
            var players = (JArray)world.Data["玩家列表"];
            for (int i = 0; i < players.Count; i++)
            {
                var wallet = players[i]["财产信息"]; var original = 全局变量.所有玩家数据表[i].财产信息;
                original.铜钱 = wallet.Value<double>("铜钱"); original.粮食 = wallet.Value<double>("粮食");
            }
            var cityIndex = 全局变量.所有城池列表.ToDictionary(c => c.坐标x + ":" + c.坐标y);
            foreach (JObject city in (JArray)world.Data["城池列表"])
            {
                var original = cityIndex[city.Value<int>("坐标x") + ":" + city.Value<int>("坐标y")];
                original.城墙 = city.Value<double>("城墙"); original.道路 = city.Value<double>("道路"); original.城主 = city.Value<int>("城主");
            }
            foreach (JObject nation in (JArray)world.Data["国家列表"])
            {
                var original = 全局方法类.获取指定名字的国家(nation.Value<string>("国号"));
                original.铜钱 = nation.Value<double>("铜钱"); original.粮食 = nation.Value<double>("粮食"); original.上次轮选时间 = nation.Value<long>("上次轮选时间");
            }
            string worldKey = state.WorldKey; var receipts = state.RecentRequests;
            state = DecodeState((JObject)world.EntityMappings["administration"], id => { var p = Player(AdministrationRules.PlayerIndex(world, id)); return p == null ? -1 : p.基础信息.ID; }, id => AdministrationRules.PlayerIndex(world, id));
            state.WorldKey = worldKey; state.RecentRequests = receipts;
        }
        private CityResult Change(string request, Func<WorldState, string, GameResult> action)
        {
            EnsureWorld();
            if (GameNetwork.Enabled) return CityResult.Fail("联机操作须等待服务器确认。");
            string error = request == null ? null : CheckRequest(request);
            if (error != null) return CityResult.Fail(error);
            var world = RuleWorld(); var result = action(world, "local-" + 全局变量.本机身份);
            if (result.Code != GameCodes.Ok) return CityResult.Fail(result.Message);
            ApplyRules(world); if (request != null) Remember(request); CityRepairClock.Watch(this);
            return CityResult.Ok(result.Message);
        }
    }
}
