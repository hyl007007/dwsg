using UnityEngine;
using UnityEngine.Scripting;

// 供原场景 UnityEvent 按原顺序调用；组件仅在接入窗口按钮时添加。
[DisallowMultipleComponent]
public sealed class 界面窗口开关 : MonoBehaviour
{
    [Preserve]
    public void SetActive(bool 显示) { 界面窗口动画.设置显示(gameObject, 显示); }
}
