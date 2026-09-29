using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using 玩家数据结构;

namespace Dwsg.Window3
{
    // 只提供既有战场的输入与战果出口；不实现伤害、寻敌、渲染或战斗循环。
    public sealed class 资源点战斗适配 : MonoBehaviour
    {
        private 资源点规则 规则;
        private 资源点军情信息 军情;
        private 战斗系统 战场;
        private bool 已结算;

        // 借用现有一级山贼生成结果，深复制；不调用会污染将领库/NPC 封地的随机生成入口。
        public static CityResult 复制一级守军(out List<将领信息> 守军)
        {
            守军 = null;
            var 山贼 = 全局变量.所有山贼数据列表 == null ? null : 全局变量.所有山贼数据列表
                .Where(x => x != null && x.等级 == 1 && x.将领数据列表 != null && x.将领数据列表.Count == 1 &&
                    x.将领数据列表.All(g => g != null && g.将领属性 != null && g.将领属性.初始属性 != null &&
                        g.将领属性.最终属性 != null && g.将领属性.成长点数 != null && g.详细信息 != null && g.将领配兵 != null &&
                        g.详细信息.状态 == 0 && g.将领属性.成长点数.等级 >= 1 && g.将领属性.成长点数.等级 <= 3 &&
                        g.将领属性.初始属性.ID >= 1 && g.将领属性.初始属性.ID <= 4 && g.将领配兵.ID == 201 &&
                        g.将领配兵.数量 >= 140 && g.将领配兵.数量 < 400 && g.将领配兵.数量 == Math.Floor(g.将领配兵.数量)))
                .OrderBy(x => x.坐标y).ThenBy(x => x.坐标x).FirstOrDefault();
            if (山贼 == null) return CityResult.Fail("现有一级山贼守军尚未生成，请稍后重试。");
            try
            {
                守军 = JsonConvert.DeserializeObject<List<将领信息>>(JsonConvert.SerializeObject(山贼.将领数据列表));
                foreach (var 将 in 守军)
                {
                    将.ID = 0; 将.详细信息.身份 = 1; 将.详细信息.状态 = 1; 将.详细信息.坑位颜色 = 1;
                    将.详细信息.剩余兵力 = 将.将领配兵.数量;
                }
                return CityResult.Ok("守军复用现有一级山贼配置。");
            }
            catch (JsonException) { 守军 = null; return CityResult.Fail("一级山贼守军无法复制。"); }
        }
        // root 在到达 hook 调用。调用者随后把同一军情登记到战场的实际参战集合。
        public static CityResult 尝试开始(资源点规则 规则, 资源点军情信息 情, Transform 战场父级, long 现在, out 战斗系统 新战场)
        {
            新战场 = null;
            if (规则 == null || !规则.战斗接入完成 || 战场父级 == null || 全局变量.山贼战斗场景pre == null)
                return CityResult.Fail("资源战斗场景尚未接入。");
            var 检查 = 规则.校验军情(情); if (!检查.Success) return 检查;
            var 点 = 规则.查询().Find(x => x.标识 == 情.资源点标识);
            if (情.阶段 != 资源出征阶段.前往 || 情.已进入战场 || 现在 < 情.到达时间 ||
                全局变量.军情列表 == null || !全局变量.军情列表.Contains(情) || 点 == null || 点.占领玩家ID != -1 || 点.剩余库存 <= 0 ||
                规则.军情().Any(x => !ReferenceEquals(x, 情) && x.资源点标识 == 情.资源点标识))
                return CityResult.Fail("资源部队尚未到达，或目标已变化。");
            List<将领信息> 守军; var 准备 = 复制一级守军(out 守军); if (!准备.Success) return 准备;
            if (全局变量.所有玩家数据表 == null || 全局变量.所有玩家数据表.Count <= 1 ||
                全局变量.所有玩家数据表[1] == null || 全局变量.所有玩家数据表[1].基础信息 == null ||
                全局变量.所有玩家数据表[1].基础信息.ID != 1 || 全局变量.所有玩家数据表[1].科技信息 == null)
                return CityResult.Fail("现有山贼阵营尚未建立。");
            var 主 = 全局变量.所有玩家数据表.FindIndex(x => x != null && x.基础信息 != null && x.基础信息.ID == 情.所属玩家ID);
            if (主 < 0 || 主 != 情.所属玩家ID) return CityResult.Fail("资源部队阵营与现有战斗索引不符。");
            GameObject 根 = null;
            try
            {
                根 = UnityEngine.Object.Instantiate(全局变量.山贼战斗场景pre);
                根.SetActive(false); 根.transform.SetParent(战场父级, false);
                var 场 = 根.GetComponentInChildren<战斗系统>(true);
                if (场 == null) { UnityEngine.Object.Destroy(根); return CityResult.Fail("既有山贼场景缺少战斗系统。"); }
                场.战场类型 = 资源点军情信息.资源战场类型; 场.坐标x = 点.坐标x; 场.坐标y = 点.坐标y;
                场.创建时间 = 现在; 场.攻身份 = 主; 场.守身份 = 1;
                场.攻方要渲染的编队将领列表.Clear(); 场.守方要渲染的编队将领列表.Clear();
                场.攻方要渲染的编队将领列表.Add(new List<将领信息>(情.队列将领列表));
                场.守方要渲染的编队将领列表.Add(守军);
                var 接 = 场.gameObject.AddComponent<资源点战斗适配>(); 接.规则 = 规则; 接.军情 = 情; 接.战场 = 场;
                foreach (var 将 in 情.队列将领列表) 将.详细信息.坑位颜色 = 0;
                情.阶段 = 资源出征阶段.参战; 情.已进入战场 = true;
                根.SetActive(true); 新战场 = 场;
                return CityResult.Ok("资源部队已进入真实战场。");
            }
            catch (Exception e) when (e is ArgumentException || e is InvalidOperationException)
            {
                if (根 != null) UnityEngine.Object.Destroy(根);
                情.阶段 = 资源出征阶段.前往; 情.已进入战场 = false;
                return CityResult.Fail("资源战斗场景未能建立。");
            }
        }
        // root 的胜负分支设置战斗结束后、清理参战集合前调用；不能从 UI 宣告胜利。
        public static CityResult 处理结束(战斗系统 场, bool 攻方获胜, long 现在)
        {
            var 接 = 场 != null ? 场.GetComponent<资源点战斗适配>() : null;
            if (接 == null || 接.已结算 || !ReferenceEquals(接.战场, 场) || 接.规则 == null || 接.军情 == null ||
                !场.战斗结束 || 场.战场类型 != 资源点军情信息.资源战场类型 ||
                !FiefActions.Finite(场.攻方兵力) || !FiefActions.Finite(场.守方兵力) ||
                (攻方获胜 ? 场.全军撤退 || 场.守方兵力 > 0 || 场.攻方兵力 <= 0 : !场.全军撤退 && 场.攻方兵力 > 0))
                return CityResult.Fail("资源战场尚未产生有效战果。");
            var 结果 = 接.规则.结算战果(接.军情, 攻方获胜, 现在);
            if (结果.Success) 接.已结算 = true;
            return 结果;
        }
        public static bool 是资源军情(军情信息 情)
        { return 情 is 资源点军情信息 || (情 != null && 情.战场类型 == 资源点军情信息.资源战场类型); }
        public static bool 匹配军情(战斗系统 场, 资源点军情信息 情)
        {
            var 接 = 场 != null ? 场.GetComponent<资源点战斗适配>() : null;
            return 接 != null && ReferenceEquals(接.战场, 场) && ReferenceEquals(接.军情, 情) && !场.战斗结束;
        }
    }
}
