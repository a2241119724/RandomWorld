namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.AI.Worker;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEngine;

    /// <summary>
    /// WorkerHomeLayout.GenerateRoomLayout 布局不变量防回归（历史 bug 密集区：
    /// 床副格碰撞 2026-08-14 / 门位堵家具封死房间 / interiorW 硬编码 7）。
    /// 反射调 internal 方法（Editor 测试程序集无 InternalsVisibleTo）；
    /// AItem 构造链纯 C#，RoomLayout.BedDef 静态初始化裸 Mono 安全。
    /// </summary>
    [TestFixture]
    public class WorkerHomeLayoutTests
    {
        private static readonly MethodInfo generateMethod = typeof(WorkerHomeLayout).GetMethod(
            "GenerateRoomLayout", BindingFlags.NonPublic | BindingFlags.Static);

        private static RoomLayout InvokeGenerate(int width, int height, int doorSide, int doorIndex)
        {
            return (RoomLayout)generateMethod.Invoke(null, new object[] { width, height, doorSide, doorIndex });
        }

        /// <summary>该门位在边上的合法索引上限（与 GenerateRandomRoomParams 同式：非角位置数 - 1）。</summary>
        private static int MaxDoorIndex(int width, int height, int doorSide)
        {
            int sideLen = (doorSide == 0 || doorSide == 1) ? height : width;
            int max = sideLen - 3;
            return max < 0 ? 0 : max;
        }

        private static List<Vector3Int> FurnitureCells(RoomLayout layout)
        {
            List<Vector3Int> cells = new List<Vector3Int>(6);
            cells.Add(layout.BedOffset);
            cells.Add(layout.BedSecondOffset);
            cells.AddRange(layout.StorageOffsets);
            return cells;
        }

        /// <summary>复刻 private IsDoorEntryBlockedByFurniture 判定：进门第一格是否落在家具占位。</summary>
        private static bool IsDoorEntryBlocked(RoomLayout layout, int doorSide)
        {
            Vector3Int entry = doorSide switch
            {
                0 => layout.DoorOffset + new Vector3Int(1, 0, 0),
                1 => layout.DoorOffset + new Vector3Int(-1, 0, 0),
                2 => layout.DoorOffset + new Vector3Int(0, -1, 0),
                3 => layout.DoorOffset + new Vector3Int(0, 1, 0),
                _ => layout.DoorOffset,
            };
            return FurnitureCells(layout).Contains(entry);
        }

        /// <summary>
        /// 全组合不变量：5/7 × 5/7 × 4 边 × 全部合法门位，逐组合验证——
        /// 墙数 = 周长格数 - 1（门）；门在墙上非角；门不在墙列表；
        /// 家具 6 格互异；家具全在房间内部；门格不是家具格。
        /// </summary>
        [Test]
        public void GenerateRoomLayout_AllCombinations_SatisfyInvariants()
        {
            int[] sizes = { 5, 7 };
            int checkedCombinations = 0;
            foreach (int w in sizes)
            {
                foreach (int h in sizes)
                {
                    int hw = (w - 1) / 2;
                    int hh = (h - 1) / 2;
                    foreach (int side in new[] { 0, 1, 2, 3 })
                    {
                        for (int idx = 0; idx <= MaxDoorIndex(w, h, side); idx++)
                        {
                            RoomLayout layout = InvokeGenerate(w, h, side, idx);
                            Assert.IsNotNull(layout, $"{w}x{h} side={side} idx={idx} 应有布局");
                            checkedCombinations++;

                            // 墙数 = 周长格数 2(w+h)-4，减 1 个门
                            int expectedWalls = 2 * (w + h) - 4 - 1;
                            Assert.AreEqual(expectedWalls, layout.WallOffsets.Count,
                                $"{w}x{h} side={side} idx={idx} 墙数应为 {expectedWalls}");
                            Assert.AreEqual(layout.WallOffsets.Count, layout.WallDirections.Count, "墙方向数与墙数一致");

                            // 门格：在对应边、非角、不在墙列表
                            Vector3Int door = layout.DoorOffset;
                            bool onEdgeNonCorner = side switch
                            {
                                0 => door.x == -hw && door.y < hh && door.y > -hh,
                                1 => door.x == hw && door.y < hh && door.y > -hh,
                                2 => door.y == hh && door.x < hw && door.x > -hw,
                                3 => door.y == -hh && door.x < hw && door.x > -hw,
                                _ => false,
                            };
                            Assert.IsTrue(onEdgeNonCorner, $"{w}x{h} side={side} idx={idx} 门应在边 {side} 非角位置");
                            Assert.IsFalse(layout.WallOffsets.Contains(door), "门格不应出现在墙列表");

                            // 家具 6 格互异（床主/副 + 4 仓库）
                            List<Vector3Int> furniture = FurnitureCells(layout);
                            Assert.AreEqual(6, furniture.Count, "家具占位 = 床主格+床副格+4 仓库");
                            Assert.AreEqual(6, new HashSet<Vector3Int>(furniture).Count,
                                $"{w}x{h} side={side} idx={idx} 家具占位互不重叠");

                            // 家具全在内部（|x| ≤ hw-1 且 |y| ≤ hh-1）
                            foreach (Vector3Int cell in furniture)
                            {
                                Assert.LessOrEqual(Mathf.Abs(cell.x), hw - 1,
                                    $"家具格 {cell} 越过 x 内边界");
                                Assert.LessOrEqual(Mathf.Abs(cell.y), hh - 1,
                                    $"家具格 {cell} 越过 y 内边界");
                            }

                            // 门格不是家具格（门与家具直接同格是布局硬伤）
                            Assert.IsFalse(furniture.Contains(door), "门格不应与家具重叠");
                        }
                    }
                }
            }

            Assert.Greater(checkedCombinations, 0, "至少应枚举到组合");
        }

        /// <summary>
        /// 7×7 契约：任一门位进门第一格都与家具块相距 ≥1 格（永不堵门）——
        /// GenerateRandomRoomParams 的兜底组合依赖此性质。
        /// </summary>
        [Test]
        public void GenerateRoomLayout_7x7_AnyDoorIndex_NeverBlocked()
        {
            foreach (int side in new[] { 0, 1, 2, 3 })
            {
                for (int idx = 0; idx <= MaxDoorIndex(7, 7, side); idx++)
                {
                    RoomLayout layout = InvokeGenerate(7, 7, side, idx);
                    Assert.IsFalse(IsDoorEntryBlocked(layout, side),
                        $"7x7 side={side} idx={idx} 进门第一格不应被家具堵（兜底组合契约）");
                }
            }
        }

        /// <summary>
        /// 5×5 契约：side 1/2/3 每边至少一个门位进门格不堵（GenerateRandomRoomParams
        /// 的 validIndices 非空前提）。side=0（左墙）是已知例外——家具靠右贴墙时
        /// 占满 5×5 内部宽度，全部进门格撞家具左列，产品靠换参重试规避
        /// （GenerateRandomRoomParams 注释记录的 2026-08 修复），此处锁定该现状防漂移。
        /// </summary>
        [Test]
        public void GenerateRoomLayout_5x5_Side123_HasUsableDoorIndex_Side0_AllBlocked()
        {
            foreach (int side in new[] { 1, 2, 3 })
            {
                bool anyUsable = false;
                for (int idx = 0; idx <= MaxDoorIndex(5, 5, side); idx++)
                {
                    if (!IsDoorEntryBlocked(InvokeGenerate(5, 5, side, idx), side))
                    {
                        anyUsable = true;
                        break;
                    }
                }

                Assert.IsTrue(anyUsable, $"5x5 side={side} 至少应有一个可用门位");
            }

            for (int idx = 0; idx <= MaxDoorIndex(5, 5, 0); idx++)
            {
                Assert.IsTrue(IsDoorEntryBlocked(InvokeGenerate(5, 5, 0, idx), 0),
                    $"5x5 side=0 idx={idx} 现状应为全堵（家具占满内部宽度）；若此处失败说明布局已变，请同步更新 GenerateRandomRoomParams 重试逻辑评估");
            }
        }

        /// <summary>
        /// 缓存契约：同参数两次 GetRoomLayout 返回同一实例（发布后不可变共享，
        /// 防回归为每次分配新 RoomLayout 的决策级 GC 压力）。参数预填避开 Random。
        /// </summary>
        [Test]
        public void GetRoomLayout_SameParams_ReturnsCachedInstance()
        {
            LAB2D.Character.Worker.AWorker.WorkerData wd = new LAB2D.Character.Worker.AWorker.WorkerData
            {
                HomeRoomWidth = 7,
                HomeRoomHeight = 7,
                HomeDoorSide = 1,
                HomeDoorIndex = 0,
            };

            RoomLayout first = WorkerHomeLayout.GetRoomLayout(wd);
            RoomLayout second = WorkerHomeLayout.GetRoomLayout(wd);
            Assert.AreSame(first, second, "同参数布局应命中缓存共享实例");
        }
    }
}
