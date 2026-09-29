using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class 单将撤退脚本 : MonoBehaviour
{
    public Camera 摄像机;
    public GameObject 撤退布局;
    public GameObject 被选中将领;
    public Transform 原小弹窗参考;
    private 原界面小弹窗 弹窗;
    private Text 撤退说明;
    private readonly List<RaycastResult> 界面命中 = new List<RaycastResult>();

    private bool 点击操作界面()
    {
        var 事件系统 = EventSystem.current;
        if (事件系统 == null) return false;
        // 观战入口将相机挂到当前战斗地图根；战斗系统在该地图的 Tilemap 分支。
        var 地图 = 摄像机 == null ? null : 摄像机.transform.parent;
        var 战场 = 地图 == null ? null : 地图.GetComponentInChildren<战斗系统>();
        if (战场 == null || !战场.正在观战 || !摄像机.isActiveAndEnabled) return true;
        界面命中.Clear();
        事件系统.RaycastAll(new PointerEventData(事件系统) { position = Input.mousePosition }, 界面命中);
        foreach (var 命中 in 界面命中)
        {
            // 原战场的 ScrollRect 背景也接收射线，须保留它来拖动地图；它不应挡住选将。
            // 战场外的窗口以及战场内的可操作控件仍阻止穿透。
            if (命中.gameObject == null) continue;
            if (!命中.gameObject.transform.IsChildOf(地图) ||
                命中.gameObject.GetComponentInParent<Selectable>() != null) return true;
        }
        return false;
    }

    private bool 可撤退(将领功能 将)
    {
        if (!将 || 将.正在退场 || 将.本将领信息 == null || 将.本将领信息.详细信息 == null ||
            将.本将领信息.详细信息.状态 != 1 || 将.本将领信息.详细信息.身份 != 全局变量.本机身份 ||
            !将.战斗系统脚本对象 || 将.战斗系统脚本对象.战斗结束 ||
            全局变量.所有玩家数据表 == null || 全局变量.本机身份 < 0 ||
            全局变量.本机身份 >= 全局变量.所有玩家数据表.Count) return false;
        var 主 = 全局变量.所有玩家数据表[全局变量.本机身份];
        if (主 == null || 主.封地信息表 == null) return false;
        foreach (var 地 in 主.封地信息表)
            if (地 != null && 地.将领信息表 != null && 地.将领信息表.Contains(将.本将领信息)) return true;
        return false;
    }

    private void 清除选择()
    {
        if (撤退布局) 撤退布局.SetActive(false);
        被选中将领 = null;
    }

    private void 显示撤退(将领功能 将)
    {
        if (弹窗 == null)
        {
            var 撤退 = 撤退布局.transform.Find("撤退").GetComponent<Button>();
            var 返回 = 撤退布局.transform.Find("返回").GetComponent<Button>();
            弹窗 = 撤退布局.AddComponent<原界面小弹窗>();
            弹窗.初始化(原小弹窗参考, "将领撤退", 清除选择);
            撤退说明 = 弹窗.添加说明("撤退说明", "");
            弹窗.添加说明("影响说明", "撤出当前战斗，返回所属封地。", 16);
            弹窗.使用原按钮(撤退);
            弹窗.使用原按钮(返回);
            返回.onClick.AddListener(清除选择);
        }
        撤退说明.text = 将.本将领信息.将领属性.初始属性.名字 + " · 撤出本次战斗？";
        撤退布局.SetActive(true);
    }

    void Update()
    {
        if (被选中将领 && !可撤退(被选中将领.GetComponent<将领功能>())) 清除选择();
        if (!Input.GetMouseButtonDown(0)) return;
        // 点击 UI 时不穿透到战场里选将领。
        if (点击操作界面()) return;
        清除选择();
        RaycastHit ray;
        if (摄像机 && Physics.Raycast(摄像机.ScreenPointToRay(Input.mousePosition), out ray))
        {
            var 将 = ray.transform.GetComponentInChildren<将领功能>();
            if (可撤退(将))
            {
                被选中将领 = 将.gameObject;
                if (撤退布局) 显示撤退(将);
            }
        }
    }

    public void 撤退选中将领()
    {
        var 将 = 被选中将领 ? 被选中将领.GetComponent<将领功能>() : null;
        if (可撤退(将)) 将.退出战场(true);
        清除选择();
    }
}
