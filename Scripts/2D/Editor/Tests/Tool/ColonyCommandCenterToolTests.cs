namespace LAB2D.Editor.Tests.Tool
{
    using LAB2D.Domain.Worker;
    using LAB2D.Enum;
    using LAB2D.Tool;
    using NUnit.Framework;

    /// <summary>
    /// ColonyCommandCenterTool 纯逻辑方法单元测试。
    /// 不依赖 Unity 场景，可直接运行。
    /// </summary>
    [TestFixture]
    public class ColonyCommandCenterToolTests
    {
        [Test]
        public void GetAlertLevelName_Stable_ReturnsChinese()
        {
            string name = ColonyCommandCenterTool.GetAlertLevelName(ColonyCommandAlertLevel.Stable);
            Assert.AreEqual("稳定", name);
        }

        [Test]
        public void GetAlertLevelName_Notice_ReturnsChinese()
        {
            string name = ColonyCommandCenterTool.GetAlertLevelName(ColonyCommandAlertLevel.Notice);
            Assert.AreEqual("关注", name);
        }

        [Test]
        public void GetAlertLevelName_Warning_ReturnsChinese()
        {
            string name = ColonyCommandCenterTool.GetAlertLevelName(ColonyCommandAlertLevel.Warning);
            Assert.AreEqual("警告", name);
        }

        [Test]
        public void GetAlertLevelName_Critical_ReturnsChinese()
        {
            string name = ColonyCommandCenterTool.GetAlertLevelName(ColonyCommandAlertLevel.Critical);
            Assert.AreEqual("危急", name);
        }

        [Test]
        public void GetAlertLevelRichColor_All_NonEmpty()
        {
            foreach (ColonyCommandAlertLevel level in System.Enum.GetValues(typeof(ColonyCommandAlertLevel)))
            {
                string color = ColonyCommandCenterTool.GetAlertLevelRichColor(level);
                Assert.IsNotNull(color);
                Assert.IsNotEmpty(color);
            }
        }

        [Test]
        public void GetBlockReasonName_All_NonEmpty()
        {
            foreach (WorkerTaskBlockReason reason in System.Enum.GetValues(typeof(WorkerTaskBlockReason)))
            {
                string name = ColonyCommandCenterTool.GetBlockReasonName(reason);
                Assert.IsNotNull(name);
                Assert.IsNotEmpty(name);
            }
        }

        [Test]
        public void GetBlockReasonRichColor_All_NonEmpty()
        {
            foreach (WorkerTaskBlockReason reason in System.Enum.GetValues(typeof(WorkerTaskBlockReason)))
            {
                string color = ColonyCommandCenterTool.GetBlockReasonRichColor(reason);
                Assert.IsNotNull(color);
                Assert.IsNotEmpty(color);
            }
        }

        [Test]
        public void BuildPlainText_NullReport_ReturnsEmptyText()
        {
            string result = ColonyCommandCenterTool.BuildPlainText(null);
            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
        }

        [Test]
        public void ToPlainText_NullReport_ReturnsEmpty()
        {
            string result = default(WorkerTaskAssignmentReport).ToPlainText();
            Assert.IsNotNull(result);
        }

        [Test]
        public void ToDisplayLine_NullDetail_ReturnsEmpty()
        {
            string result = default(WorkerTaskBlockDetail).ToDisplayLine();
            Assert.AreEqual(string.Empty, result);
        }

        [Test]
        public void TryGetFieldValue_NullTarget_ReturnsFalse()
        {
            bool ok = ColonyCommandCenterTool.TryGetFieldValue<string>(null, "anything", out string value);
            Assert.IsFalse(ok);
            Assert.IsNull(value);
        }

        [Test]
        public void TryGetFieldValue_EmptyFieldName_ReturnsFalse()
        {
            bool ok = ColonyCommandCenterTool.TryGetFieldValue<string>(new object(), string.Empty, out string value);
            Assert.IsFalse(ok);
            Assert.IsNull(value);
        }
    }
}
