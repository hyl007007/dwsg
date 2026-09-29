using System;
using System.Collections.Generic;
using 玩家数据结构;

namespace 缺失界面.窗口4
{
    public sealed class 名将查询记录
    {
        public 将领属性库类 模板;
        public 将领信息 将领;
        public 玩家数据 玩家;
        public 封地信息 封地;
        public 城池信息库类 城池;
        public string 名字 { get { return 模板.名字; } }
        public string 归属 { get { return 玩家 != null && 玩家.基础信息 != null ? 玩家.基础信息.名字 : 城池 != null ? 城池.获取国家名字() : "归属未知"; } }
        public string 状态 { get { return 城池 != null && (玩家 == null || 归属 == "野名") ? "驻守" : 将领 != null && 将领.详细信息 != null ? 将领.详细信息.状态 == 和平驻防规则.驻防占用状态 ? "驻防占用" : 全局方法类.获取指定ID状态文本((int)将领.详细信息.状态) : "状态未知"; } }
        public string 位置
        {
            get
            {
                if (城池 != null) return 城池.名称 + "(" + 城池.坐标x + "," + 城池.坐标y + ")";
                if (封地 != null && 封地.所在城池 != null)
                    return 封地.封地名字 + "(" + 封地.所在城池.x + "," + 封地.所在城池.y + ")";
                return "位置未知";
            }
        }
    }

    public static class 画册查询规则
    {
        public static 名将查询记录 查询(string 名字)
        {
            名字 = (名字 ?? "").Trim();
            if (名字.Length == 0) return null;
            名将查询记录 记录 = null;
            foreach (var 玩家 in 全局变量.所有玩家数据表)
            {
                if (玩家 == null || 玩家.封地信息表 == null) continue;
                foreach (var 地 in 玩家.封地信息表)
                {
                    if (地 == null || 地.将领信息表 == null) continue;
                    foreach (var 将 in 地.将领信息表)
                    {
                        if (将 == null || 将.将领属性 == null || 将.将领属性.初始属性 == null || 将.将领属性.初始属性.名字 != 名字) continue;
                        var 模板 = 全局将领库.查询指定ID的将领数据(将.将领属性.初始属性.ID);
                        if (模板 == null || 模板.系列 != "名将" || 模板.名字 != 名字) continue;
                        记录 = new 名将查询记录 { 模板 = 模板, 将领 = 将, 玩家 = 玩家, 封地 = 地 };
                        if (玩家.基础信息 != null && 玩家.基础信息.名字 != "野名") return 记录;
                    }
                }
            }
            foreach (var 城 in 全局变量.所有城池列表)
            {
                if (城 == null || 城.城池驻防列表 == null) continue;
                foreach (var 索引 in 城.城池驻防列表)
                {
                    if (索引 == null) continue;
                    var 模板 = 全局将领库.查询指定ID的将领数据(索引.将领ID标识);
                    if (模板 == null || 模板.系列 != "名将" || 模板.名字 != 名字) continue;
                    if (记录 == null) 记录 = new 名将查询记录 { 模板 = 模板 };
                    记录.城池 = 城;
                    return 记录;
                }
            }
            return 记录;
        }

        public static 军事结果 购买(玩家数据 玩家, List<画册信息> 画册表, 名将查询记录 记录, bool 黄金)
        {
            if (记录 == null || 记录.模板 == null || 画册表 == null)
                return 军事结果.拒绝(军事错误.无将领, "请先查询名将。");
            var 当前 = 查询(记录.名字);
            if (当前 == null || 当前.模板.ID != 记录.模板.ID)
                return 军事结果.拒绝(军事错误.无将领, "名将已变更，请重新查询。");
            foreach (var 册 in 画册表)
                if (册 != null && (册.将领id == (int)记录.模板.ID || 册.名字 == 记录.名字) && 册.到期时间 > 0)
                    return 军事结果.拒绝(军事错误.状态冲突, "已拥有该名将画册，无需重复购买。");
            int 数量 = 全局方法类.根据突围获取材料数量((int)当前.模板.突围);
            var 检查 = 将领流程规则.支付材料(玩家, 黄金 ? new Dictionary<string, int>() : new Dictionary<string, int> { { "勇士令", 数量 } }, 黄金 ? 数量 * 10 : 0);
            if (!检查.成功) return 检查;
            画册表.RemoveAll(x => x != null && (x.将领id == (int)记录.模板.ID || x.名字 == 记录.名字));
            // 现有任务每秒扣减此字段，它保存剩余秒数，不能写 Unix 到期时间。
            画册表.Add(new 画册信息(86400, 记录.名字, (int)记录.模板.ID));
            return 军事结果.通过(黄金 ? "黄金购买成功，画册有效期24小时。" : "勇士令交换成功，画册有效期24小时。");
        }
    }
}
