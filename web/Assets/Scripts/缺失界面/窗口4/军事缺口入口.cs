using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using 玩家数据结构;
using Dwsg.Window3;

namespace 缺失界面.窗口4
{
    public sealed class 军事缺口入口 : MonoBehaviour
    {
        public static I军事目标提供器 目标提供器 = new 本地军事目标提供器();
        private readonly Dictionary<string, Transform> 根表 = new Dictionary<string, Transform>();
        private readonly HashSet<Button> 已绑定 = new HashSet<Button>();
        private readonly List<KeyValuePair<Button, UnityAction>> 监听 = new List<KeyValuePair<Button, UnityAction>>();
        private 将领列表显示 将领页;
        private 封地信息界面UI脚本 封地信息页;
        private 封地界面脚本 封地页;
        private 战斗界面UI脚本 战斗页;
        private 军事界面样式 样式;
        private 军事详情面板 动态页;
        private 军事详情面板 操作页;
        private 军事详情面板 选择页;
        private 军事详情面板 目标页;
        private 军事详情面板 战略页;
        private 军事详情面板 战斗选择页;
        private string 搜索词 = "";
        private string 选中目标;
        private Transform 资源标记容器;
        private readonly List<GameObject> 资源标记 = new List<GameObject>();
        private string 资源标记签名;
        private long 上次资源标记刷新 = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void 安装()
        {
            SceneManager.sceneLoaded -= 场景载入;
            SceneManager.sceneLoaded += 场景载入;
            场景载入(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }
        private static void 场景载入(Scene 场景, LoadSceneMode 模式)
        {
            bool 有主界面 = false;
            foreach (var 根 in 场景.GetRootGameObjects())
            {
                if (根.GetComponent<军事缺口入口>() != null) return;
                if (根.GetComponent<主界面UI脚本>() != null) 有主界面 = true;
            }
            if (!有主界面) return;
            var 对象 = new GameObject("窗口4军事界面入口");
            SceneManager.MoveGameObjectToScene(对象, 场景);
            对象.AddComponent<军事缺口入口>();
        }
        private IEnumerator Start()
        {
            // 等共享窗口管理器及主场景 Start 完成；初始化只扫描一次。
            yield return null;
            foreach (var 根 in gameObject.scene.GetRootGameObjects()) 根表[根.name] = 根.transform;
            Transform 君主 = 根("君主信息界面UI");
            样式 = new 军事界面样式(君主);
            将领页 = 获取<将领列表显示>();
            封地信息页 = 获取<封地信息界面UI脚本>();
            封地页 = 获取<封地界面脚本>();
            战斗页 = 获取<战斗界面UI脚本>();
            var 画册根 = 根("画册界面UI");
            var 画册 = 画册根 != null ? 画册根.GetComponent<画册脚本>() : null;
            if (画册 != null)
            {
                画册.初始化原窗口();
                foreach (var 场景根 in 根表.Values)
                    foreach (var 按 in 场景根.GetComponentsInChildren<Button>(true))
                    {
                        if (按.transform.IsChildOf(画册.transform)) continue;
                        for (int i = 0; i < 按.onClick.GetPersistentEventCount(); i++)
                            if (按.onClick.GetPersistentTarget(i) == 画册.gameObject &&
                                按.onClick.GetPersistentMethodName(i) == "SetActive")
                            {
                                // 保留拜访名将及其他原入口事件，随后恢复原查询页。
                                接入(按, 画册.打开查询);
                                break;
                            }
                    }
            }
            动态页 = 军事详情面板.创建("君主军事动态详情", 样式);
            操作页 = 军事详情面板.创建("将领修炼恢复配兵", 样式);
            选择页 = 军事详情面板.创建("军事切换封地", 样式);
            目标页 = 军事详情面板.创建("出征军事目标选择", 样式);
            if (战斗页 != null)
            {
                战略页 = 军事详情面板.创建("战斗战略详情", 样式, true, 战斗页.transform);
                战斗选择页 = 军事详情面板.创建("战斗增援切换封地", 样式, true, 战斗页.transform);
            }

            绑定(君主, "动态布局/将领信息/查看按钮", 查看将领);
            绑定(君主, "动态布局/俘虏信息/查看按钮", () => 查看封地兵员("俘虏"));
            绑定(君主, "动态布局/兵力信息/查看按钮", () => 查看封地兵员("闲兵"));
            绑定(君主, "动态布局/伤兵信息/查看按钮", () => 查看封地兵员("伤兵"));
            foreach (string 队列 in new[] { "建筑", "征兵", "研究" })
            {
                string 类别 = 队列;
                绑定(君主, "动态布局/" + 队列 + "队列信息/查看按钮", () => 查看队列(类别));
            }
            if (将领页 != null)
            {
                绑定(将领页.transform, "将领属性布局/将领属性操作布局/修炼", () => 打开将领操作("修炼"));
                绑定(将领页.transform, "将领属性布局/所属经验体力俸禄布局/体力加号", () => 打开将领操作("恢复体力"));
                绑定(将领页.transform, "将领配兵布局/配兵当前部队布局/当前部队已配兵布局/调整", () => 打开将领操作("配兵调整"));
            }
            var 出征 = 根("出征界面UI (1)");
            绑定(出征, "选择出征目标布局/攻占城池", () => 查看目标(军事目标类型.攻城));
            绑定(出征, "选择出征目标布局/占领资源点", () => 查看目标(军事目标类型.资源点));
            绑定(出征, "选择出征目标布局/驻防", () => 查看目标(军事目标类型.驻防));
            绑定(出征, "出征界面操作/战场说明", 战场说明);
            绑定(出征, "封地操作/切换封地", () => 切换封地(null));
            foreach (var 脚本 in 获取全部<选择出征将领>())
            {
                var 当前 = 脚本;
                绑定(脚本.transform, "切换封地", () => 切换封地(当前));
                绑定(脚本.transform, "剿灭山贼界面操作/切换", () => 切换封地(当前));
            }
            var 编队 = 获取<将领编队>();
            if (编队 != null)
                foreach (var 按 in 编队.GetComponentsInChildren<Button>(true))
                    if (按.name == "切换封地" && 按.onClick.GetPersistentEventCount() == 0)
                        接入(按, () => 切换封地(null, 编队));
            if (战斗页 != null)
            {
                绑定(战斗页.transform, "战略", 查看战略);
                绑定(战斗页.transform, "增援", () =>
                {
                    if (战略页 != null) 战略页.gameObject.SetActive(false);
                    if (战斗选择页 != null) 战斗选择页.gameObject.SetActive(false);
                });
            }
            更新出征封地显示();
        }

        private void 更新出征封地显示()
        {
            var 出征根 = 根("出征界面UI (1)");
            var 封地操作 = 出征根 != null ? 出征根.Find("封地操作") as RectTransform : null;
            var 名称对象 = 封地操作 != null ? 封地操作.Find("出征封地显示") : null;
            var 字 = 名称对象 != null ? 名称对象.GetComponent<Text>() : null;
            if (字 == null) return;
            var 标题 = 封地操作.Find("出征封地标题") as RectTransform;
            var 区域 = 字.rectTransform;
            var 左边界 = RectTransformUtility.CalculateRelativeRectTransformBounds(封地操作, 标题 != null ? 标题 : 区域);
            float 间距 = 字.fontSize * .5f;
            float 左留边 = (标题 != null ? 左边界.max.x + 间距 : 左边界.min.x) - 封地操作.rect.xMin;
            var 下边 = 区域.offsetMin;
            var 上边 = 区域.offsetMax;
            区域.anchorMin = new Vector2(0, 区域.anchorMin.y);
            区域.anchorMax = new Vector2(1, 区域.anchorMax.y);
            区域.offsetMin = new Vector2(左留边, 下边.y);
            区域.offsetMax = new Vector2(-间距, 上边.y);
            字.horizontalOverflow = HorizontalWrapMode.Overflow;
            // 原行框略小于该字体的行距，按字形显示，避免首行被整体截掉。
            字.verticalOverflow = VerticalWrapMode.Overflow;
            字.text = 当前封地名字();
            军事界面样式.限定名称(字);
        }

        private Transform 根(string 名) { Transform 值; return 根表.TryGetValue(名, out 值) ? 值 : null; }
        private T 获取<T>() where T : Component { return 获取全部<T>().FirstOrDefault(); }
        private List<T> 获取全部<T>() where T : Component
        {
            var 表 = new List<T>();
            foreach (var 根 in 根表.Values) if (根 != null) 表.AddRange(根.GetComponentsInChildren<T>(true));
            return 表;
        }
        private void 绑定(Transform 父, string 路径, Action 动作)
        {
            var 物体 = 父 != null ? 父.Find(路径) : null;
            if (物体 != null) 接入(物体.GetComponent<Button>(), 动作);
        }
        private void 接入(Button 按, Action 动作)
        {
            if (按 == null || !已绑定.Add(按)) return;
            foreach (var 字 in 按.GetComponentsInChildren<Text>(true))
                if (字.GetComponentInParent<Selectable>(true) == 按) 原界面文字样式.居中按钮文字(字);
            UnityAction 回调 = () => 动作();
            按.onClick.AddListener(回调);
            监听.Add(new KeyValuePair<Button, UnityAction>(按, 回调));
            界面窗口管理器.注册运行时按钮(按);
        }
        private void OnDestroy()
        {
            foreach (var 项 in 监听) if (项.Key != null) 项.Key.onClick.RemoveListener(项.Value);
            if (资源标记容器 != null) Destroy(资源标记容器.gameObject);
        }

        private void LateUpdate()
        {
            long 现在 = TIME.getTime();
            if (目标页 == null || 上次资源标记刷新 == 现在) return;
            上次资源标记刷新 = 现在;
            var 规则 = 资源点规则.本地;
            var 点位 = 规则.查询();
            string 签名 = 规则.载入号 + ":" + 全局变量.本机身份 + ":" + string.Join("|", 点位.Select(x => x.标识));
            if (签名 == 资源标记签名 && 资源标记容器 != null) return;
            var 城池布局 = 获取<所有城池界面脚本>();
            var 字 = 城池布局 != null ? 城池布局.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.font != null) : null;
            if (城池布局 == null || 城池布局.transform.parent == null || 字 == null) return;
            if (资源标记容器 == null)
            {
                var 容器 = new GameObject("资源点独立地图标记", typeof(RectTransform)).GetComponent<RectTransform>();
                容器.SetParent(城池布局.transform.parent, false);
                var 原框 = 城池布局.transform as RectTransform;
                if (原框 != null)
                {
                    容器.anchorMin = 原框.anchorMin; 容器.anchorMax = 原框.anchorMax;
                    容器.pivot = 原框.pivot; 容器.sizeDelta = 原框.sizeDelta;
                }
                容器.localPosition = 城池布局.transform.localPosition;
                容器.localRotation = 城池布局.transform.localRotation;
                容器.localScale = 城池布局.transform.localScale;
                容器.SetSiblingIndex(城池布局.transform.GetSiblingIndex() + 1);
                资源标记容器 = 容器;
            }
            foreach (var 标记 in 资源标记) if (标记 != null) { 标记.SetActive(false); Destroy(标记); }
            资源标记.Clear();
            资源标记.AddRange(资源点界面适配.创建地图标记(资源标记容器, 字, 打开资源详情));
            foreach (var 标记 in 资源标记) 界面窗口管理器.注册运行时按钮(标记.GetComponent<Button>());
            资源标记签名 = 签名;
        }

