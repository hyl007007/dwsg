using UnityEngine;

public sealed class 界面互斥窗口 : MonoBehaviour
{
    [HideInInspector] public 界面窗口管理器 管理器;

    private void OnEnable()
    {
        if (管理器 != null) 管理器.打开窗口(this);
    }

    private void OnDisable()
    {
        if (管理器 != null) 管理器.关闭窗口(this);
    }
}
