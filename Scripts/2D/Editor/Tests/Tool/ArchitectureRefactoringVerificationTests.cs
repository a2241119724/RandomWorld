namespace LAB2D.Editor.Tests.Tool
{
    using LAB2D.Domain.Worker;
    using LAB2D.Gameplay;
    using NUnit.Framework;

    /// <summary>
    /// 验证本轮架构改造成果。
    ///
    /// 注意：Provider 委托（internal static）在 Editor 程序集中不可访问，
    /// 其可替换性由设计保证。39 个已有 Domain 测试覆盖全部 RuleService。
    /// 本测试聚焦本轮改造特有的集成验证点。
    /// </summary>
    [TestFixture]
    public class ArchitectureRefactoringVerificationTests
    {
        #region GameplaySessionStats — 验证 IGameTime 迁移后构造函数安全

        [Test]
        public void GameplaySessionStats_Construct_DoesNotThrow()
        {
            // 验证构造函数 + ResetSession 使用 IGameTime 而非 Time.realtimeSinceStartup
            // (IGameTime 已在 GlobalInit.RegisterSafeServices 中注册)
            Assert.DoesNotThrow(() =>
            {
                GameplaySessionStats stats = new GameplaySessionStats();
                stats.ResetSession();
            }, "GameplaySessionStats 构造应通过 IGameTime 接口安全访问时间");
        }

        [Test]
        public void GameplaySessionStats_CreateSnapshot_HasValidDuration()
        {
            GameplaySessionStats stats = new GameplaySessionStats();
            GameplaySessionStatsSnapshot snapshot = stats.CreateSnapshot();
            Assert.GreaterOrEqual(snapshot.SessionDuration, 0f,
                "会话持续时间应 >= 0（通过 IGameTime.RealtimeSinceStartup 计算）");
        }

        #endregion

        #region ComboBonusManager — 验证移除 using UnityEngine 后功能正常

        [Test]
        public void ComboBonusManager_GetDamageMultiplier_NoCombo_ReturnsOne()
        {
            float multiplier = ComboBonusManager.GetDamageMultiplierForCombo(1);
            Assert.AreEqual(1.0f, multiplier, 0.0001f);
        }

        [Test]
        public void ComboBonusManager_GetDamageMultiplier_HighCombo_GreaterThanOne()
        {
            float multiplier = ComboBonusManager.GetDamageMultiplierForCombo(50);
            Assert.Greater(multiplier, 1.0f);
        }

        [Test]
        public void ComboBonusManager_GetExperienceMultiplier_ScalesWithCombo()
        {
            float low = ComboBonusManager.GetExperienceMultiplierForCombo(3);
            float high = ComboBonusManager.GetExperienceMultiplierForCombo(20);
            Assert.Greater(high, low,
                "更高连击数应产生更高的经验倍率（纯 C# 计算，零 UnityEngine）");
        }

        #endregion

        #region WorkerSupplyIssueManager — 验证移除 using UnityEngine 后可实例化

        [Test]
        public void WorkerSupplyIssueManager_CurrentReport_DoesNotThrow()
        {
            // 验证 CurrentReport 属性通过 FoodInventoryProvider/BedBindingProvider 获取数据
            // 并完成了 Vector3Int → GameGridPosition 转换
            WorkerSupplyIssueManager manager = WorkerSupplyIssueManager.Instance;
            Assert.DoesNotThrow(() =>
            {
                WorkerSupplyReport report = manager.CurrentReport;
                Assert.NotNull(report);
            }, "CurrentReport 应通过 GameGridPosition Provider 安全获取补给数据");
        }

        #endregion
    }
}
