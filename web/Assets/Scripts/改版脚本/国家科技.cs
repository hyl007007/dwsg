using UnityEngine;
using UnityEngine.UI;

public class 国家科技 : MonoBehaviour
{
    public Text 攻击科技等级对象;

    public Text 防御科技等级对象;

    public Text 资源科技等级对象;

    private 国家信息库类 国家信息库类;

    public GameObject 概况脚本;

    public void 刷新显示信息()
    {

        int identity = 全局变量.本机身份;
        if (identity < 0 || identity >= 全局变量.所有玩家数据表.Count) return;
        国家信息库类 = 全局方法类.获取指定名字的国家(全局变量.所有玩家数据表[identity].基础信息.国家);
        if (国家信息库类 == null) return;
        国家信息库类.刷新科技信息();
        攻击科技等级对象.text = (int)国家信息库类.攻击科技 > 0 ? 国家信息库类.攻击科技.ToString() : "0";
        防御科技等级对象.text = 国家信息库类.防御科技 > 0 ? 国家信息库类.防御科技.ToString() : "0";
        资源科技等级对象.text = 国家信息库类.资源科技 > 0 ? 国家信息库类.资源科技.ToString() : "0";
        概况脚本.transform.GetComponent<显示概况脚本>().刷新显示();

    }

    public void 升级攻击科技()
    {
        提升科技("攻击科技");
    }

    public void 升级防御科技()
    {
        提升科技("防御科技");
    }

    public void 升级资源科技()
    {
        提升科技("资源科技");
    }

    private void 提升科技(string 科技类型)
    {
        NationClient.Research(科技类型, result =>
        {
            全局变量.提示类.显示信息(result.Message);
            刷新显示信息();
        });
    }
}