        private void 打开资源详情(string ID)
        {
            var 点 = 资源点规则.本地.查询().Find(x => x.标识 == ID);
            if (点 == null) return;
            选中目标 = ID;
            操作页.打开("军事目标 · " + 点.类型, 页 => 构造目标详情(页, 军事目标类型.资源点));
        }
        public static 玩家数据 当前玩家()
        {
            int 号 = 全局变量.本机身份;
            return 全局变量.所有玩家数据表 != null && 号 >= 0 && 号 < 全局变量.所有玩家数据表.Count
                ? 全局变量.所有玩家数据表[号] : null;
        }
        private string 当前封地名字()
        {
            var 玩家 = 当前玩家();
            int 号 = 全局变量.第几个封地;
            return 玩家 != null && 号 >= 0 && 号 < 玩家.封地信息表.Count ? 玩家.封地信息表[号].封地名字 : "未拥有封地";
        }
        private bool 有角色(军事详情面板 页, out 玩家数据 玩家)
        {
            玩家 = 当前玩家();
            if (玩家 != null && 玩家.封地信息表 != null) return true;
            页.说明("本地角色尚未载入。请先进入角色，再查看军事动态。");
            return false;
        }
        private static string 状态名(将领信息 将)
        {
            return 将.详细信息.状态 == 0 ? "空闲" : 将.详细信息.状态 == 1 ? "出征/战斗" : 将.详细信息.状态 == 2 ? "驻防占用" : 将.详细信息.状态 == 3 ? "被俘" : "其他占用状态";
        }
        private static string 兵名(double ID)
        {
            var 兵 = 全局兵种库.查询指定ID的数据(ID);
            return 兵 != null ? 兵.名称 : "未配兵";
        }
        private void 查看将领()
        {
            if (将领页 == null) return;
            将领页.默认显示全部封地将领();
            将领页.gameObject.SetActive(true);
            将领页.重置刷新将领列表();
        }
        private void 查看封地兵员(string 类型)
        {
            动态页.打开("君主动态 · " + (类型 == "闲兵" ? "兵力" : 类型), 页 =>
            {
                玩家数据 玩家;
                if (!有角色(页, out 玩家)) return;
                页.状态.text = "封地 " + 玩家.封地信息表.Count + "处";
                if (玩家.封地信息表.Count == 0) 页.说明("暂无封地，请先建立封地。");
                foreach (var 封地 in 玩家.封地信息表)
                {
                    var 当前 = 封地;
                    double 闲兵 = 封地.闲兵信息表.Sum(x => Math.Max(0, x.数量));
                    double 伤兵 = 封地.伤兵信息表.Sum(x => Math.Max(0, x.数量));
                    double 配兵 = 封地.将领信息表.Sum(x => Math.Max(0, x.将领配兵.数量));
                    string 文案 = 类型 == "俘虏" ? "俘虏 " + 封地.俘虏信息表.Count + "名" : 类型 == "伤兵" ? "伤兵 " + 伤兵.ToString("0") + "名" : "闲兵 " + 闲兵.ToString("0") + " · 已配兵 " + 配兵.ToString("0");
                    页.数据行(封地.封地名字, 文案, "查看", () => 打开封地兵员(当前, 类型), 封地信息页 != null);
                }
            });
        }
        private void 打开封地兵员(封地信息 封地, string 类型)
        {
            var 玩家 = 当前玩家();
            int 索引 = 玩家 != null ? 玩家.封地信息表.IndexOf(封地) : -1;
            if (索引 < 0 || 封地信息页 == null) { 动态页.提示(军事结果.拒绝(军事错误.无封地, "封地已变更。")); return; }
            全局变量.第几个封地 = 索引;
            封地信息页.gameObject.SetActive(true);
            封地信息页.打开兵员分类(类型);
        }

