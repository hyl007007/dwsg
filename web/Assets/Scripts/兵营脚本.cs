using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Dwsg.Window3;

public class 兵营脚本 : MonoBehaviour
{
    public GameObject 标题列表对象;
    public Image 头像对象;
    public Text 名字等级对象;
    public GameObject 兵种列表对象;
    public 调整数量脚本 调整招募数量脚本对象;
    public int 第几个玩家, 第几个封地, 第几个建筑;
    public int 兵营类型 = 4;
    private HorizontalLayoutGroup 升级费用布局;
    private Text 升级费用文字;
    public void 刷新显示()
    {
        var b = FiefActions.Building(第几个玩家, 第几个封地, 第几个建筑);
        var f = FiefActions.Fief(第几个玩家, 第几个封地);
        if (b == null || b.类型 < 4 || b.类型 > 7) { if (名字等级对象 != null) 名字等级对象.text = "请重新选择兵营"; return; }
        兵营类型 = b.类型;
        if (标题列表对象 != null)
        {
            var titles = 标题列表对象.transform;
            for (int i = 0; i < titles.childCount; i++)
            {
                var title = titles.GetChild(i); bool selected = i == 兵营类型 - 4;
                var toggle = title.GetComponent<Toggle>();
                if (toggle != null) toggle.SetIsOnWithoutNotify(selected);
                title.gameObject.SetActive(selected);
            }
        }
        var portraits = 兵营类型 == 4 ? 全局变量.骑兵营头像资源表 : 兵营类型 == 5 ? 全局变量.步兵营头像资源表 : 兵营类型 == 6 ? 全局变量.弓兵营头像资源表 : 全局变量.战车营头像资源表;
        int avatar = b.获取建筑头像索引();
        if (头像对象 != null && avatar < portraits.Length) 头像对象.sprite = portraits[avatar];
        名字等级对象.text = b.获取建筑等级文本();
        if (升级费用文字 == null)
        {
            var upgradeLabel = transform.Find("兵营信息布局/建筑名称");
            if (upgradeLabel != null) 升级费用文字 = upgradeLabel.GetComponent<Text>();
        }
        if (升级费用文字 != null)
        {
            string error = FiefActions.UpgradeError(f, 第几个建筑);
            升级费用文字.text = error == null ? "铜" + FiefActions.BuildingCost(b.等级, 2).ToString("0") + "/粮" + FiefActions.BuildingCost(b.等级, 4).ToString("0") : b.等级 >= 10 ? "兵营已满级" : "请先升级大厅";
            布局升级费用(升级费用文字);
        }
        for (int i = 0; i < 4 && i < 兵种列表对象.transform.childCount; i++)
        {
            int id = (兵营类型 - 3) * 100 + i + 1;
            var u = 全局兵种库.查询指定ID的数据(id); var row = 兵种列表对象.transform.GetChild(i);
            row.gameObject.SetActive(u != null); if (u == null || row.childCount < 17) continue;
            int sprite = 全局兵种库.查询指定兵种的图片(u.名称);
            if (sprite >= 0 && sprite < 全局变量.所有兵种图片资源表.Length) row.GetChild(1).GetComponent<Image>().sprite = 全局变量.所有兵种图片资源表[sprite];
            row.GetChild(2).GetComponent<Text>().text = u.名称;
            var count = row.GetChild(2).GetChild(0); var locked = row.GetChild(2).GetChild(1);
            bool available = b.等级 >= i * 3 + 1;
            count.gameObject.SetActive(available); locked.gameObject.SetActive(!available);
            count.GetComponent<Text>().text = "(闲兵" + f.闲兵信息表.Where(x => x.ID == id).Sum(x => x.数量).ToString("0") + ")";
            var lockText = locked.GetComponent<Text>(); if (lockText != null) lockText.text = "需兵营" + (i * 3 + 1) + "级";
            row.GetChild(15).gameObject.SetActive(!available);
            if (!available) row.GetChild(16).gameObject.SetActive(false);
            row.GetChild(4).GetComponent<Text>().text = u.攻击力.ToString();
            row.GetChild(6).GetComponent<Text>().text = u.防御力.ToString();
            row.GetChild(8).GetComponent<Text>().text = u.生命值.ToString();
            row.GetChild(10).GetComponent<Text>().text = u.攻击速度.ToString();
            row.GetChild(12).GetComponent<Text>().text = u.移动速度.ToString();
            row.GetChild(14).GetComponent<Text>().text = u.占用人口.ToString();
        }
        调整数值列();
    }

