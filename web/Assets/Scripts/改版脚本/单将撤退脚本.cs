using UnityEngine;

public class 单将撤退脚本 : MonoBehaviour
{
    public Camera 摄像机;
    public GameObject 撤退布局;
    public GameObject 被选中将领;

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

    void Update()
    {
        if (被选中将领 && !可撤退(被选中将领.GetComponent<将领功能>())) 清除选择();
        if (!Input.GetMouseButtonDown(0)) return;
        // 点击 UI 时不穿透到战场里选将领。
        if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        清除选择();
        RaycastHit ray;
        if (摄像机 && Physics.Raycast(摄像机.ScreenPointToRay(Input.mousePosition), out ray))
        {
            var 将 = ray.transform.GetComponentInChildren<将领功能>();
            if (可撤退(将))
            {
                被选中将领 = 将.gameObject;
                if (撤退布局) 撤退布局.SetActive(true);
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