        private void 查看队列(string 类型)
        {
            动态页.打开("君主动态 · " + 类型 + "队列", 页 =>
            {
                玩家数据 玩家;
                if (!有角色(页, out 玩家)) return;
                页.状态.text = "当前没有" + 类型 + "队列";
                if (玩家.封地信息表.Count == 0) { 页.说明("暂无封地，请先建立封地。"); return; }
                foreach (var 封地 in 玩家.封地信息表)
                {
                    var 当前 = 封地;
                    string 数据 = 类型 == "建筑" ? "建筑 " + 封地.建筑信息表.Count(x => x != null && x.类型 >= 0) + "座"
                        : 类型 == "征兵" ? "闲兵 " + 封地.闲兵信息表.Sum(x => Math.Max(0, x.数量)).ToString("0") + "名" : "查看科技等级";
                    页.数据行(封地.封地名字, 数据, "前往封地", () => 前往封地(当前), 封地页 != null);
                }
            });
        }
        private void 前往封地(封地信息 封地)
        {
            var 玩家 = 当前玩家();
            int 号 = 玩家 != null ? 玩家.封地信息表.IndexOf(封地) : -1;
            if (号 < 0 || 封地页 == null) return;
            全局变量.第几个封地 = 号;
            封地页.第几个封地 = 号;
            界面窗口管理器.关闭当前场景窗口();
            var 主界面 = 获取<主界面UI脚本>();
            if (主界面 != null) 主界面.加载封地场景();
            封地页.显示封地所有建筑();
        }

