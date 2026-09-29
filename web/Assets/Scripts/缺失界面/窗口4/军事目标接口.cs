using System;
using System.Collections.Generic;
using Dwsg.Window3;

namespace 缺失界面.窗口4
{
    public enum 军事目标类型 { 攻城, 资源点, 驻防 }
    public sealed class 军事目标
    {
        public string 标识;
        public string 名称;
        public int 坐标x;
        public int 坐标y;
        public string 说明;
        public bool 可提交;
        public string 限制;
    }
    public sealed class 军事目标请求
    {
        public 军事目标类型 类型;
        public string 目标标识;
        public int 封地ID;
        public List<int> 将领ID = new List<int>();
        public long 资源载入号;
    }
    public interface I军事目标提供器
    {
        string 状态说明 { get; }
        List<军事目标> 查询(军事目标类型 类型, string 搜索);
        军事结果 提交(军事目标请求 请求);
    }

    // 攻城和驻防均转入原将领选择页；派遣在最终确认时校验实际引用。
    // 新提供器可替换此接口，但必须返回真实能力/错误，不能伪造网络成功。
    public sealed class 本地军事目标提供器 : I军事目标提供器
    {
        public string 状态说明 { get { return "选择城池查看详情"; } }
        public List<军事目标> 查询(军事目标类型 类型, string 搜索)
        {
            var 结果 = new List<军事目标>();
            if (类型 == 军事目标类型.资源点) return 资源点界面适配.查询目标(搜索);
            if (全局变量.所有城池列表 == null) return 结果;
            foreach (var 城池 in 全局变量.所有城池列表)
            {
                if (城池 == null) continue;
                if (!string.IsNullOrEmpty(搜索) &&
                    (城池.名称 + " " + 城池.坐标x + "," + 城池.坐标y).IndexOf(搜索, StringComparison.OrdinalIgnoreCase) < 0) continue;
                var 检查 = 类型 == 军事目标类型.驻防 ? 和平驻防规则.检查目标(军事缺口入口.当前玩家(), 城池) : 军事结果.通过("");
                结果.Add(new 军事目标
                {
                    标识 = 城池.坐标x + "," + 城池.坐标y,
                    名称 = 城池.名称,
                    坐标x = 城池.坐标x,
                    坐标y = 城池.坐标y,
                    说明 = 城池.获取规模名称() + " · 国家：" + 城池.获取国家名字() +
                        " · 城墙：" + 城池.城墙.ToString("0") + " · 玩家驻防兵力：" + 和平驻防规则.已占兵力(城池).ToString("0") + "/" + 城池.获取驻防上限().ToString("0"),
                    可提交 = 检查.成功,
                    限制 = 检查.成功 ? "" : 检查.说明
                });
            }
            return 结果;
        }
        public 军事结果 提交(军事目标请求 请求)
        {
            if (请求 == null) return 军事结果.拒绝(军事错误.数量无效, "请选择军事目标。");
            if (请求.类型 != 军事目标类型.驻防 && 请求.类型 != 军事目标类型.资源点)
                return 军事结果.拒绝(军事错误.未接入, "请通过原将领选择页确认出征。");
            var 玩家 = 军事缺口入口.当前玩家();
            if (玩家 == null) return 军事结果.拒绝(军事错误.无角色, "角色尚未载入。");
            var 地 = 玩家.封地信息表.Find(x => x != null && x.ID == 请求.封地ID);
            var 目标 = 查询(请求.类型, "").Find(x => x.标识 == 请求.目标标识);
            var 城 = 目标 != null ? 全局变量.所有城池列表.Find(x => x != null && x.坐标x == 目标.坐标x && x.坐标y == 目标.坐标y) : null;
            var 将领表 = new List<玩家数据结构.将领信息>();
            if (地 != null && 请求.将领ID != null)
                foreach (int ID in 请求.将领ID) 将领表.Add(地.将领信息表.Find(x => x != null && x.ID == ID));
            if (请求.类型 == 军事目标类型.资源点)
            {
                资源点军情信息 情;
                var 结果 = 资源点规则.本地.派遣(请求.目标标识, 请求.封地ID, 将领表, TIME.getTime(), 请求.资源载入号, out 情);
                return 结果.Success ? 军事结果.通过(结果.Message) : 军事结果.拒绝(军事错误.状态冲突, 结果.Message);
            }
            return 和平驻防规则.派遣(玩家, 地, 城, 将领表, TIME.getTime());
        }
    }
}
