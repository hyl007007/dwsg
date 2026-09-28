using UnityEngine;
using UnityEngine.EventSystems;

public sealed class 界面点击来源 : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [HideInInspector] public 界面窗口管理器 管理器;
    [HideInInspector] public 界面互斥窗口 所属窗口;

    public void OnPointerDown(PointerEventData 数据)
    {
        if (管理器 != null) 管理器.记录点击来源(所属窗口);
    }

    public void OnPointerUp(PointerEventData 数据)
    {
        if (管理器 != null) 管理器.释放指针();
    }
}
