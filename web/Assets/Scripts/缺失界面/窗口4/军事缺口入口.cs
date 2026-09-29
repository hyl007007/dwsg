using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using 玩家数据结构;

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
            var 出征状态 = 出征 != null ? 出征.GetComponentsInChildren<Text>(true) : new Text[0];
            foreach (var 字 in 出征状态)
                if (字.name == "出征封地显示") 字.text = 当前封地名字();
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
            UnityAction 回调 = () => 动作();
            按.onClick.AddListener(回调);
            监听.Add(new KeyValuePair<Button, UnityAction>(按, 回调));
            界面窗口管理器.注册运行时按钮(按);
        }
        private void OnDestroy()
        {
            foreach (var 项 in 监听) if (项.Key != null) 项.Key.onClick.RemoveListener(项.Value);
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
            return 将.详细信息.状态 == 0 ? "空闲" : 将.详细信息.状态 == 1 ? "出征/战斗" : 将.详细信息.状态 == 3 ? "被俘" : "其他占用状态";
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
                页.状态.text = "本地兵员 · 按封地查看，沿用现有劝降、闲兵及伤兵操作";
                if (玩家.封地信息表.Count == 0) 页.说明("尚未拥有封地。先建立封地，才能管理俘虏、闲兵和伤兵。");
                foreach (var 封地 in 玩家.封地信息表)
                {
                    var 当前 = 封地;
                    double 闲兵 = 封地.闲兵信息表.Sum(x => Math.Max(0, x.数量));
                    double 伤兵 = 封地.伤兵信息表.Sum(x => Math.Max(0, x.数量));
                    double 配兵 = 封地.将领信息表.Sum(x => Math.Max(0, x.将领配兵.数量));
                    string 文案 = 类型 == "俘虏" ? "俘虏 " + 封地.俘虏信息表.Count + "名" : 类型 == "伤兵" ? "伤兵 " + 伤兵.ToString("0") + "名" : "闲兵 " + 闲兵.ToString("0") + " · 已配兵 " + 配兵.ToString("0");
                    页.操作行(封地.封地名字, 文案 + "\n查看本封地现有兵员列表", "查看", () => 打开封地兵员(当前, 类型), 封地信息页 != null);
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
                页.状态.text = "本地规则：" + 类型 + "为即时操作 · 当前没有计时队列";
                页.说明(类型 == "建筑" ? "当前没有待施工项目。建造、升级、拆除均在原封地建筑页确认后即时完成。"
                    : 类型 == "征兵" ? "当前没有待征士兵。招募仍在原兵营页确认后即时扣除资源并加入闲兵。"
                    : "当前没有待研究项目。科技等级与升级仍在原书院页查看、操作。", 66);
                if (玩家.封地信息表.Count == 0) { 页.说明("尚未拥有封地，无法建设、征兵或研究。请先建立封地。"); return; }
                foreach (var 封地 in 玩家.封地信息表)
                {
                    var 当前 = 封地;
                    页.操作行(封地.封地名字, "待" + 类型 + "项目：0 · 前往现有封地页查看详情和操作", "前往封地", () => 前往封地(当前), 封地页 != null);
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
            页.状态.text = 封地.封地名字 + " · " + 状态名(将) + " · 本地操作随世界存档保存";
            页.说明("等级 " + 将.将领属性.成长点数.等级 + " · 经验 " + 将.详细信息.经验.ToString("0") + "/" + 将.获取当前等级升级需要经验(将.将领属性.成长点数.等级).ToString("0") +
                "\n体力 " + 将.详细信息.剩余体力.ToString("0") + "/" + 将.将领属性.最终属性.体力上限.ToString("0") + " · 铜钱 " + 玩家.财产信息.铜钱.ToString("0"), 56);
            if (操作 == "修炼")
            {
                页.说明("本地修炼规则：每次消耗10体力、等级×100铜钱；获得本级升级经验的10%（向上取整）。最高99级，仅空闲将领可修炼。", 68);
                页.操作行("本次修炼", "经验+" + 军事本地规则.修炼经验(将) + " · 铜钱-" + 军事本地规则.修炼费用(将) + " · 体力-10", "确认修炼",
                    () => 完成操作(页, 军事本地规则.修炼(当前玩家(), 封地, 将)), 将.将领属性.成长点数.等级 < 99);
            }
            else if (操作 == "恢复体力")
            {
                double 丹数 = 玩家.背包道具列表.获取指定道具数量("活血丹");
                页.说明("活血丹沿用现有道具说明：每个恢复50体力，达到上限时不消耗。背包数量：" + 丹数.ToString("0"), 68);
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
                页.操作行("调整当前部队", "可用闲兵 " + 军事本地规则.闲兵数量(封地, 兵种).ToString("0") + "名 · 不创建新士兵", "确认调整", () =>
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
                        var 出征根 = 根("出征界面UI (1)");
                        var 标签 = 出征根 != null ? 出征根.Find("封地操作/出征封地显示") : null;
                        if (标签 != null) 标签.GetComponent<Text>().text = 当前.封地名字;
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
            页.状态.text = 目标提供器.状态说明;
            var 搜索 = 页.输入("名称或坐标", 搜索词, false, 值 => { 搜索词 = 值.Trim(); });
            页.操作行("查找目标", "输入城池名称或坐标，查看目标详情。", "搜索", () => { 搜索词 = 搜索.text.Trim(); 页.刷新(); });
            var 目标表 = 目标提供器.查询(类型, 搜索词);
            if (目标表.Count == 0)
                页.说明(类型 == 军事目标类型.资源点 ? "当前版本暂未开放资源点。" : "未找到匹配城池。可修改名称或坐标后重试。", 66);
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
            var 目标 = 目标提供器.查询(类型, "").FirstOrDefault(x => x.标识 == 选中目标);
            if (目标 == null) { 页.说明("目标已变更，请返回目录重新选择。"); return; }
            页.状态.text = 目标提供器.状态说明 + " · 出发封地：" + 当前封地名字();
            页.说明(目标.名称 + " (" + 目标.坐标x + "," + 目标.坐标y + ")\n" + 目标.说明, 70);
            var 玩家 = 当前玩家();
            bool 有封地 = 玩家 != null && 玩家.封地信息表.Count > 0;
            if (类型 == 军事目标类型.攻城)
                页.操作行("选择攻城将领", "选择将领并确认出征后，部队才会出发。", "选择将领", () => 打开攻城(目标), 有封地);
            else
            {
                页.说明("驻防派遣暂未开放。可查看城池已有驻防。", 66);
                var 城 = 全局变量.所有城池列表.FirstOrDefault(x => x.坐标x == 目标.坐标x && x.坐标y == 目标.坐标y);
                if (城 != null)
                    foreach (var 将 in 城.城池玩家驻防列表)
                        页.说明("现有驻防：" + 将.将领属性.初始属性.名字 + " · " + 兵名(将.将领配兵.ID) + " " + 将.将领配兵.数量.ToString("0"), 36);
            }
            if (!有封地) 页.说明("尚未拥有封地，无法选择出征将领。");
        }
        private void 打开攻城(军事目标 目标)
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
        }

        private void 战场说明()
        {
            动态页.打开("军情与战场说明", 页 =>
            {
                页.状态.text = "本地模拟 · 界面倍速沿用已有战斗设置";
                页.说明("现有出征最多选择5名空闲将领；需要配兵，体力至少5点。山贼本地行军默认10秒，攻城默认10秒。战果由既有战斗系统结算。", 70);
                页.说明("战略采用已有攻击模式：伤害优先、强敌优先、追击损兵。倍速设置位于战斗设置按钮，本页不调整时间流速。", 66);
                if (全局变量.军情列表.Count == 0) 页.说明("当前没有本地军情。可选择山贼或城池，在将领页确认出征后查看行军。", 64);
                foreach (var 情 in 全局变量.军情列表)
                {
                    string 状态 = 情.已进入战场 ? "已进入战场" : 情.到达时间 > TIME.getTime() ? "行军中 · 剩余" + TIME.ToTimeFormat(情.到达时间 - TIME.getTime()) : "已到达 · 等待/执行本地结算";
                    页.说明((情.战场类型 == 0 ? "山贼" : "城池") + " (" + 情.坐标x + "," + 情.坐标y + ") · 将领 " + 情.队列将领列表.Count + "名\n" + 状态, 54);
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
                页.状态.text = "本地战斗 · 攻方 " + 系统.攻方兵力.ToString("0") + " · 守方 " + 系统.守方兵力.ToString("0");
                页.说明("沿用现有目标排序：伤害优先按预计伤害、强敌优先按攻击力、追击损兵按已损失兵力降序选择。只修改本机参战将领的攻击模式。", 74);
                var 本机参战 = 系统.GetComponentsInChildren<将领功能>(true).Where(x => x.本将领信息 != null && x.本将领信息.详细信息.身份 == 全局变量.本机身份 && x.本将领信息.详细信息.状态 == 1).ToArray();
                if (本机参战.Length == 0) 页.说明("本机没有正在此战场作战的将领，仅可查看战略说明。");
                string[] 模式 = { "伤害优先", "强敌优先", "追击损兵" };
                for (int i = 0; i < 模式.Length; i++)
                {
                    int 值 = i;
                    页.操作行(模式[i], "应用到当前战场的本机参战将领，不创建军情、不改变倍速。", "应用", () =>
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
