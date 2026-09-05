namespace LAB2D.Editor.Tests.Domain
{
    using System;
    using System.Collections.Concurrent;
    using System.Reflection;
    using System.Threading;
    using LAB2D.Core.Seek;
    using NUnit.Framework;

    /// <summary>
    /// WalkabilityCache 快照发布契约。全量构建路径（EnsureBuilt/UpdateCell）依赖
    /// ASeek.IsCanReach（Unity icall），裸 Mono 不可调用——此处只测 Initialize/IsWalkable/Clear
    /// 纯 C# 部分：未初始化读数兜底 false，以及「尺寸切换 + 后台并发读」全程零异常
    /// （旧实现静态 width/height 与数组分别发布，换尺寸窗口内后台读可 IndexOutOfRange
    /// 炸寻路线程；数组与尺寸绑定 Snapshot 单次发布后该窗口必须永久关闭）。
    /// </summary>
    [TestFixture]
    public class WalkabilityCacheTests
    {
        private static readonly FieldInfo isBuiltField = typeof(WalkabilityCache).GetField(
            "isBuilt", BindingFlags.NonPublic | BindingFlags.Static);

        [SetUp]
        public void SetUp()
        {
            WalkabilityCache.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            WalkabilityCache.Clear();
        }

        /// <summary>未初始化/未构建：任意坐标返回 false（宁可寻路失败不误穿墙的安全方向）。</summary>
        [Test]
        public void IsWalkable_BeforeInitialize_ReturnsFalse()
        {
            Assert.IsFalse(WalkabilityCache.IsWalkable(0, 0));
            Assert.IsFalse(WalkabilityCache.IsWalkable(5, -3));
            Assert.IsFalse(WalkabilityCache.IsInitialized);
        }

        /// <summary>Initialize 后已初始化标志成立；同参数重复 Initialize 幂等无异常。</summary>
        [Test]
        public void Initialize_ThenInitializedFlag_SameParamsIdempotent()
        {
            WalkabilityCache.Initialize(4, 4, 1);
            Assert.IsTrue(WalkabilityCache.IsInitialized);

            WalkabilityCache.Initialize(4, 4, 1);
            Assert.IsTrue(WalkabilityCache.IsInitialized);
        }

        /// <summary>
        /// 核心回归：反射发布构建标志后，多后台线程循环扫描大小地图边界坐标带，
        /// 主线程反复 4×4↔8×8 换尺寸——全程不得抛任何异常。
        /// 只断言「永不抛」（无时序依赖，修复后恒过）；旧实现「新边界+旧数组」
        /// 越界窗口在此压测下可间歇性炸线程。
        /// </summary>
        [Test]
        public void IsWalkable_UnderSizeSwitchingConcurrentReads_NeverThrows()
        {
            WalkabilityCache.Initialize(8, 8, 1);
            isBuiltField.SetValue(null, true); // 绕过 EnsureBuilt（icall），仅放开 IsWalkable 读路径

            const int Readers = 3;
            ConcurrentQueue<Exception> errors = new ConcurrentQueue<Exception>();
            ManualResetEvent stop = new ManualResetEvent(false);
            Thread[] threads = new Thread[Readers];
            for (int i = 0; i < Readers; i++)
            {
                threads[i] = new Thread(() =>
                {
                    try
                    {
                        while (!stop.WaitOne(0))
                        {
                            // 扫过大小两档地图的边界坐标带，覆盖任何新旧边界组合
                            for (int y = 0; y < 10; y++)
                            {
                                for (int x = 0; x < 10; x++)
                                {
                                    WalkabilityCache.IsWalkable(x, y);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        errors.Enqueue(e);
                    }
                })
                {
                    IsBackground = true,
                };
                threads[i].Start();
            }

            // 主线程换尺寸：新 Snapshot 发布 + isBuilt 复位/再置真交替（模拟重建-可用循环）
            for (int round = 0; round < 200; round++)
            {
                WalkabilityCache.Initialize(4, 4, round + 2);
                isBuiltField.SetValue(null, true);
                WalkabilityCache.Initialize(8, 8, round + 2);
                isBuiltField.SetValue(null, true);
            }

            stop.Set();
            foreach (Thread t in threads)
            {
                t.Join(2000);
            }

            Assert.IsEmpty(errors, $"并发读不应抛异常，捕获 {errors.Count} 个（首个: {(errors.IsEmpty ? "无" : errors.ToArray()[0].Message)}）");
        }
    }
}
