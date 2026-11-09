using EVEDataUtils;
using SMT.EVEData;
using Xunit;
using EveSystem = SMT.EVEData.System;

namespace EVEData.Tests
{
    /// <summary>
    /// The systems from the Systems.dat that ships with SMT, with Navigation set up from them (no jump bridges).
    /// </summary>
    public sealed class NavigationFixture
    {
        public NavigationFixture()
        {
            Systems = Serialization.DeserializeFromDisk<List<EveSystem>>(Path.Combine(AppContext.BaseDirectory, "data", "Systems.dat"));
            Navigation.InitNavigation(Systems, new List<JumpBridge>());
        }

        public List<EveSystem> Systems { get; }
    }

    /// <summary>
    /// Navigation's lookups against the same answers worked out the slow, obvious way from the shipped map data.
    /// </summary>
    public sealed class NavigationTests : IClassFixture<NavigationFixture>
    {
        private readonly List<EveSystem> systems;

        public NavigationTests(NavigationFixture fixture)
        {
            systems = fixture.Systems;
        }

        // Capital routes are planned over this table. Navigation builds it through a grid of cubes; a system missed
        // at a cube's edge would quietly make some routes longer or impossible.
        [Fact]
        public void EverySystemLinksToExactlyTheSystemsACapitalCanJumpTo()
        {
            // the rule: any other system under 10 light years away that isn't highsec, in Pochven or Zarzakh;
            // here every system is measured against every other
            const double maxRangeSquared = 10.0 * 10.0;
            int count = systems.Count;
            double[] x = systems.Select(s => (double)s.ActualX).ToArray();
            double[] y = systems.Select(s => (double)s.ActualY).ToArray();
            double[] z = systems.Select(s => (double)s.ActualZ).ToArray();
            bool[] canJumpInto = systems.Select(s => s.TrueSec <= 0.45 && s.Region != "Pochven" && s.Name != "Zarzakh").ToArray();

            long links = 0;
            List<string> wrong = new List<string>();
            for(int from = 0; from < count; from++)
            {
                Dictionary<string, double> jumpable = Navigation.GetJumpableSystems(systems[from].Name);
                int expected = 0;
                bool matches = true;
                for(int to = 0; to < count; to++)
                {
                    if(!canJumpInto[to])
                    {
                        continue;
                    }

                    double dx = x[from] - x[to];
                    double dy = y[from] - y[to];
                    double dz = z[from] - z[to];
                    double distanceSquared = (dx * dx) + (dy * dy) + (dz * dz);
                    if(distanceSquared > 0 && distanceSquared < maxRangeSquared)
                    {
                        expected++;
                        matches &= jumpable.TryGetValue(systems[to].Name, out double range) && Math.Abs(range - Math.Sqrt(distanceSquared)) < 1e-9;
                    }
                }

                links += expected;
                if(!matches || jumpable.Count != expected)
                {
                    wrong.Add(systems[from].Name);
                }
            }

            Assert.Empty(wrong);
            Assert.True(links > 0, "the map data has no systems within jump range of each other");
        }

        // danger zones (1 to 6 jumps around each character) and the storm areas
        [Fact]
        public void SystemsWithinJumpsAreTheOnesThatManyGatesAway()
        {
            Dictionary<string, List<string>> gates = systems.ToDictionary(s => s.Name, s => s.Jumps);

            // every 20th system: a few hundred starts spread over the whole map
            for(int jumps = 1; jumps <= 6; jumps++)
            {
                for(int i = 0; i < systems.Count; i += 20)
                {
                    string start = systems[i].Name;
                    HashSet<string> expected = WithinGates(gates, start, jumps);

                    List<string> found = Navigation.GetSystemsXJumpsFrom(new List<string>(), start, jumps);

                    Assert.True(expected.SetEquals(found), $"{jumps} jumps from {start}: expected {expected.Count} systems, found {found.Count}");
                    Assert.True(found.Count == expected.Count, $"{jumps} jumps from {start}: a system is listed twice");
                }
            }
        }

        // a character in a wormhole is in a system the map doesn't have; a danger zone can be switched off (0 jumps)
        [Fact]
        public void NothingIsWithinJumpsOfASystemNotOnTheMapOrWithinNoJumps()
        {
            Assert.Empty(Navigation.GetSystemsXJumpsFrom(new List<string>(), "J123456", 3));
            Assert.Empty(Navigation.GetSystemsXJumpsFrom(new List<string>(), "Jita", 0));
        }

        /// <summary>
        /// Every system at most that many gates from start, start included, by walking outwards one gate at a time.
        /// </summary>
        private static HashSet<string> WithinGates(Dictionary<string, List<string>> gates, string start, int jumps)
        {
            Dictionary<string, int> distance = new Dictionary<string, int> { [start] = 0 };
            Queue<string> queue = new Queue<string>();
            queue.Enqueue(start);
            while(queue.Count > 0)
            {
                string system = queue.Dequeue();
                if(distance[system] == jumps || !gates.TryGetValue(system, out List<string> neighbours))
                {
                    continue;
                }

                foreach(string neighbour in neighbours)
                {
                    if(distance.TryAdd(neighbour, distance[system] + 1))
                    {
                        queue.Enqueue(neighbour);
                    }
                }
            }

            return new HashSet<string>(distance.Keys);
        }
    }
}