        private void 打开将领操作(string 操作)
        {
            封地信息 封地;
            将领信息 将;
            if (将领页 == null || !将领页.尝试获取选中将领(out 封地, out 将))
            {
                操作页.打开(操作, 页 => 页.说明("尚未选择将领。返回将领页，先选中一名将领。"));
                return;
            }
            var 目标将 = 将;
            var 目标封地 = 封地;
            操作页.打开(将.将领属性.初始属性.名字 + " · " + 操作, 页 => 构造将领操作(页, 目标封地, 目标将, 操作));
        }
        private void 构造将领操作(军事详情面板 页, 封地信息 封地, 将领信息 将, string 操作)
        {
            var 玩家 = 当前玩家();
            var 检查 = 军事本地规则.检查将领(玩家, 封地, 将);
            if (!检查.成功) { 页.说明(检查.说明, 90); return; }
            页.状态.text = 封地.封地名字 + " · " + 状态名(将);
            页.说明("等级 " + 将.将领属性.成长点数.等级 + " · 经验 " + 将.详细信息.经验.ToString("0") + "/" + 将.获取当前等级升级需要经验(将.将领属性.成长点数.等级).ToString("0") +
                "\n体力 " + 将.详细信息.剩余体力.ToString("0") + "/" + 将.将领属性.最终属性.体力上限.ToString("0") + " · 铜钱 " + 玩家.财产信息.铜钱.ToString("0"), 56);
            if (操作 == "修炼")
            {
                页.说明("修炼需10体力和等级×100铜钱，获得本级升级经验的10%。最高99级。", 48);
                页.操作行("本次修炼", "经验+" + 军事本地规则.修炼经验(将) + " · 铜钱-" + 军事本地规则.修炼费用(将) + " · 体力-10", "确认修炼",
                    () => 完成操作(页, 军事本地规则.修炼(当前玩家(), 封地, 将)), 将.将领属性.成长点数.等级 < 99);
            }
            else if (操作 == "恢复体力")
            {
                double 丹数 = 玩家.背包道具列表.获取指定道具数量("活血丹");
                页.说明("活血丹每个恢复50体力。背包：" + 丹数.ToString("0") + "个", 40);
                页.操作行("活血丹", "恢复量受体力上限限制；确认时重新检查库存和将领状态。", "使用1个",
                    () => 完成操作(页, 军事本地规则.恢复体力(当前玩家(), 封地, 将)), 丹数 >= 1 && 将.详细信息.剩余体力 < 将.将领属性.最终属性.体力上限,
                    Resources.Load<Sprite>("道具头像/活血丹"));
            }
            else
            {
                int 兵种 = (int)将.将领配兵.ID;
                页.说明("当前 " + 兵名(兵种) + " " + 将.将领配兵.数量.ToString("0") + "名 · 统兵上限 " + 将.将领属性.最终属性.统兵.ToString("0") +
                    "\n输入部队的目标总数；减少时归还本封地闲兵，填0解除配兵。", 62);
                var 输入 = 页.输入("目标总兵力", 将.将领配兵.数量.ToString("0"), true);
                页.操作行("调整当前部队", "可用闲兵 " + 军事本地规则.闲兵数量(封地, 兵种).ToString("0") + "名", "确认调整", () =>
                {
                    int 数量;
                    if (!int.TryParse(输入.text, out 数量)) { 页.提示(军事结果.拒绝(军事错误.数量无效, "请输入非负整数。")); return; }
                    完成操作(页, 军事本地规则.配兵(当前玩家(), 封地, 将, 兵种, 数量));
                });
            }
        }
        private void 完成操作(军事详情面板 页, 军事结果 结果)
        {
            if (结果.成功)
            {
                军事本地规则.计算属性保留体力(当前玩家());
                页.刷新();
            }
            页.提示(结果);
        }

