using UnityEngine;
using UnityEngine.UI;
using 缺失界面.窗口2;

public class 显示概况脚本 : MonoBehaviour
{
    public Text 国家名字;
    public Text 国王名字;
    public Text 国都名字;
    public Text 城池数量;
    public Text 成员数量;
    public Text 科技等级;
    public Text 排名;
    public Text 国家名字效率显示;
    // A display context only: it never changes the local player or their nation.
    public string 查看国号 { get; set; }
    public string 当前查看国号 { get { return 查看国号 ?? NationDataSource.Current.OwnNationCode; } }

    private void OnEnable() { 刷新显示(); }

    public void 刷新显示()
    {
        var 数据 = NationDataSource.Current;
        var 国家 = 数据.ReadNation(当前查看国号);
        if (国家 != null)
        {
            国家名字.text = Short(国家.Name, 16) + "(" + Short(国家.Code, 3) + ")";
            国王名字.text = 国家.King == null ? "无国王" : Short(国家.King.Name, 10);
            国都名字.text = 国家.Capital == null ? "无有效国都" : Short(国家.Capital.Name, 10);
            城池数量.text = 国家.Cities.Count + "/" + 全局变量.所有城池列表.Count;
            成员数量.text = 国家.Members.Count.ToString();
            科技等级.text = NationDataSource.Number(国家.Technology);
            排名.text = 国家.Rank.ToString();
            国家名字效率显示.text = Short(国家.Name, 14) + "(" + Short(国家.Code, 3) + "," + NationDataSource.Scale(国家.Cities.Count) + ")    效率:" + NationDataSource.Number(国家.Efficiency) + "%";
        }
        else
        {
            国家名字.text = "未加入国家"; 国王名字.text = "无国王"; 国都名字.text = "无国都";
            城池数量.text = "0"; 成员数量.text = "0"; 科技等级.text = "0"; 排名.text = "—";
            国家名字效率显示.text = "返回换国入口加入有效国家";
        }
        foreach (Text 文本 in new[] { 国家名字, 国王名字, 国都名字, 城池数量, 成员数量, 科技等级, 排名, 国家名字效率显示 })
        {
            if (文本 == null) continue;
            文本.supportRichText = false; 文本.horizontalOverflow = HorizontalWrapMode.Wrap; 文本.verticalOverflow = VerticalWrapMode.Truncate;
            文本.resizeTextForBestFit = true; 文本.resizeTextMinSize = 10; 文本.resizeTextMaxSize = Mathf.Max(10, 文本.fontSize);
        }
    }
    private static string Short(string value, int max) { value = value ?? ""; return value.Length <= max ? value : value.Substring(0, max) + "…"; }
}
