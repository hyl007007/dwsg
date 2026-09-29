using System;
using System.Collections.Generic;

namespace Dwsg.Shared.Economy
{
    public static class ItemStackRules
    {
        public static int MinimumIndex<T>(IList<T> items, string name, Func<T, string> getName, Func<T, double> getQuantity)
        {
            double minimum = 2000.0;
            int result = -1;
            for (int i = 0; i < items.Count; i++)
                if (getName(items[i]) == name && getQuantity(items[i]) < minimum)
                {
                    minimum = getQuantity(items[i]);
                    result = i;
                }
            return result;
        }

        public static int RequiredSlots<T>(IList<T> items, string name, int quantity, Func<T, string> getName, Func<T, double> getQuantity)
        {
            if (items == null) return 0;
            int index = MinimumIndex(items, name, getName, getQuantity);
            return index == -1 || getQuantity(items[index]) + quantity > 999.0 ? 1 : 0;
        }

        public static void Add<T>(IList<T> items, string name, int quantity, Func<T, string> getName,
            Func<T, double> getQuantity, Action<T, double> setQuantity, Func<string, double, T> create)
        {
            if (items == null) return;
            int index = MinimumIndex(items, name, getName, getQuantity);
            if (index == -1) items.Add(create(name, quantity));
            else if (getQuantity(items[index]) + quantity > 999.0)
            {
                double remaining = 999.0 - getQuantity(items[index]);
                items.Add(create(name, quantity - remaining));
                setQuantity(items[index], 999.0);
            }
            else setQuantity(items[index], getQuantity(items[index]) + quantity);
        }

        public static bool Subtract<T>(IList<T> items, string name, int quantity, Func<T, string> getName,
            Func<T, double> getQuantity, Action<T, double> setQuantity)
        {
            if (items == null || quantity <= 0) return false;
            double available = 0;
            foreach (T item in items) if (getName(item) == name) available += getQuantity(item);
            if (available < quantity) return false;
            int remaining = quantity;
            for (int i = items.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (getName(items[i]) != name) continue;
                int removed = Math.Min(remaining, (int)getQuantity(items[i]));
                setQuantity(items[i], getQuantity(items[i]) - removed);
                remaining -= removed;
                if (getQuantity(items[i]) <= 0) items.RemoveAt(i);
            }
            return remaining == 0;
        }
    }
}
