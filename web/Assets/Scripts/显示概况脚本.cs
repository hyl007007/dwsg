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
            国家名字效率显示.text = "选择入国，可加入或建立国家";
        }
        foreach (Text 文本 in new[] { 国家名字, 国王名字, 国都名字, 城池数量, 成员数量, 科技等级, 排名, 国家名字效率显示 })
        {
            // 国家名称和效率另有保留后缀的省略规则，其余字段也保留完整字体行高。
            if (文本 != 国家名字 && 文本 != 国家名字效率显示) NationOriginalControls.SingleLine(文本);
        }
        // Keep the original readable size. Full names remain available through the existing 查看 links.
        Clip(国家名字, 国家 == null ? "" : "(" + Short(国家.Code, 3) + ")");
        Clip(国王名字); Clip(国都名字);
        Clip(国家名字效率显示, 国家 == null ? "" : "    效率:" + NationDataSource.Number(国家.Efficiency) + "%");
    }
    private static void Clip(Text text, string suffix = "")
    {
        NationOriginalControls.SingleLine(text, suffix);
    }
    private static string Short(string value, int max) { value = value ?? ""; return value.Length <= max ? value : value.Substring(0, max) + "…"; }
}
