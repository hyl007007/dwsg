using UnityEngine;
using UnityEngine.UI;
using 玩家数据结构;

public class 显示国库脚本 : MonoBehaviour
{
    public Text 铜钱显示文本;

    public Text 粮食显示文本;

    private void Start()
    {
        刷新官职();
    }

    public void 获取国家国库()
    {
        国家信息库类 国家 = 全局方法类.获取指定名字的国家(全局变量.所有玩家数据表[全局变量.本机身份].基础信息.国家);
        铜钱显示文本.text = 国家.铜钱.ToString();
        粮食显示文本.text = 国家.粮食.ToString();
        print(国家.铜钱 + 国家.国名 + 国家.粮食);
    }
    private void Update()
    {
       
    }

    public void 领取俸禄()
    {
        if (全局变量.领取倒计时 > 0)
        {
            全局变量.提示类.显示信息("未到领取时间");
            return;
        }
        NationClient.ClaimSalary(result =>
        {
            全局变量.提示类.显示信息(result.Message);
            if (result.Code == Dwsg.Shared.GameCodes.Ok)
            {
                获取国家国库();
                刷新官职();
            }
        });
    }

    private void 刷新官职()
    {
        if (Dwsg.Network.GameNetwork.Enabled) return;
        var player = 全局变量.所有玩家数据表[全局变量.本机身份]; int office;
        if (Dwsg.Shared.Economy.NationSalaryRules.TryOffice(player.基础信息.战功, out office)) player.基础信息.官职 = (官职信息)office;
    }
}
