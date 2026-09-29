using UnityEngine;
using UnityEngine.UI;
using 缺失界面.窗口2;

public class 显示国库脚本 : MonoBehaviour
{
    public Text 铜钱显示文本;
    public Text 粮食显示文本;

    private void OnEnable() { 获取国家国库(); }

    public void 获取国家国库()
    {
        var 数据 = NationDataSource.Current; var 国家 = 数据.ReadNation(数据.OwnNationCode);
        if (铜钱显示文本 != null) 铜钱显示文本.text = 国家 == null ? "无国家" : NationDataSource.Number(国家.Copper);
        if (粮食显示文本 != null) 粮食显示文本.text = 国家 == null ? "无国家" : NationDataSource.Number(国家.Grain);
        刷新官职();
    }

    public void 领取俸禄()
    {
        var 结果 = NationDataSource.Current.ClaimSalary();
        if (全局变量.提示类 != null) 全局变量.提示类.显示信息(结果.Message);
        获取国家国库();
    }

    private void 刷新官职()
    {
        int 索引 = 全局变量.本机身份;
        if (索引 < 0 || 索引 >= 全局变量.所有玩家数据表.Count) return;
        var 玩家 = 全局变量.所有玩家数据表[索引]; if (玩家 == null || 玩家.基础信息 == null) return;
        var 国家 = NationDataSource.Current.ReadNation(玩家.基础信息.国家);
        玩家.基础信息.官职 = 国家 == null ? 玩家数据结构.官职信息.平民 : NationSalaryRules.Office(玩家.基础信息.战功, 国家.KingId == 玩家.基础信息.ID);
    }
}
