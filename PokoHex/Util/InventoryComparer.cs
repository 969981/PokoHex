using System;
using System.Collections;
using System.Windows.Forms;
using PokoHex.Core;

namespace PokoHex;

internal sealed class InventoryComparer(Item[] items, int column, bool descending) : IComparer
{
    public int Compare(object? x, object? y)
    {
        var left = (DataGridViewRow)x!;
        var right = (DataGridViewRow)y!;
        var a = (int)left.Tag!;
        var b = (int)right.Tag!;
        if (column != 0)
        {
            var empty = items[a].IsEmpty.CompareTo(items[b].IsEmpty);
            if (empty != 0)
                return empty;
            if (items[a].IsEmpty)
                return a.CompareTo(b);
        }
        var result = column switch
        {
            2 => StringComparer.CurrentCultureIgnoreCase.Compare(left.Cells[2].Value as string, right.Cells[2].Value as string),
            3 => items[a].Count.CompareTo(items[b].Count),
            _ => a.CompareTo(b),
        };
        return result == 0 ? a.CompareTo(b) : descending ? -result : result;
    }
}
