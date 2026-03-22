namespace Utils.nAlpha
{
    internal class VertexCounter
    {
        private readonly Dictionary<int, int> vertexCounts = new();

        public void IncreaseForIndex(int index)
        {
            if (!vertexCounts.ContainsKey(index)) vertexCounts.Add(index, 0);
            vertexCounts[index]++;
        }

        public int[] GetIndicesByCount(int count)
        {
            return vertexCounts.Where(kvp => kvp.Value == count).Select(kvp => kvp.Key).ToArray();
        }
    }
}