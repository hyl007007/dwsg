using System;
using System.Globalization;
using System.Linq;
using 玩家数据结构;
using UnityEngine;
using UnityEngine.UI;

namespace Dwsg.Window1
{
    // 商城沿用既有价格、限购和背包容量，在任何扣款前完成数量与空间检查。
    public static class InventoryTrade
    {
        private static bool Number(double value) { return !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0; }
        private static bool Ready(玩家数据 player, 商品属性类 goods, int count, out string error)
        {
            error = "当前商品或君主数据不可用";
            if (player == null || player.基础信息 == null || player.财产信息 == null || player.背包道具列表 == null || player.背包装备列表 == null ||
                goods == null || 全局道具库.获取指定名字的道具(goods.道具名) == null) return false;
            if (count < 1 || count > 100) { error = "请选择 1 到 100 个整数数量"; return false; }
            var bag = player.背包道具列表; var gear = player.背包装备列表;
            if (new[] { bag.宝物道具列表, bag.加速道具列表, bag.生产道具列表, bag.宝箱道具列表, bag.强化道具列表, bag.任务道具列表 }.Any(l => l == null) ||
                gear.武器装备列表 == null || gear.头盔装备列表 == null || gear.铠甲装备列表 == null || gear.坐骑装备列表 == null) return false;
            return true;
        }
        private static bool Price(商品属性类 goods, bool silver, int count, out double amount, out string error)
        {
            double price = silver ? goods.白银售价 : goods.黄金售价;
            amount = price * count; error = "该商品不支持此货币";
            return price > 0 && Number(price) && Number(amount);
        }
        public static bool CanBuy(玩家数据 player, 商品属性类 goods, int count, bool silver, out string error)
        {
            if (!Ready(player, goods, count, out error)) return false;
            var definition = 全局道具库.获取指定名字的道具(goods.道具名);
            if (definition.分类 == "宝箱" && !全局道具库.可在背包开启(goods.道具名))
            { error = definition.说明; return false; }
            double amount;
            if (!Price(goods, silver, count, out amount, out error)) return false;
            if (goods.限购数量 < -1 || (goods.限购数量 != -1 && goods.限购数量 < count)) { error = "库存不足，无法购买所选数量"; return false; }
            double balance = silver ? player.财产信息.白银 : player.财产信息.黄金;
            if (!Number(balance) || balance < amount) { error = "余额不足"; return false; }
            int slots = player.背包道具列表.所需新增格数(goods.道具名, count);
            if (slots < 0) { error = "道具堆叠数据异常，无法购买"; return false; }
            double capacity = player.基础信息.背包容量上限;
            if (!Number(capacity) || capacity != Math.Floor(capacity) || player.获取背包物品数量() + slots > capacity)
            { error = "背包空间不足，请先整理背包"; return false; }
            error = null; return true;
        }
        public static bool Buy(玩家数据 player, 商品属性类 goods, int count, bool silver, out string error)
        {
            if (!CanBuy(player, goods, count, silver, out error)) return false;
            double amount = (silver ? goods.白银售价 : goods.黄金售价) * count;
            player.背包道具列表.添加道具(goods.道具名, count);
            if (silver) player.财产信息.白银 -= amount; else player.财产信息.黄金 -= amount;
            if (goods.限购数量 != -1) goods.限购数量 -= count;
            return true;
        }
        public static bool CanSell(玩家数据 player, 商品属性类 goods, int count, bool silver, out string error)
        {
            if (!Ready(player, goods, count, out error)) return false;
            double amount;
            if (!Price(goods, silver, count, out amount, out error)) return false;
            double balance = silver ? player.财产信息.白银 : player.财产信息.黄金;
            if (!Number(balance) || !Number(balance + amount)) { error = "资产数值异常，无法卖出"; return false; }
            if (player.背包道具列表.所需新增格数(goods.道具名, 1) < 0) { error = "道具堆叠数据异常，无法卖出"; return false; }
            if (player.背包道具列表.获取指定道具数量(goods.道具名) < count) { error = "持有数量不足"; return false; }
            error = null; return true;
        }
        public static bool Sell(玩家数据 player, 商品属性类 goods, int count, bool silver, out string error)
        {
            if (!CanSell(player, goods, count, silver, out error)) return false;
            if (!player.背包道具列表.扣除道具(goods.道具名, count)) { error = "持有数量不足"; return false; }
            double amount = (silver ? goods.白银售价 : goods.黄金售价) * count;
            if (silver) player.财产信息.白银 += amount; else player.财产信息.黄金 += amount;
            return true;
        }
    }

    public static class InventoryUi
    {
        // 只缩略原格子标签；完整名称和点击后的详情继续来自业务对象。
        public static void 显示格子名称(Text text)
        {
            if (text == null) return;
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            string fullText = (text.text ?? string.Empty).Replace('\r', ' ').Replace('\n', ' ');
            text.text = fullText;
            float width = Mathf.Max(0, text.rectTransform.rect.width - 2);
            if (text.preferredWidth <= width) return;
            const string ellipsis = "…";
            text.text = ellipsis;
            if (text.preferredWidth > width) { text.text = string.Empty; return; }
            var boundaries = StringInfo.ParseCombiningCharacters(fullText);
            int low = 0, high = boundaries.Length - 1;
            while (low < high)
            {
                int count = (low + high + 1) / 2;
                text.text = fullText.Substring(0, boundaries[count]) + ellipsis;
                if (text.preferredWidth <= width) low = count;
                else high = count - 1;
            }
            text.text = fullText.Substring(0, boundaries[low]) + ellipsis;
        }

        public static Text Empty(Text existing, RectTransform list, Text fontSource, string message)
        {
            if (existing == null)
            {
                var scroll = list.GetComponentInParent<ScrollRect>(true);
                var parent = scroll != null && scroll.viewport != null ? scroll.viewport : list.parent;
                existing = Window1Style.Text(parent, "窗口1_列表空态", "", fontSource.font, 15, Window1Style.Ink, TextAnchor.MiddleCenter);
                var rt = existing.rectTransform;
                if (parent == list.parent)
                { rt.anchorMin = list.anchorMin; rt.anchorMax = list.anchorMax; rt.pivot = list.pivot; rt.anchoredPosition = list.anchoredPosition; rt.sizeDelta = list.sizeDelta; }
                else Window1Style.Anchors(rt, Vector2.zero, Vector2.one);
            }
            existing.text = message; existing.gameObject.SetActive(!string.IsNullOrEmpty(message)); return existing;
        }
        public static void 调整长文(Text text, bool reset)
        {
            if (text == null) return;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Overflow;
            var scroll = text.GetComponentInParent<ScrollRect>(true);
            if (scroll == null || scroll.content == null) return;
            var fitter = text.GetComponent<ContentSizeFitter>() ?? text.gameObject.AddComponent<ContentSizeFitter>();
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained; fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            if (scroll.content != text.rectTransform)
            {
                // 购买页的原内容包含头像、售价与说明，保留原坐标，向下扩展以容纳全文。
                var content = scroll.content; content.pivot = new Vector2(content.pivot.x, 1);
                content.anchoredPosition = new Vector2(content.anchoredPosition.x, reset ? 0 : content.anchoredPosition.y);
                float minimum = scroll.viewport == null ? scroll.GetComponent<RectTransform>().rect.height : scroll.viewport.rect.height;
                content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(minimum, -text.rectTransform.anchoredPosition.y + text.preferredHeight + 12));
            }
            LayoutRebuilder.MarkLayoutForRebuild(scroll.content);
            if (reset) { scroll.StopMovement(); scroll.verticalNormalizedPosition = 1; }
        }
    }
}