        private void 切换封地(选择出征将领 出征脚本, 将领编队 编队 = null)
        {
            var 当前选择页 = 出征脚本 != null && 战斗页 != null && 出征脚本.transform.IsChildOf(战斗页.transform) ? 战斗选择页 : 选择页;
            if (当前选择页 == 战斗选择页 && 战略页 != null) 战略页.gameObject.SetActive(false);
            当前选择页.打开("切换封地", 页 =>
            {
                玩家数据 玩家;
                if (!有角色(页, out 玩家)) return;
                页.状态.text = "当前封地：" + 当前封地名字() + " · 切换会清空本页出征选择";
                if (玩家.封地信息表.Count == 0) 页.说明("尚未拥有封地。请先建立封地，才能选择出征来源。");
                foreach (var 封地 in 玩家.封地信息表)
                {
                    var 当前 = 封地;
                    页.操作行(封地.封地名字, "将领 " + 封地.将领信息表.Count + "名 · 闲兵 " + 封地.闲兵信息表.Sum(x => x.数量).ToString("0") + "名", "选择", () =>
                    {
                        var 角色 = 当前玩家();
                        int 号 = 角色 != null ? 角色.封地信息表.IndexOf(当前) : -1;
                        if (号 < 0) { 页.提示(军事结果.拒绝(军事错误.无封地, "封地已变更，请刷新列表。")); return; }
                        全局变量.第几个封地 = 号;
                        更新出征封地显示();
                        if (出征脚本 != null) 出征脚本.切换出征封地(号);
                        if (编队 != null) { 编队.显示第几个封地 = 号; 编队.重置刷新将领列表(); }
                        // 父页恢复后仅刷新数据；由统一导航处理返回。
                        页.gameObject.SetActive(false);
                    });
                }
            });
        }

