using UnityEngine;
using UnityEngine.UI;
using 缺失界面.窗口2;

public class 显示个人脚本 : MonoBehaviour
{
    public Text 战功显示;
    public Text 贡献显示;
    public Text 官职显示;
    public Text 轮选剩余时间;
    private float 下次刷新;

    private void OnEnable() { 下次刷新 = 0; 刷新显示(); }

    public void 刷新显示()
    {
        if (Time.unscaledTime < 下次刷新) return;
        var 数据 = NationDataSource.Current;
        var 国家 = 数据.ReadNation(数据.OwnNationCode);
        var 玩家 = 数据.ReadPlayer(数据.ActorId);
        if (国家 != null && 玩家 != null)
        {
            战功显示.text = NationDataSource.Number(玩家.Merit);
            贡献显示.text = NationDataSource.Number(玩家.Contribution);
            官职显示.text = 玩家.Office;
            轮选剩余时间.text = TIME.ToTimeFormat(国家.ElectionRemaining);
        }
        else
        {
            战功显示.text = 玩家 == null ? "0" : NationDataSource.Number(玩家.Merit);
            贡献显示.text = 玩家 == null ? "0" : NationDataSource.Number(玩家.Contribution);
            官职显示.text = "无国家"; 轮选剩余时间.text = "无轮选";
        }
        下次刷新 = Time.unscaledTime + 1;
    }

    private void Update() { 刷新显示(); }
}
