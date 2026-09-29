using UnityEngine;

public class 单将撤退脚本 : MonoBehaviour
{
    public Camera 摄像机;
    public GameObject 撤退布局;
    public GameObject 被选中将领;
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        RaycastHit ray;
        if (Input.GetMouseButtonDown(0))
        {
            //点在 UI 上（比如聊天面板、输入框）时不要穿透到战场里选将领
            if (UnityEngine.EventSystems.EventSystem.current != null && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }
            if (Physics.Raycast(摄像机.ScreenPointToRay(Input.mousePosition), out ray))
            {
                if (ray.transform.GetComponentInChildren<将领功能>())
                {
                    被选中将领 = ray.transform.GetComponentInChildren<将领功能>().gameObject;
                    将领功能 选中将领 = 被选中将领.GetComponent<将领功能>();
                    if (选中将领.战斗系统脚本对象.服务器战场)
                    {
                        bool 可以撤退 = 可以撤退联机将领(选中将领);
                        撤退布局.SetActive(可以撤退);
                        if (!可以撤退) 被选中将领 = null;
                    }
                    else if (选中将领.战斗系统脚本对象.攻身份 == 全局变量.本机身份)
                    {
                        撤退布局.SetActive(true);
                    }
                    
                }
            }   
        }
    }

    private bool 可以撤退联机将领(将领功能 将领)
    {
        return 将领.本将领信息.详细信息.身份 == 全局变量.本机身份
            && Dwsg.Combat.CombatClient.CanWithdrawUnit(将领.战斗系统脚本对象.服务器战场ID,
                Dwsg.Combat.CombatClient.GeneralId(将领.本将领信息.ID));
    }

    public void 撤退选中将领()
    {
        if (被选中将领 != null)
        {
            将领功能 将领 = 被选中将领.GetComponent<将领功能>();
            if (将领.战斗系统脚本对象.服务器战场)
            {
                if (!可以撤退联机将领(将领)) return;
                Dwsg.Combat.CombatClient.WithdrawGeneral(将领.战斗系统脚本对象.服务器战场ID,
                    Dwsg.Combat.CombatClient.GeneralId(将领.本将领信息.ID), result =>
                    {
                        if (this == null || result.Code != Dwsg.Shared.GameCodes.Ok) return;
                        撤退布局.SetActive(false);
                        被选中将领 = null;
                    });
                return;
            }
            被选中将领.GetComponent<将领功能>().战斗系统脚本对象.攻方兵力 -= 被选中将领.GetComponent<将领功能>().本将领信息.将领配兵.数量;
            被选中将领.GetComponent<将领功能>().设置死亡状态();
            Destroy(被选中将领, 0.5f);
            被选中将领.GetComponent<将领功能>().本将领信息.详细信息.状态 = 0.0;
            撤退布局.SetActive(false);
            被选中将领 = null;
        }

    }
}