        private void 查看目标(军事目标类型 类型)
        {
            搜索词 = "";
            选中目标 = null;
            目标页.打开(类型 == 军事目标类型.攻城 ? "出征目标 · 攻城" : 类型 == 军事目标类型.驻防 ? "出征目标 · 驻防" : "出征目标 · 资源点",
                页 => 构造目标(页, 类型));
        }
        private void 构造目标(军事详情面板 页, 军事目标类型 类型)
        {
            if (类型 == 军事目标类型.资源点 && 目标提供器.查询(类型, "").Count == 0)
            {
                页.状态.text = "资源点 0处";
                页.说明("请先进入角色并建立封地；资源目录会在当前世界中生成。", 40);
                return;
            }
            页.状态.text = 类型 == 军事目标类型.资源点 ? "铜矿与牧场 · 每人最多两处" : 目标提供器.状态说明;
            var 搜索 = 页.输入("名称或坐标", 搜索词, false, 值 => { 搜索词 = 值.Trim(); });
            页.操作行("查找目标", 类型 == 军事目标类型.资源点 ? "输入铜矿、牧场或坐标，查看资源点详情。" : "输入城池名称或坐标，查看目标详情。", "搜索", () => { 搜索词 = 搜索.text.Trim(); 页.刷新(); });
            var 目标表 = 目标提供器.查询(类型, 搜索词);
            if (目标表.Count == 0)
                页.说明(类型 == 军事目标类型.资源点 ? "未找到匹配资源点。可修改类型或坐标后重试。" : "未找到匹配城池。可修改名称或坐标后重试。", 66);
            // 真实世界有数百城池；每次最多显示40条，先用名称/坐标缩小结果。
            if (目标表.Count > 40) 页.说明("匹配" + 目标表.Count + "座城池，显示前40座。请输入名称或坐标缩小范围。", 40);
            foreach (var 目标 in 目标表.Take(40))
            {
                var 当前 = 目标;
                页.操作行(目标.名称 + " (" + 目标.坐标x + "," + 目标.坐标y + ")", 目标.说明, "选择", () =>
                {
                    选中目标 = 当前.标识;
                    操作页.打开("军事目标 · " + 当前.名称, 详情 => 构造目标详情(详情, 类型));
                });
            }
        }
        private void 构造目标详情(军事详情面板 页, 军事目标类型 类型)
        {
            if (类型 == 军事目标类型.资源点)
            {
                资源点界面适配.构造详情(页, 选中目标, 打开资源出征, 情 =>
                {
                    var 军情页 = 获取<军情界面脚本>();
                    if (军情页 != null) 军情页.进入指定军情(情);
                });
                return;
            }
            var 目标 = 目标提供器.查询(类型, "").FirstOrDefault(x => x.标识 == 选中目标);
            if (目标 == null) { 页.说明("目标已变更，请返回目录重新选择。"); return; }
            页.状态.text = "出发封地：" + 当前封地名字();
            页.说明(目标.名称 + " (" + 目标.坐标x + "," + 目标.坐标y + ")\n" + 目标.说明, 70);
            var 玩家 = 当前玩家();
            bool 有封地 = 玩家 != null && 玩家.封地信息表.Count > 0;
            if (类型 == 军事目标类型.攻城)
                页.操作行("选择攻城将领", "选择将领并确认出征后，部队才会出发。", "选择将领", () => 打开攻城(目标), 有封地);
            else
            {
                var 城 = 全局变量.所有城池列表.FirstOrDefault(x => x.坐标x == 目标.坐标x && x.坐标y == 目标.坐标y);
                var 检查 = 和平驻防规则.检查目标(玩家, 城);
                if (!检查.成功) 页.说明(检查.说明, 40);
                页.操作行("驻防派遣", "到达后驻守；撤回到达封地后恢复空闲，保留配兵。", "选择将领", () => 打开攻城(目标, true), 有封地 && 检查.成功);
                var 任务 = 和平驻防规则.获取任务().Where(x => x.所属玩家 == 全局变量.本机身份 && x.驻防坐标x == 目标.坐标x && x.驻防坐标y == 目标.坐标y).ToList();
                if (任务.Count == 0) 页.说明("本城没有我方驻防任务。", 40);
                foreach (var 情 in 任务)
                {
                    var 当前 = 情;
                    string 名 = 情.队列将领列表.Count > 0 ? 情.队列将领列表[0].将领属性.初始属性.名字 : "将领";
                    页.数据行(名 + "等" + 情.队列将领列表.Count + "将", 和平驻防规则.状态说明(情), 情.阶段 == 驻防任务阶段.撤回 ? "返回中" : "撤回", () =>
                    {
                        var 结果 = 和平驻防规则.撤回(当前玩家(), 当前, TIME.getTime());
                        页.刷新();
                        页.提示(结果);
                    }, 情.阶段 != 驻防任务阶段.撤回 && 情.阶段 != 驻防任务阶段.参战);
                }
            }
            if (!有封地) 页.说明("尚未拥有封地，无法选择出征将领。");
        }
        private void 打开资源出征(string ID)
        {
            var 检查 = 资源点规则.本地.检查目标(ID);
            if (!检查.Success) { 操作页.提示(军事结果.拒绝(军事错误.状态冲突, 检查.Message)); return; }
            var 根对象 = 根("攻占城池出征界面UI");
            var 脚本 = 根对象 != null ? 根对象.GetComponent<选择出征将领>() : null;
            if (脚本 == null) { 操作页.提示(军事结果.拒绝(军事错误.未接入, "当前场景未找到原将领选择页。")); return; }
            根对象.gameObject.SetActive(true);
            脚本.切换出征封地(全局变量.第几个封地);
            脚本.设置资源点模式(ID);
        }