    private void 布局升级费用(Text text)
    {
        var native = transform.parent == null ? null : transform.parent.Find("建造建筑界面UI/建筑信息布局/需要铜钱显示 (1)");
        var source = native == null ? null : native.GetComponent<Text>();
        if (source != null)
        {
            text.font = source.font; text.fontSize = source.fontSize; text.color = source.color;
            text.fontStyle = source.fontStyle; text.material = source.material;
        }
        text.resizeTextForBestFit = false;
        CityMapPresentation.SingleLineField(text);
        if (升级费用布局 != null) return;
        var parent = text.transform.parent as RectTransform;
        var upgrade = parent == null ? null : parent.Find("升级建筑") as RectTransform;
        if (parent == null || upgrade == null) return;
        Rect original = CityMapPresentation.Bounds(text.rectTransform, parent);
        // 原费用槽20.5高，含斜杠的18号字实际高22；在原头部留白中补足单行高度。
        float priceHeight = text.cachedTextGeneratorForLayout.GetPreferredHeight("铜0123456789/粮",
            text.GetGenerationSettings(Vector2.zero)) / text.pixelsPerUnit;
        float height = Mathf.Max(original.height, Mathf.Ceil(text.preferredHeight), Mathf.Ceil(priceHeight));
        original = new Rect(original.xMin, original.center.y - height * .5f, original.width, height);
        Rect button = CityMapPresentation.Bounds(upgrade, parent);
        var demolish = parent.Find("拆除建筑") as RectTransform;
        if (demolish != null) button.xMin = Mathf.Min(button.xMin, CityMapPresentation.Bounds(demolish, parent).xMin);
        // 原费用 Text 留在原路径。布局只管理这个槽，头像、名称和两个原按钮忽略此布局。
        foreach (Transform child in parent)
        {
            if (child == text.transform) continue;
            var element = child.GetComponent<LayoutElement>() ?? child.gameObject.AddComponent<LayoutElement>();
            element.ignoreLayout = true;
        }
        var slot = text.GetComponent<LayoutElement>() ?? text.gameObject.AddComponent<LayoutElement>();
        slot.minHeight = slot.preferredHeight = original.height;
        升级费用布局 = parent.gameObject.AddComponent<HorizontalLayoutGroup>();
        升级费用布局.childAlignment = TextAnchor.MiddleLeft;
        升级费用布局.padding = new RectOffset(Mathf.CeilToInt(original.xMin - parent.rect.xMin),
            Mathf.CeilToInt(parent.rect.xMax - button.xMin + text.fontSize * .5f),
            Mathf.CeilToInt(parent.rect.yMax - original.yMax), Mathf.CeilToInt(original.yMin - parent.rect.yMin));
        升级费用布局.childControlWidth = 升级费用布局.childControlHeight = true;
        升级费用布局.childForceExpandWidth = true; 升级费用布局.childForceExpandHeight = false;
    }

    private void 调整数值列()
    {
        var rows = Enumerable.Range(0, Mathf.Min(4, 兵种列表对象.transform.childCount))
            .Select(i => 兵种列表对象.transform.GetChild(i)).Where(r => r.gameObject.activeSelf && r.childCount >= 17).ToArray();
        // 六项属性共用原来的三列图标和数字。按实际字体的最长数值分配列宽，保持原左边缘和上下位置。
        foreach (var row in rows)
            foreach (int index in new[] { 4, 6, 8, 10, 12, 14 })
            {
                var text = row.GetChild(index).GetComponent<Text>();
                text.horizontalOverflow = HorizontalWrapMode.Overflow;
                text.verticalOverflow = VerticalWrapMode.Overflow;
            }
        for (int column = 0; column < 3; column++)
        {
            int[] fields = { 4 + column * 2, 10 + column * 2 };
            float width = 0;
            foreach (var row in rows)
                foreach (int index in fields)
                {
                    var text = row.GetChild(index).GetComponent<Text>();
                    width = Mathf.Max(width, text.rectTransform.rect.width, text.preferredWidth);
                }
            float shift = 0;
            foreach (var row in rows)
                foreach (int index in fields)
                {
                    var rect = (RectTransform)row.GetChild(index);
                    var left = rect.localPosition.x + rect.rect.xMin;
                    rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
                    rect.localPosition += new Vector3(left - rect.localPosition.x - rect.rect.xMin, 0, 0);
                    if (column == 2) continue;
                    var icon = (RectTransform)row.GetChild(index - 1);
                    var nextIcon = (RectTransform)row.GetChild(index + 1);
                    float gap = Mathf.Max(0, left - icon.localPosition.x - icon.rect.xMax);
                    shift = Mathf.Max(shift, left + width + gap - nextIcon.localPosition.x - nextIcon.rect.xMin);
                }
            // 后续两行整列共同让位，不单独挪某一个数字，也不改字号或行距。
            if (shift <= 0) continue;
            foreach (var row in rows)
                for (int following = column + 1; following < 3; following++)
                    foreach (int index in new[] { 3 + following * 2, 4 + following * 2, 9 + following * 2, 10 + following * 2 })
                        row.GetChild(index).localPosition += new Vector3(shift, 0, 0);
        }
    }
    public void 招募选中兵种()
    {
        for (int i = 0; i < 4 && i < 兵种列表对象.transform.childCount; i++)
        {
            var row = 兵种列表对象.transform.GetChild(i);
            if (!row.gameObject.activeSelf || row.childCount < 17 || !row.GetChild(16).gameObject.activeSelf) continue;
            int id = (兵营类型 - 3) * 100 + i + 1;
            string error = FiefActions.RecruitmentError(第几个玩家, 第几个封地, 第几个建筑, id);
            if (error != null) { 全局变量.提示类.显示信息(error); return; }
            if (调整招募数量脚本对象 == null) return;
            调整招募数量脚本对象.第几个封地 = 第几个封地; 调整招募数量脚本对象.第几个玩家 = 第几个玩家;
            调整招募数量脚本对象.第几个建筑 = 第几个建筑; 调整招募数量脚本对象.兵营脚本对象 = this;
            调整招募数量脚本对象.调整类型 = 1; 调整招募数量脚本对象.兵种ID = id;
            调整招募数量脚本对象.显示调整界面(); 调整招募数量脚本对象.显示说明文本(); return;
        }
        全局变量.提示类.显示信息("请先选择已解锁的兵种。" );
    }
}
