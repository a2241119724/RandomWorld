namespace LAB2D.Editor.Tests.Domain
{
    using LAB2D.Domain.Dialogue;
    using NUnit.Framework;
    using System.Collections.Generic;

    [TestFixture]
    public class PromptAssemblyServiceTests
    {
        [Test]
        public void BuildMessages_WithTemplateProvider_FillsSystemPromptAndAppendsInput()
        {
            PromptAssemblyService service = new PromptAssemblyService(new FakePromptTemplateProvider());
            DialoguePromptProfileModel profile = new DialoguePromptProfileModel
            {
                NpcName = "Ada",
                NpcRole = "Engineer",
                NpcLocation = "Lab",
                PersonalityDescription = "Careful",
                BackgroundStory = "Builds tools",
                SpeakingStyle = "Concise",
                MaxSentences = 2,
            };

            List<ChatMessage> messages = service.BuildMessages(
                profile,
                "Hello",
                null,
                "World",
                "State",
                new[] { "Knowledge" });

            Assert.AreEqual(2, messages.Count);
            Assert.AreEqual("system", messages[0].role);
            Assert.IsTrue(messages[0].content.Contains("Ada"));
            Assert.IsTrue(messages[0].content.Contains("Engineer"));
            Assert.IsTrue(messages[0].content.Contains("World"));
            Assert.IsTrue(messages[0].content.Contains("Knowledge"));
            Assert.AreEqual("user", messages[1].role);
            Assert.AreEqual("Hello", messages[1].content);
        }

        private sealed class FakePromptTemplateProvider : IPromptTemplateProvider
        {
            public string FillTemplate(string templateName, Dictionary<string, string> replacements)
            {
                if (templateName == "SystemPromptTemplate")
                {
                    return replacements["NPC_NAME"]
                        + "|"
                        + replacements["NPC_ROLE"]
                        + "|"
                        + replacements["WORLD_INFO"]
                        + "|"
                        + replacements["KNOWLEDGE_CONTEXT"];
                }

                return string.Empty;
            }
        }
    }
}
