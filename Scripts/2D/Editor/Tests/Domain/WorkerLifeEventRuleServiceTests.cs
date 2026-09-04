namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Character.Worker;
    using LAB2D.Domain.Worker;
    using NUnit.Framework;
    using System.Collections.Generic;

    /// <summary>
    /// 随机人生事件纯规则 — 权重掷骰覆盖、封顶、恩典（濒危免骰）、负事件不动硬生存值。
    /// </summary>
    [TestFixture]
    public class WorkerLifeEventRuleServiceTests
    {
        [Test]
        public void GetEventTable_HasSevenDefs()
        {
            List<WorkerLifeEventDef> table = WorkerLifeEventRuleService.GetEventTable();
            Assert.AreEqual(7, table.Count);

            float total = 0f;
            foreach (WorkerLifeEventDef d in table)
            {
                Assert.IsFalse(string.IsNullOrEmpty(d.TypeKey));
                Assert.Greater(d.Weight, 0f);
                total += d.Weight;
            }

            Assert.Greater(total, 0f);
        }

        [Test]
        public void Roll_Zero_ReturnsFirstWeighted()
        {
            WorkerLifeEventDef d = WorkerLifeEventRuleService.Roll(0f);
            Assert.AreEqual(WorkerMindConstant.EVT_INSIGHT, d.TypeKey);
        }

        [Test]
        public void Roll_JustBelowOne_ReturnsLastWeighted()
        {
            WorkerLifeEventDef d = WorkerLifeEventRuleService.Roll(0.9999f);
            Assert.AreEqual(WorkerMindConstant.EVT_NIGHTMARE, d.TypeKey);
        }

        [Test]
        public void Roll_OutOfRange_Clamps()
        {
            Assert.AreEqual(
                WorkerLifeEventRuleService.Roll(0f).TypeKey,
                WorkerLifeEventRuleService.Roll(-5f).TypeKey);
            Assert.AreEqual(
                WorkerLifeEventRuleService.Roll(0.9999f).TypeKey,
                WorkerLifeEventRuleService.Roll(2f).TypeKey);
        }

        [Test]
        public void Roll_Sweep_CoversAllSevenTypes()
        {
            // 概率和=1：全区间扫描，7 个事件全部可达（无死权重）
            var seen = new HashSet<string>();
            for (int i = 0; i < 10000; i++)
            {
                float rv = i / 10000f;
                seen.Add(WorkerLifeEventRuleService.Roll(rv).TypeKey);
            }

            Assert.AreEqual(7, seen.Count);
            Assert.IsTrue(seen.Contains(WorkerMindConstant.EVT_INSIGHT));
            Assert.IsTrue(seen.Contains(WorkerMindConstant.EVT_WIND_FALL));
            Assert.IsTrue(seen.Contains(WorkerMindConstant.EVT_SMALL_JOY));
            Assert.IsTrue(seen.Contains(WorkerMindConstant.EVT_ENLIGHTENMENT));
            Assert.IsTrue(seen.Contains(WorkerMindConstant.EVT_MISFORTUNE));
            Assert.IsTrue(seen.Contains(WorkerMindConstant.EVT_ILLNESS));
            Assert.IsTrue(seen.Contains(WorkerMindConstant.EVT_NIGHTMARE));
        }

        [Test]
        public void Apply_CapsSpiritDeltaToSurvivalCap()
        {
            AWorker.WorkerData wd = new AWorker.WorkerData();
            wd.CurSpirit = 50f;
            wd.MaxSpirit = 100f;
            var mind = new WorkerMindData();

            var def = new WorkerLifeEventDef { SpiritDelta = 999f };
            WorkerLifeEventRuleService.Apply(wd, mind, def, 1);

            Assert.AreEqual(50f + WorkerMindConstant.LifeEventSurvivalDamageCap, wd.CurSpirit, 0.0001f);
        }

        [Test]
        public void Apply_ClampsSpiritToBounds()
        {
            AWorker.WorkerData wd = new AWorker.WorkerData();
            wd.CurSpirit = 95f;
            wd.MaxSpirit = 100f;
            var mind = new WorkerMindData();

            WorkerLifeEventRuleService.Apply(wd, mind,
                new WorkerLifeEventDef { SpiritDelta = 15f }, 1);
            Assert.AreEqual(100f, wd.CurSpirit, 0.0001f);

            wd.CurSpirit = 5f;
            WorkerLifeEventRuleService.Apply(wd, mind,
                new WorkerLifeEventDef { SpiritDelta = -15f }, 1);
            Assert.AreEqual(0f, wd.CurSpirit, 0.0001f);
        }

        [Test]
        public void Apply_MoraleClampedToBounds()
        {
            AWorker.WorkerData wd = new AWorker.WorkerData();
            wd.CurMorale = 98f;
            wd.MaxMorale = 100f;
            var mind = new WorkerMindData();

            WorkerLifeEventRuleService.Apply(wd, mind,
                new WorkerLifeEventDef { MoraleDelta = 15f }, 1);
            Assert.AreEqual(100f, wd.CurMorale, 0.0001f);
        }

        [Test]
        public void Apply_NegativeEvent_DoesNotTouchHardSurvival()
        {
            // ③ 可恢复：事件结构无饥饿/疲劳增量，负事件只动软维度
            AWorker.WorkerData wd = new AWorker.WorkerData();
            wd.CurHungry = 80f;
            wd.CurTired = 20f;
            wd.CurSpirit = 50f;
            var mind = new WorkerMindData();

            WorkerLifeEventRuleService.Apply(wd, mind,
                new WorkerLifeEventDef { Valence = MemoryValence.Negative, SpiritDelta = -10f, MoraleDelta = -8f, MoodDelta = -5f }, 1);

            Assert.AreEqual(80f, wd.CurHungry, 0.0001f);
            Assert.AreEqual(20f, wd.CurTired, 0.0001f);
            Assert.AreEqual(40f, wd.CurSpirit, 0.0001f);
        }

        [Test]
        public void Apply_GoldDelta_AddsToWallet()
        {
            AWorker.WorkerData wd = new AWorker.WorkerData();
            var mind = new WorkerMindData();

            WorkerLifeEventRuleService.Apply(wd, mind,
                new WorkerLifeEventDef { GoldDelta = 40f }, 1);

            Assert.AreEqual(70, wd.Wallet.Gold); // 构造自带初始 30 金（AWorker.cs:1678）
        }

        [Test]
        public void Apply_ZeroGoldDelta_WalletUnchanged()
        {
            AWorker.WorkerData wd = new AWorker.WorkerData();
            var mind = new WorkerMindData();

            WorkerLifeEventRuleService.Apply(wd, mind,
                new WorkerLifeEventDef { Valence = MemoryValence.Negative }, 1);

            Assert.AreEqual(30, wd.Wallet.Gold); // 零增量不动钱包，保持构造初始 30
        }

        [Test]
        public void Apply_DriftCappedToPersonalityDriftCap()
        {
            AWorker.WorkerData wd = new AWorker.WorkerData();
            var mind = new WorkerMindData();

            WorkerLifeEventRuleService.Apply(wd, mind,
                new WorkerLifeEventDef { AmbitionDrift = 999f, SocialityDrift = -999f }, 1);

            Assert.AreEqual(WorkerMindConstant.LifeEventPersonalityDriftCap, mind.Drift.AmbitionDrift, 0.0001f);
            Assert.AreEqual(-WorkerMindConstant.LifeEventPersonalityDriftCap, mind.Drift.SocialityDrift, 0.0001f);
        }

        [Test]
        public void IsCritical_ExtremeHunger_True()
        {
            AWorker.WorkerData wd = new AWorker.WorkerData();
            wd.MaxHungry = 100f;
            wd.CurHungry = 4f; // 4% ≤ CriticalRatio 5%
            Assert.IsTrue(WorkerLifeEventRuleService.IsCritical(wd));
        }

        [Test]
        public void IsCritical_ExtremeTired_True()
        {
            AWorker.WorkerData wd = new AWorker.WorkerData();
            wd.MaxTired = 100f;
            wd.CurTired = 97f; // 97% ≥ 95%
            Assert.IsTrue(WorkerLifeEventRuleService.IsCritical(wd));
        }

        [Test]
        public void IsCritical_Healthy_False()
        {
            AWorker.WorkerData wd = new AWorker.WorkerData();
            Assert.IsFalse(WorkerLifeEventRuleService.IsCritical(wd));
        }

        [Test]
        public void IsCritical_Null_True()
        {
            Assert.IsTrue(WorkerLifeEventRuleService.IsCritical(null));
        }
    }
}
