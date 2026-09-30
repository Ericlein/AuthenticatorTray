namespace AuthenticatorTray
{
    public static class ColumnLayout
    {
        // Greedy top-to-bottom fill; an item marked keepWithNext never ends a column on its own
        public static int[] Pack(IReadOnlyList<int> heights, IReadOnlyList<bool> keepWithNext, int maxHeight)
        {
            var columns = new int[heights.Count];
            int column = 0, used = 0;
            for (int i = 0; i < heights.Count; i++)
            {
                int needed = heights[i] + (keepWithNext[i] && i + 1 < heights.Count ? heights[i + 1] : 0);
                if (used > 0 && used + needed > maxHeight)
                {
                    column++;
                    used = 0;
                }
                columns[i] = column;
                used += heights[i];
            }
            return columns;
        }
        // Fewest columns that fit maxHeight (capped at maxColumns), then the shortest even split across them
        public static int[] Balance(IReadOnlyList<int> heights, IReadOnlyList<bool> keepWithNext, int maxHeight, int maxColumns)
        {
            if (heights.Count == 0) return Array.Empty<int>();
            int columnCount = Math.Min(Pack(heights, keepWithNext, maxHeight).Max() + 1, Math.Max(1, maxColumns));
            int total = heights.Sum();
            int low = Math.Max(heights.Max(), (total + columnCount - 1) / columnCount);
            int high = Math.Max(low, total);
            while (low < high)
            {
                int mid = (low + high) / 2;
                if (Pack(heights, keepWithNext, mid).Max() + 1 <= columnCount) high = mid;
                else low = mid + 1;
            }
            return Pack(heights, keepWithNext, low);
        }
    }
}
