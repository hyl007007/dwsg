using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using 缺失界面.窗口4;

namespace Dwsg.Window3
{
    // 使用既有军事详情面板和地图字体，不创建新页面皮肤、贴图或旧页面逻辑副本。
    public static class 资源点界面适配
    {
        public static List<军事目标> 查询目标(string 搜索)
        {
            var 规则 = 资源点规则.本地;
            return 规则.查询(搜索).Select(x =>
            {
                var 检查 = 规则.检查目标(x.标识);
                return new 军事目标
                {
                标识 = x.标识, 名称 = x.类型.ToString(), 坐标x = x.坐标x, 坐标y = x.坐标y,
                说明 = 状态文字(x), 可提交 = 检查.Success, 限制 = 检查.Success ? "" : 检查.Message
                };
            }).ToList();
        }
        public static string 状态文字(资源点状态 点)
        {
            string 币 = 点.类型 == 资源点类型.铜矿 ? "铜钱" : "粮食";
            var 主 = 全局变量.所有玩家数据表 == null ? null : 全局变量.所有玩家数据表.Find(x => x != null && x.基础信息 != null && x.基础信息.ID == 点.占领玩家ID);
            string 属 = 点.占领玩家ID < 0 ? (点.剩余库存 > 0 ? "中立" : "采尽，恢复中") : "占领者：" + (主 != null ? 主.基础信息.名字 : "未知玩家");
            return 属 + " · " + 币 + " " + 点.时产 + "/小时 · 库存 " + 点.剩余库存 + "/" + 点.时产;
        }
        private static void 反馈(军事详情面板 页, CityResult 结果)
        {
            页.提示(结果.Success ? 军事结果.通过(结果.Message) : 军事结果.拒绝(军事错误.状态冲突, 结果.Message));
            if (结果.Success) 页.刷新();
        }
        public static void 构造详情(军事详情面板 页, string 点ID, Action<string> 选择将领,
            Action<资源点军情信息> 观战 = null, Func<long> 现在 = null)
        {
            if (页 == null) return;
            var 规则 = 资源点规则.本地; long 载入号 = 规则.载入号; int 角色 = 全局变量.本机身份;
            var 点 = 规则.查询().Find(x => x.标识 == 点ID);
            if (点 == null) { 页.说明("资源点已变更，请重新选择。", 40); return; }
            Func<long> 时钟 = 现在 ?? (() => TIME.getTime());
            页.状态.text = "每人最多占领两处 · 采尽后十分钟恢复";
            页.说明(点.类型 + "（" + 点.坐标x + "," + 点.坐标y + "）\n" + 状态文字(点), 62);
            页.说明("胜利后自动采集，部队归队；单轮库存为对应1级建筑的一小时基础产量。", 44, 14);
            var 表 = 全局变量.所有玩家数据表;
            var 主 = 表 != null && 全局变量.本机身份 >= 0 && 全局变量.本机身份 < 表.Count ? 表[全局变量.本机身份] : null;
            bool 我的 = 主 != null && 主.基础信息 != null && 点.占领玩家ID == 主.基础信息.ID;
            var 任务 = 规则.军情().Where(x => x.资源点标识 == 点ID).ToList();
            if (我的)
                页.操作行("放弃资源点", "结清已采资源；剩余库存保留，再次占领需要战斗。", "放弃", () =>
                {
                    if (规则.载入号 != 载入号 || 全局变量.本机身份 != 角色) { 反馈(页, CityResult.Fail("世界已切换，请重新打开资源点。")); return; }
                    反馈(页, 规则.放弃(点ID, 时钟()));
                });
            else if (点.占领玩家ID == -1 && 点.剩余库存 > 0 && 任务.Count == 0)
                页.操作行("占领资源点", "守军：1级山贼。击败守军后占领。", "选择将领", () =>
                {
                    if (规则.载入号 != 载入号 || 全局变量.本机身份 != 角色) { 反馈(页, CityResult.Fail("世界已切换，请重新打开资源点。")); return; }
                    if (选择将领 != null && 规则.战斗接入完成) 选择将领(点ID);
                }, 选择将领 != null && 规则.检查目标(点ID).Success);
            if (!规则.战斗接入完成) 页.说明("资源出征暂不可用，请稍后重试。", 38, 14);
            foreach (var 情 in 任务)
            {
                var 当前 = 情; bool 属我 = 主 != null && 主.基础信息 != null && 情.所属玩家ID == 主.基础信息.ID;
                页.操作行("资源部队 · " + 情.队列将领列表.Count + "将",
                    情.阶段 == 资源出征阶段.参战 ? "战斗中" : "到达倒计时 " + Math.Max(0, 情.到达时间 - 时钟()) + "秒",
                    情.阶段 == 资源出征阶段.参战 ? "观战" : "撤回", () =>
                    {
                        if (规则.载入号 != 载入号 || 全局变量.本机身份 != 角色) { 反馈(页, CityResult.Fail("世界已切换，请重新打开资源点。")); return; }
                        if (当前.阶段 == 资源出征阶段.参战) { if (观战 != null) 观战(当前); }
                        else 反馈(页, 规则.撤回(当前));
                    }, 属我 && (情.阶段 != 资源出征阶段.参战 || 观战 != null));
            }
        }
        // root 传入地图变换下的独立容器；不能传城池按 child 索引排列的容器。
        // 将容器置于同一地图变换下，可沿用原相机、拖动和缩放。返回的对象由调用者销毁/刷新。
        public static List<GameObject> 创建地图标记(Transform 独立容器, Text 原地图文字, Action<string> 打开详情)
        {
            var 列表 = new List<GameObject>();
            if (独立容器 == null || 原地图文字 == null || 原地图文字.font == null || 打开详情 == null ||
                独立容器.GetComponent<所有城池界面脚本>() != null) return 列表;
            var 规则 = 资源点规则.本地; long 载入号 = 规则.载入号;
            foreach (var 点 in 规则.查询())
            {
                var 框 = 军事界面样式.矩形("资源点." + 点.标识, 独立容器, new Vector2(.42f, .42f), Vector2.zero);
                框.localPosition = new Vector3(-16.8f + (点.坐标x - 1) * .7f, 7.2f - (点.坐标y - 1) * .7f, 0);
                var 图 = 框.gameObject.AddComponent<Image>(); 图.color = Color.clear; 图.raycastTarget = true;
                var 按 = 框.gameObject.AddComponent<Button>(); 按.targetGraphic = 图;
                var 字框 = 军事界面样式.矩形("标记文字", 框, new Vector2(40, 30), Vector2.zero);
                字框.localScale = new Vector3(.01f, .01f, 1);
                var 字 = 字框.gameObject.AddComponent<Text>(); 字.font = 原地图文字.font;
                字.fontSize = Mathf.Clamp(原地图文字.fontSize, 12, 20); 字.fontStyle = 原地图文字.fontStyle;
                字.alignment = TextAnchor.MiddleCenter; 字.horizontalOverflow = HorizontalWrapMode.Wrap;
                字.verticalOverflow = VerticalWrapMode.Truncate; 字.raycastTarget = false;
                字.color = 原地图文字.color; 字.text = 点.类型 == 资源点类型.铜矿 ? "铜" : "牧";
                string ID = 点.标识;
                按.onClick.AddListener(() => { if (规则.载入号 == 载入号) 打开详情(ID); });
                列表.Add(框.gameObject);
            }
            return 列表;
        }
    }
}
