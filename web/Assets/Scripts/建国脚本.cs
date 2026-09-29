using UnityEngine;
using UnityEngine.UI;
using 缺失界面.窗口2;

public class 建国脚本 : MonoBehaviour
{
    public Text 国名对象;
    public Text 国号对象;
    public Text 国都对象;
    public Text 国家宣言对象;
    public Text 国名输入对象;
    public Text 国号输入对象;
    public Text 宣言输入对象;
    public 城池信息库类 国都城池信息;
    public GameObject 国家列表布局;
    private int 打开时角色 = -1;

    private void OnEnable()
    {
        打开时角色 = NationDataSource.Current.ActorId;
        配置输入(国名输入对象, 国名对象, 16); 配置输入(国号输入对象, 国号对象, 1); 配置输入(宣言输入对象, 国家宣言对象, 100);
        if (!NationBasicActions.Current.CanUseCapital(国都城池信息))
        { 国都城池信息 = null; if (国都对象 != null) 国都对象.text = "请选择国都"; }
        NationOriginalControls.Fit(国都对象);
        配置表单对齐();
        配置备注();
    }

    private void 配置表单对齐()
    {
        var 列表 = transform.Find("国家信息布局列表"); if (列表 == null) return;
        foreach (Transform 行 in 列表)
        {
            var 说明对象 = 行.Find("说明文本");
            var 说明 = 说明对象 == null ? null : 说明对象.GetComponent<Text>();
            if (说明 == null) continue;
            说明.alignByGeometry = true;
            var 输入区域 = 行.Find("InputField") as RectTransform;
            if (输入区域 == null) continue;
            // 沿用同一行标签的垂直锚点；输入框横向尺寸和原背景不变。
            var 标签区域 = 说明.rectTransform;
            输入区域.anchorMin = new Vector2(输入区域.anchorMin.x, 标签区域.anchorMin.y);
            输入区域.anchorMax = new Vector2(输入区域.anchorMax.x, 标签区域.anchorMax.y);
            输入区域.pivot = new Vector2(输入区域.pivot.x, 标签区域.pivot.y);
            输入区域.anchoredPosition = new Vector2(输入区域.anchoredPosition.x, 标签区域.anchoredPosition.y);
        }
        var 资金对象 = 列表.Find("建国资金说明文本");
        var 资金文字 = 资金对象 == null ? null : 资金对象.GetComponent<Text>();
        if (资金文字 != null) 资金文字.alignByGeometry = true;
        if (国都对象 != null) 国都对象.alignByGeometry = true;
    }

    private void 配置备注()
    {
        var 备注对象 = transform.Find("备注说明文本"); if (备注对象 == null) return;
        var 备注 = 备注对象.GetComponent<Text>(); if (备注 == null) return;
        备注.text = "国名3-16字符；国号1个汉字\n国都：自有未交战县城及以上，现有国都除外\n材料：玉玺*1，虎符*10\n印绶*100，令牌*1000";
    }

    private static void 配置输入(Text 输入, Text 显示, int 上限)
    {
        if (输入 == null) return;
        var 编辑框 = 输入.GetComponentInParent<InputField>(); if (编辑框 == null) return;
        编辑框.characterLimit = 上限;
        原界面文字样式.单行输入(输入, 显示, TextAnchor.MiddleLeft, 18);
    }

    private static string 读取(Text 输入, Text 显示)
    {
        var 编辑框 = 输入 == null ? null : 输入.GetComponentInParent<InputField>();
        return (编辑框 != null ? 编辑框.text : 输入 != null ? 输入.text : 显示 != null ? 显示.text : "") ?? "";
    }

    public void 输入国家名字() { if (国名对象 != null) 国名对象.text = 读取(国名输入对象, 国名对象).Trim(); }
    public void 输入国家国号() { if (国号对象 != null) 国号对象.text = 读取(国号输入对象, 国号对象).Trim(); }
    public void 输入国家宣言() { if (国家宣言对象 != null) 国家宣言对象.text = 读取(宣言输入对象, 国家宣言对象).Trim(); }

    public void 确定建国()
    {
        if (国都城池信息 == null) { 提示("请先选择自己拥有的国都城池。"); return; }
        var 结果 = NationBasicActions.Current.Found(读取(国名输入对象, 国名对象), 读取(国号输入对象, 国号对象),
            读取(宣言输入对象, 国家宣言对象), 国都城池信息.坐标x, 国都城池信息.坐标y, 打开时角色);
        提示(结果.Message); if (!结果.Success) return;
        gameObject.SetActive(false); if (国家列表布局 != null) 国家列表布局.SetActive(false);
        NationOriginalControls.ReturnToNation(this, NationDataSource.Current.OwnNationCode);
    }

    private static void 提示(string 内容) { if (全局变量.提示类 != null) 全局变量.提示类.显示信息(内容); }
}
