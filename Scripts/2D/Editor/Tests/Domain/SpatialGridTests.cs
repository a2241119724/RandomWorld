namespace LAB2D.Editor.Tests.Domain
{
    using System.Collections.Generic;
    using LAB2D.Domain.Common;
    using NUnit.Framework;

    /// <summary>
    /// SpatialGrid 单测：暴力线性扫描做对照，验证网格查询与全量扫描结果一致。
    /// </summary>
    [TestFixture]
    public class SpatialGridTests
    {
        private const float CellSize = 8f;

        /// <summary>测试点（暴力参考侧自持）。</summary>
        private struct TestPoint
        {
            public GameVector2 Pos;
            public string Id;
        }

        private readonly List<TestPoint> points = new List<TestPoint>();

        private static float SqrDistance(GameVector2 a, GameVector2 b)
        {
            float dx = a.X - b.X;
            float dy = a.Y - b.Y;
            return (dx * dx) + (dy * dy);
        }

        /// <summary>把测试点灌进网格（每轮重建）。</summary>
        private SpatialGrid<string> RebuildGrid()
        {
            SpatialGrid<string> grid = new SpatialGrid<string>(CellSize);
            grid.BeginRebuild();
            foreach (TestPoint p in this.points)
            {
                grid.Add(p.Pos, p.Id);
            }

            return grid;
        }

        private void FillRandom(System.Random rng, int n)
        {
            this.points.Clear();
            for (int i = 0; i < n; i++)
            {
                // 含负坐标与桶边界（CellSize 整数倍）上的点
                float x = (float)(rng.NextDouble() * 40.0 - 10.0);
                float y = (float)(rng.NextDouble() * 40.0 - 10.0);
                if (i % 7 == 0)
                {
                    x = (rng.Next(-2, 4)) * CellSize;
                }

                if (i % 11 == 0)
                {
                    y = (rng.Next(-2, 4)) * CellSize;
                }

                this.points.Add(new TestPoint { Pos = new GameVector2(x, y), Id = $"p{i}" });
            }
        }

        // ---- QueryRange ----

        [Test]
        public void QueryRange_MatchesBruteForce_VariousSizes()
        {
            int[] sizes = new int[] { 0, 1, 17, 200 };
            foreach (int n in sizes)
            {
                this.FillRandom(new System.Random(20260903), n);
                SpatialGrid<string> grid = this.RebuildGrid();
                Assert.AreEqual(n, grid.Count, $"N={n} Count");

                System.Random queryRng = new System.Random(777);
                for (int q = 0; q < 20; q++)
                {
                    GameVector2 center = new GameVector2(
                        (float)(queryRng.NextDouble() * 30.0 - 5.0),
                        (float)(queryRng.NextDouble() * 30.0 - 5.0));
                    float radius = 0.5f + (float)(queryRng.NextDouble() * 11.5);

                    List<string> expected = new List<string>();
                    float radiusSq = radius * radius;
                    foreach (TestPoint p in this.points)
                    {
                        if (SqrDistance(p.Pos, center) <= radiusSq)
                        {
                            expected.Add(p.Id);
                        }
                    }

                    List<string> actual = new List<string>();
                    grid.QueryRange(center, radius, actual);

                    Assert.AreEqual(expected.Count, actual.Count, $"N={n} q={q} r={radius:F2} 数量不一致");
                    CollectionAssert.AreEquivalent(expected, actual, $"N={n} q={q} r={radius:F2} 集合不一致");
                }
            }
        }

        [Test]
        public void QueryRange_IncludesExactRadiusBoundary()
        {
            this.points.Clear();
            this.points.Add(new TestPoint { Pos = new GameVector2(8f, 0f), Id = "edge" }); // 距中心恰 = radius
            this.points.Add(new TestPoint { Pos = new GameVector2(8.01f, 0f), Id = "outside" });
            SpatialGrid<string> grid = this.RebuildGrid();

            List<string> result = new List<string>();
            grid.QueryRange(new GameVector2(0f, 0f), 8f, result);

            Assert.Contains("edge", result);
            Assert.IsFalse(result.Contains("outside"));
        }

        [Test]
        public void QueryRange_AppendsToResults_WithoutClearing()
        {
            this.points.Clear();
            this.points.Add(new TestPoint { Pos = new GameVector2(1f, 1f), Id = "a" });
            SpatialGrid<string> grid = this.RebuildGrid();

            List<string> result = new List<string> { "existing" };
            grid.QueryRange(new GameVector2(0f, 0f), 4f, result);

            Assert.AreEqual(2, result.Count);
            Assert.Contains("existing", result);
        }

        // ---- QueryNearest ----

        [Test]
        public void QueryNearest_MatchesBruteForce()
        {
            this.FillRandom(new System.Random(4242), 200);
            SpatialGrid<string> grid = this.RebuildGrid();

            System.Random queryRng = new System.Random(888);
            for (int q = 0; q < 20; q++)
            {
                GameVector2 center = new GameVector2(
                    (float)(queryRng.NextDouble() * 30.0 - 5.0),
                    (float)(queryRng.NextDouble() * 30.0 - 5.0));
                float radius = 0.5f + (float)(queryRng.NextDouble() * 11.5);

                float bestSq = float.MaxValue;
                int bestCount = 0;
                float radiusSq = radius * radius;
                foreach (TestPoint p in this.points)
                {
                    float d = SqrDistance(p.Pos, center);
                    if (d <= radiusSq)
                    {
                        bestCount++;
                        if (d < bestSq)
                        {
                            bestSq = d;
                        }
                    }
                }

                string nearest = grid.QueryNearest(center, radius, out float sqrDistance);

                if (bestCount == 0)
                {
                    Assert.IsNull(nearest, $"q={q} 无候选应返回 null");
                    Assert.AreEqual(float.MaxValue, sqrDistance);
                }
                else
                {
                    // 并列最近时允许不同实例，只比距离
                    Assert.IsNotNull(nearest, $"q={q} 有候选应返回非 null");
                    Assert.AreEqual(bestSq, sqrDistance, 0.0001f, $"q={q} 最近距离不一致");
                    TestPoint found = this.points.Find(p => p.Id == nearest);
                    Assert.AreEqual(bestSq, SqrDistance(found.Pos, center), 0.0001f, $"q={q} 返回实例的距离与声称不符");
                }
            }
        }

        // ---- 重建语义 ----

        [Test]
        public void Rebuild_SecondRound_ReplacesContents()
        {
            this.points.Clear();
            this.points.Add(new TestPoint { Pos = new GameVector2(100f, 100f), Id = "old" });
            SpatialGrid<string> grid = this.RebuildGrid();

            this.points.Clear();
            this.points.Add(new TestPoint { Pos = new GameVector2(1f, 1f), Id = "new" });
            grid.BeginRebuild();
            foreach (TestPoint p in this.points)
            {
                grid.Add(p.Pos, p.Id);
            }

            Assert.AreEqual(1, grid.Count);
            List<string> nearNew = new List<string>();
            grid.QueryRange(new GameVector2(0f, 0f), 4f, nearNew);
            Assert.Contains("new", nearNew);

            List<string> nearOld = new List<string>();
            grid.QueryRange(new GameVector2(100f, 100f), 4f, nearOld);
            Assert.AreEqual(0, nearOld.Count, "二轮重建后不应残留上轮条目");
        }

        [Test]
        public void Rebuild_Empty_ClearsEverything()
        {
            this.points.Clear();
            this.points.Add(new TestPoint { Pos = new GameVector2(0f, 0f), Id = "a" });
            SpatialGrid<string> grid = this.RebuildGrid();

            grid.BeginRebuild(); // 无 Add = 空网格

            Assert.AreEqual(0, grid.Count);
            List<string> result = new List<string>();
            grid.QueryRange(new GameVector2(0f, 0f), 16f, result);
            Assert.AreEqual(0, result.Count);
            Assert.IsNull(grid.QueryNearest(new GameVector2(0f, 0f), 16f, out _));
        }

        [Test]
        public void ConsecutiveRebuilds_NoStaleEntries()
        {
            SpatialGrid<string> grid = new SpatialGrid<string>(CellSize);

            // 多桶轮
            grid.BeginRebuild();
            for (int i = 0; i < 10; i++)
            {
                grid.Add(new GameVector2(i * 20f, i * 20f), $"multi{i}");
            }

            // 单桶轮（只覆盖原点附近）
            grid.BeginRebuild();
            grid.Add(new GameVector2(1f, 1f), "single");

            List<string> result = new List<string>();
            grid.QueryRange(new GameVector2(0f, 0f), 200f, result);
            Assert.AreEqual(1, result.Count, "单桶轮不应查到多桶轮的陈旧条目");
            Assert.Contains("single", result);

            // 再回多桶轮
            grid.BeginRebuild();
            for (int i = 0; i < 10; i++)
            {
                grid.Add(new GameVector2(i * 20f, i * 20f), $"multi{i}");
            }

            result.Clear();
            // 最远点 (180,180) 距原点 ~254.6——半径须 >254.6 才能覆盖全部（首版 200 恰排除 2 个对角远点）
            grid.QueryRange(new GameVector2(0f, 0f), 360f, result);
            Assert.AreEqual(10, result.Count, "恢复多桶轮后条目齐全");
        }

        // ---- filter ----

        [Test]
        public void Filter_ExcludesFilteredPayload()
        {
            this.points.Clear();
            this.points.Add(new TestPoint { Pos = new GameVector2(1f, 0f), Id = "near" });   // 最近但被排除
            this.points.Add(new TestPoint { Pos = new GameVector2(3f, 0f), Id = "far" });    // 次近
            SpatialGrid<string> grid = this.RebuildGrid();

            string nearest = grid.QueryNearest(
                new GameVector2(0f, 0f), 8f, out float sqrDistance, id => id != "near");
            Assert.AreEqual("far", nearest);
            Assert.AreEqual(9f, sqrDistance, 0.0001f);

            List<string> range = new List<string>();
            grid.QueryRange(new GameVector2(0f, 0f), 8f, range, id => id != "near");
            Assert.AreEqual(1, range.Count);
            Assert.AreEqual("far", range[0]);
        }

        // ---- 构造校验 ----

        [Test]
        public void CellSize_ZeroOrNegative_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SpatialGrid<string>(0f));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new SpatialGrid<string>(-8f));
        }
    }
}
