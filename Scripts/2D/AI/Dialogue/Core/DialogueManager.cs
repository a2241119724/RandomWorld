namespace LAB2D
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using UnityEngine;

    /// <summary>
    /// NPC 对话管理器，组织完整对话流程
    /// </summary>
    public class DialogueManager : Singleton<DialogueManager>
    {
        private ILLMClient llmClient;
        private PromptBuilder promptBuilder => PromptBuilder.Instance;
        private DialogueMemoryManager memoryManager => DialogueMemoryManager.Instance;
        private GameKnowledgeRetriever knowledgeRetriever => GameKnowledgeRetriever.Instance;

        private readonly Dictionary<string, DialogueSession> activeSessions = new Dictionary<string, DialogueSession>();

        /// <summary>
        /// 流式 token 回调（npcId, token）
        /// </summary>
        public event Action<string, string> OnTokenReceived;

        /// <summary>
        /// 对话完成回调
        /// </summary>
        public event Action<string, string> OnDialogueComplete;

        /// <summary>
        /// 错误回调
        /// </summary>
        public event Action<string, string> OnDialogueError;

        /// <summary>
        /// 对话结束回调（面板关闭时触发）
        /// </summary>
        public event Action<string> OnDialogueEnded;

        /// <summary>
        /// 设置 LLM 客户端（可在构造后替换）
        /// </summary>
        public void SetLLMClient(ILLMClient client)
        {
            this.llmClient = client;
        }

        /// <summary>
        /// 获取或创建 LLM 客户端
        /// </summary>
        public ILLMClient GetLLMClient()
        {
            if (this.llmClient == null)
            {
                this.llmClient = new LlamaServerClient();
            }

            return this.llmClient;
        }

        /// <summary>
        /// 开始与 NPC 对话
        /// </summary>
        public DialogueSession StartDialogue(string npcId, NPCPromptProfile profile)
        {
            // 如果已有活跃会话，先结束
            if (this.activeSessions.TryGetValue(npcId, out DialogueSession existingSession))
            {
                existingSession.isActive = false;
            }

            DialogueSession session = new DialogueSession(npcId, profile);
            this.activeSessions[npcId] = session;

            LogManager.Instance.Log(
                "DialogueManager: 开始对话 " + profile.npcName,
                LogManager.LogLevelEnum.Info);

            return session;
        }

        /// <summary>
        /// 发送玩家消息
        /// </summary>
        public async void SendMessage(string npcId, string playerInput)
        {
            if (!this.activeSessions.TryGetValue(npcId, out DialogueSession session))
            {
                LogManager.Instance.Log(
                    "DialogueManager: 未找到活跃会话 " + npcId,
                    LogManager.LogLevelEnum.Warning);
                return;
            }

            if (!session.isActive)
            {
                return;
            }

            session.AddPlayerMessage(playerInput);
            session.accumulatedResponse.Clear();

            // 1. 收集游戏状态
            GameStateContext gameContext = this.BuildGameStateContext(session.profile);

            // 2. 获取对话历史
            List<ChatMessage> history = this.memoryManager.GetRecentHistory(npcId, 6);

            // 3. RAG 检索
            List<GameKnowledgeEntry> ragResults = this.knowledgeRetriever.Retrieve(
                playerInput, session.profile.knowledgeTags);

            // 4. 组装 Prompt
            List<ChatMessage> messages = this.promptBuilder.BuildMessages(
                session.profile, playerInput, history, gameContext, ragResults);

            // 5. 调用 LLM
            ILLMClient client = this.GetLLMClient();
            try
            {
                await client.ChatStreamAsync(
                    messages,
                    session.options,
                    onToken: (token) =>
                    {
                        session.accumulatedResponse.Append(token);
                        this.OnTokenReceived?.Invoke(npcId, token);
                    },
                    onComplete: () =>
                    {
                        string fullResponse = session.GetFullResponse();
                        session.AddNPCMessage(fullResponse);
                        this.memoryManager.RecordExchange(npcId, playerInput, fullResponse);
                        this.OnDialogueComplete?.Invoke(npcId, fullResponse);

                        LogManager.Instance.Log(
                            "DialogueManager: NPC " + session.profile.npcName + " 回复完成",
                            LogManager.LogLevelEnum.Info);
                    },
                    onError: (error) =>
                    {
                        LogManager.Instance.Log(
                            "DialogueManager: 对话错误 - " + error,
                            LogManager.LogLevelEnum.Error);
                        this.OnDialogueError?.Invoke(npcId, error);
                    });
            }
            catch (Exception e)
            {
                LogManager.Instance.Log(
                    "DialogueManager.SendMessage 异常: " + e.Message,
                    LogManager.LogLevelEnum.Error);
                this.OnDialogueError?.Invoke(npcId, e.Message);
            }
        }

        /// <summary>
        /// 结束对话
        /// </summary>
        public void EndDialogue(string npcId)
        {
            if (this.activeSessions.TryGetValue(npcId, out DialogueSession session))
            {
                session.isActive = false;
                this.activeSessions.Remove(npcId);

                LogManager.Instance.Log(
                    "DialogueManager: 结束对话 " + session.profile?.npcName,
                    LogManager.LogLevelEnum.Info);
            }

            this.OnDialogueEnded?.Invoke(npcId);
        }

        /// <summary>
        /// 获取活跃会话
        /// </summary>
        public DialogueSession GetSession(string npcId)
        {
            this.activeSessions.TryGetValue(npcId, out DialogueSession session);
            return session;
        }

        /// <summary>
        /// 构建游戏状态上下文
        /// </summary>
        public GameStateContext BuildGameStateContext(NPCPromptProfile profile)
        {
            GameStateContext context = new GameStateContext();

            try
            {
                Player player = PlayerManager.Instance?.Mine;
                if (player != null)
                {
                    context.playerName = player.name;
                    context.playerLevel = player.CharacterDataLAB?.Level ?? 1;
                    context.playerHp = player.CharacterDataLAB?.Hp ?? 100;
                    context.playerMaxHp = player.CharacterDataLAB?.MaxHp ?? 100;

                    // 从 PlayerData 获取额外信息
                    if (player.CharacterDataLAB is Player.PlayerData playerData)
                    {
                        context.playerLevel = playerData.Level;
                    }
                }

                if (WeatherManager.Instance != null)
                {
                    context.currentWeather = WeatherManager.Instance.CurrentWeather.ToString();
                }

                context.npcFavorability = profile != null ? profile.initialFavorability : 50;
            }
            catch (Exception e)
            {
                LogManager.Instance.Log(
                    "DialogueManager.BuildGameStateContext: " + e.Message,
                    LogManager.LogLevelEnum.Warning);
            }

            return context;
        }

        /// <summary>
        /// 获取 NPC 问候语
        /// </summary>
        public string GetGreeting(NPCPromptProfile profile)
        {
            if (profile == null)
            {
                return "你好，旅行者。";
            }

            var sb = new StringBuilder();
            sb.Append("你好，我是");
            sb.Append(profile.npcName);
            sb.Append("，这里的");
            sb.Append(profile.npcRole);
            sb.Append("。有什么我能帮你的吗？");

            return sb.ToString();
        }
    }
}
