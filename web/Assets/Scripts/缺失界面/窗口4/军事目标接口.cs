using System;
using System.Collections.Generic;

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
    }
    public interface I军事目标提供器
    {
        string 状态说明 { get; }
        List<军事目标> 查询(军事目标类型 类型, string 搜索);
        军事结果 提交(军事目标请求 请求);
    }

    // 世界目录只读。攻城转入现有将领选择页；该页负责提交既有本地军情。
    // 新提供器可替换此接口，但必须返回真实能力/错误，不能伪造网络成功。
    public sealed class 本地军事目标提供器 : I军事目标提供器
    {
        public string 状态说明 { get { return "本地世界 · 未连接多人服务器"; } }
        public List<军事目标> 查询(军事目标类型 类型, string 搜索)
        {
            var 结果 = new List<军事目标>();
            if (类型 == 军事目标类型.资源点) return 结果;
            foreach (var 城池 in 全局变量.所有城池列表)
            {
                if (城池 == null) continue;
                if (!string.IsNullOrEmpty(搜索) &&
                    (城池.名称 + " " + 城池.坐标x + "," + 城池.坐标y).IndexOf(搜索, StringComparison.OrdinalIgnoreCase) < 0) continue;
                结果.Add(new 军事目标
                {
                    标识 = 城池.坐标x + "," + 城池.坐标y,
                    名称 = 城池.名称,
                    坐标x = 城池.坐标x,
                    坐标y = 城池.坐标y,
                    说明 = 城池.获取规模名称() + " · 国家：" + 城池.获取国家名字() +
                        " · 城墙：" + 城池.城墙.ToString("0") + " · 驻防：" + 城池.城池玩家驻防列表.Count,
                    可提交 = 类型 == 军事目标类型.攻城,
                    限制 = 类型 == 军事目标类型.驻防 ? "当前驻防派遣/撤回结算尚未接入，未占用将领或添加军情。" : ""
                });
            }
            return 结果;
        }
        public 军事结果 提交(军事目标请求 请求)
        {
            return 军事结果.拒绝(军事错误.未接入, 请求 != null && 请求.类型 == 军事目标类型.资源点
                ? "当前世界没有资源点目录、占领或产出结算。请求未执行。"
                : "驻防派遣/撤回结算尚未接入。请求未执行。");
        }
    }
}