        private void 打开攻城(军事目标 目标, bool 和平驻防 = false)
        {
            var 根对象 = 根("攻占城池出征界面UI");
            var 脚本 = 根对象 != null ? 根对象.GetComponent<选择出征将领>() : null;
            if (脚本 == null) { 操作页.提示(军事结果.拒绝(军事错误.未接入, "当前场景未找到攻城将领选择页。")); return; }
            var 城 = 所有城池界面脚本.根据坐标获取指定城池(目标.坐标x, 目标.坐标y);
            if (城 == null) { 操作页.提示(军事结果.拒绝(军事错误.状态冲突, "城池已变更，请重新选择。")); return; }
            脚本.index = 0;
            脚本.城池坐标x = 目标.坐标x;
            脚本.城池坐标y = 目标.坐标y;
            根对象.gameObject.SetActive(true);
            脚本.切换出征封地(全局变量.第几个封地);
            脚本.刷新城池();
            脚本.设置和平驻防模式(和平驻防);
        }

        private void 战场说明()
        {
            动态页.打开("军情与战场说明", 页 =>
            {
                页.状态.text = "军情 " + 全局变量.军情列表.Count + "条";
                页.说明("每次最多出征5名空闲将领，需先配兵，体力至少5点。", 40);
                if (全局变量.军情列表.Count == 0) 页.说明("当前没有军情，请先选择将领出征。", 40);
                foreach (var 情 in 全局变量.军情列表)
                {
                    var 驻 = 情 as 驻防军情信息;
                    string 状态 = 驻 != null ? 和平驻防规则.状态说明(驻) : 情.已进入战场 ? "战斗中" : 情.到达时间 > TIME.getTime() ? "行军中 · 剩余" + TIME.ToTimeFormat(情.到达时间 - TIME.getTime()) : "已到达 · 等待结算";
                    页.说明(和平驻防规则.获取用途(情) + " (" + 情.坐标x + "," + 情.坐标y + ") · 将领 " + 情.队列将领列表.Count + "名\n" + 状态, 54);
                }
            });
        }
        private void 查看战略()
        {
            if (战略页 == null) return;
            if (战斗选择页 != null) 战斗选择页.gameObject.SetActive(false);
            var 增援布局 = 战斗页.transform.Find("增援界面UI");
            if (增援布局 != null) 增援布局.gameObject.SetActive(false);
            战略页.打开("战场战略", 页 =>
            {
                var 系统 = 战斗页.战斗系统脚本对象;
                if (系统 == null) { 页.说明("当前没有可查看的战场，请先进入实际战斗。"); return; }
                页.状态.text = "攻方 " + 系统.攻方兵力.ToString("0") + " · 守方 " + 系统.守方兵力.ToString("0");
                页.说明("选择部队攻击目标的优先顺序。", 36);
                var 本机参战 = 系统.GetComponentsInChildren<将领功能>(true).Where(x => x.本将领信息 != null && x.本将领信息.详细信息.身份 == 全局变量.本机身份 && x.本将领信息.详细信息.状态 == 1).ToArray();
                if (本机参战.Length == 0) 页.说明("本机没有正在此战场作战的将领，仅可查看战略说明。");
                string[] 模式 = { "伤害优先", "强敌优先", "追击损兵" };
                for (int i = 0; i < 模式.Length; i++)
                {
                    int 值 = i;
                    页.操作行(模式[i], i == 0 ? "优先攻击预计伤害最高的目标" : i == 1 ? "优先攻击攻击力最高的目标" : "优先追击损兵最多的目标", "应用", () =>
                    {
                        int 数量 = 0;
                        foreach (var 将 in 本机参战)
                            if (将 != null && 将.本将领信息.详细信息.身份 == 全局变量.本机身份 && 将.本将领信息.详细信息.状态 == 1)
                            { 将.本将领信息.详细信息.攻击模式 = 值; 数量++; }
                        页.刷新();
                        页.提示(数量 > 0 ? 军事结果.通过("已将" + 数量 + "名本机将领设为" + 模式[值] + "。") : 军事结果.拒绝(军事错误.状态冲突, "参战将领状态已变更。"));
                    }, 本机参战.Length > 0);
                }
                foreach (var 将 in 本机参战)
                {
                    int 值 = Mathf.Clamp((int)将.本将领信息.详细信息.攻击模式, 0, 2);
                    页.说明(将.本将领信息.将领属性.初始属性.名字 + " · " + 模式[值] + " · 兵力 " + 将.本将领信息.详细信息.剩余兵力.ToString("0"), 34);
                }
            });
        }
    }
}
