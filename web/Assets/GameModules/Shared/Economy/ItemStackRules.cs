using System;
using System.Collections.Generic;

namespace Dwsg.Shared.Economy
{
    public static class ItemStackRules
    {
        public static int MinimumIndex<T>(IList<T> items, string name, Func<T, string> getName, Func<T, double> getQuantity)
        {
            if (items == null) return -1;
            double minimum = double.MaxValue;
            int result = -1;
            for (int i = 0; i < items.Count; i++)
                if (!ReferenceEquals(items[i], null) && getName(items[i]) == name && getQuantity(items[i]) > 0 && getQuantity(items[i]) < minimum)
                {
                    minimum = getQuantity(items[i]);
                    result = i;
                }
            return result;
        }

        public static int RequiredSlots<T>(IList<T> items, string name, int quantity, Func<T, string> getName, Func<T, double> getQuantity)
        {
            if (items == null || quantity <= 0) return -1;
            long remaining = quantity;
            foreach (T item in items)
            {
                if (ReferenceEquals(item, null)) return -1;
                if (getName(item) != name) continue;
                double count = getQuantity(item);
                if (double.IsNaN(count) || double.IsInfinity(count) || count < 1 || count > 999 || count != Math.Floor(count)) return -1;
                remaining = Math.Max(0, remaining - (999 - (int)count));
            }
            return (int)((remaining + 998) / 999);
        }

        public static void Add<T>(IList<T> items, string name, int quantity, Func<T, string> getName,
            Func<T, double> getQuantity, Action<T, double> setQuantity, Func<string, double, T> create)
        {
            if (RequiredSlots(items, name, quantity, getName, getQuantity) < 0) return;
            foreach (T item in items)
            {
                if (getName(item) != name) continue;
                int added = Math.Min(quantity, 999 - (int)getQuantity(item));
                setQuantity(item, getQuantity(item) + added);
                quantity -= added;
                if (quantity == 0) return;
            }
            while (quantity > 0)
            {
                int added = Math.Min(quantity, 999);
                items.Add(create(name, added));
                quantity -= added;
            }
        }

        public static bool Subtract<T>(IList<T> items, string name, int quantity, Func<T, string> getName,
            Func<T, double> getQuantity, Action<T, double> setQuantity)
        {
            if (items == null || quantity <= 0) return false;
            double available = 0;
            foreach (T item in items)
            {
                if (ReferenceEquals(item, null)) return false;
                if (getName(item) != name) continue;
                double count = getQuantity(item);
                if (double.IsNaN(count) || double.IsInfinity(count) || count < 1 || count > 999 || count != Math.Floor(count)) return false;
                available += count;
            }
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

        public static bool ConsumeOne<T>(IList<T> items, string name, Func<T, string> getName,
            Func<T, double> getQuantity, Action<T, double> setQuantity)
        {
            if (items == null) return false;
            int index = MinimumIndex(items, name, getName, getQuantity);
            if (index == -1 || getQuantity(items[index]) <= 0) return false;
            setQuantity(items[index], getQuantity(items[index]) - 1);
            if (getQuantity(items[index]) <= 0) items.RemoveAt(index);
            return true;
        }
    }
}
