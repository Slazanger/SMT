using System.Collections.Generic;
using System.Linq;

namespace nAlpha
{
    internal class VertexCounter
    {
        Dictionary<int, int> vertexCounts = new Dictionary<int, int>();

        public void IncreaseForIndex(int index)
        {
            if (!vertexCounts.ContainsKey(index))
            {
                vertexCounts.Add(index, 0);
            }
            vertexCounts[index]++;
        }

        public int[] GetIndicesByCount(int count)
        {
            return vertexCounts.Where(kvp => kvp.Value == count).Select(kvp => kvp.Key).ToArray();
        }
    }
}