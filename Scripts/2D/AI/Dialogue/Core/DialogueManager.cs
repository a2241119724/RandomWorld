namespace LAB2D.AI.Dialogue.Core
{
    using LAB2D;
    using LAB2D.AI.Dialogue.LLM;
    using LAB2D.AI.Dialogue.Memory;
    using LAB2D.AI.Dialogue.Prompt;
    using LAB2D.AI.Dialogue.RAG;
    using LAB2D.Character.Worker;
    using LAB2D.Character.Worker.Task;
    using LAB2D.Enum;
    using LAB2D.Serializable;
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
        private string cachedClientSource; // "local" or "remote"
        private PromptBuilder promptBuilder => PromptBuilder.Instance;
        private DialogueMemoryManager memoryManager => DialogueMemoryManager.Instance;
        private GameKnowledgeRetriever knowledgeRetriever => GameKnowledgeRetriever.Instance;
        private PromptTemplateLoader requestLogTemplateLoader;

        private readonly Dictionary<string, DialogueSession> activeSessions = new Dictionary<string, DialogueSession>();
        private readonly Dictionary<string, AWorker> dialogueWorkers = new Dictionary<string, AWorker>();

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
        /// 清除客户端缓存（设置变更后调用，下次请求时重建）
        /// </summary>
        public void ResetLLMClient()
        {
            this.llmClient = null;
            this.cachedClientSource = null;
        }
        public ILLMClient GetLLMClient()
        {
            string expectedSource = ModelSourceSettings.Current == ModelSource.Remote
                ? "remote"
                : "local";

            if (this.llmClient == null || this.cachedClientSource != expectedSource)
            {
                if (ModelSourceSettings.Current == ModelSource.Remote)
                {
                    this.llmClient = new RemoteAPIClient(
                        ModelSourceSettings.RemoteApiBaseUrl,
                        ModelSourceSettings.RemoteApiKey,
                        ModelSourceSettings.RemoteModelName);
                }
                else
                {
                    this.llmClient = new LlamaServerClient();
                }

                this.cachedClientSource = expectedSource;
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
                this.ResumeDialogueWorker(npcId);
            }

            DialogueSession session = new DialogueSession(npcId, profile);
            this.activeSessions[npcId] = session;
            this.PauseDialogueWorker(npcId);

            AWorkerTask.LogProvider(
                "DialogueManager: 开始对话 " + profile.npcName,
                LogManager.LogLevelEnum.Info);

            return session;
        }

        /// <summary>
        /// 注册对话目标对应的 Worker，仅用于向 Prompt 注入该 Worker 自身数据。
        /// </summary>
        public void RegisterDialogueWorker(string npcId, AWorker worker)
        {
            if (string.IsNullOrEmpty(npcId) || worker == null)
            {
                return;
            }

            this.dialogueWorkers[npcId] = worker;
        }

        /// <summary>
        /// 发送玩家消息
        /// </summary>
        public async void SendMessage(string npcId, string playerInput)
        {
            if (!this.activeSessions.TryGetValue(npcId, out DialogueSession session))
            {
                AWorkerTask.LogProvider(
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

            // 1. 收集当前对话 Worker 的自身状态
            GameStateContext gameContext = this.BuildGameStateContext(npcId, session.profile);

            // 2. 获取对话历史
            bool sendOnlyCurrentMessage = session.profile == null || session.profile.sendOnlyCurrentMessage;
            List<ChatMessage> history = sendOnlyCurrentMessage
                ? null
                : this.memoryManager.GetRecentHistory(npcId, 6);

            // 3. RAG 检索
            List<GameKnowledgeEntry> ragResults = this.knowledgeRetriever.Retrieve(
                playerInput, session.profile.knowledgeTags);

            // 4. 组装 Prompt
            List<ChatMessage> messages = this.promptBuilder.BuildMessages(
                session.profile, playerInput, history, gameContext, ragResults);

            this.LogRequest(npcId, session, playerInput, messages, gameContext, ragResults, history);

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

                        AWorkerTask.LogProvider(
                            "DialogueManager: NPC " + session.profile.npcName + " 回复完成",
                            LogManager.LogLevelEnum.Info);
                    },
                    onError: (error) =>
                    {
                        AWorkerTask.LogProvider(
                            "DialogueManager: 对话错误 - " + error,
                            LogManager.LogLevelEnum.Error);
                        this.OnDialogueError?.Invoke(npcId, error);
                    });
            }
            catch (Exception e)
            {
                AWorkerTask.LogProvider(
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
                this.ResumeDialogueWorker(npcId);
                this.dialogueWorkers.Remove(npcId);

                AWorkerTask.LogProvider(
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
        /// 构建当前对话 Worker 的自身状态上下文。
        /// </summary>
        public GameStateContext BuildGameStateContext(string npcId, NPCPromptProfile profile)
        {
            GameStateContext context = new GameStateContext();

            try
            {
                this.FillWorkerContext(context, npcId);
            }
            catch (Exception e)
            {
                AWorkerTask.LogProvider(
                    "DialogueManager.BuildGameStateContext: " + e.Message,
                    LogManager.LogLevelEnum.Warning);
            }

            return context;
        }

        private void FillWorkerContext(GameStateContext context, string npcId)
        {
            if (context == null)
            {
                return;
            }

            context.workerDetails.Clear();
            AWorker worker = this.GetDialogueWorker(npcId);
            if (worker == null)
            {
                return;
            }

            if (!WorkerConditionTool.TryGetWorkerData(worker, out AWorker.WorkerData workerData))
            {
                return;
            }

            WorkerConditionState conditionState = WorkerConditionTool.GetState(workerData);
            context.workerDetails.Add(this.BuildWorkerPromptInfo(worker, workerData, conditionState));
        }

        private AWorker GetDialogueWorker(string npcId)
        {
            if (!string.IsNullOrEmpty(npcId) &&
                this.dialogueWorkers.TryGetValue(npcId, out AWorker worker) &&
                worker != null)
            {
                return worker;
            }

            return null;
        }

        private void PauseDialogueWorker(string npcId)
        {
            AWorker worker = this.GetDialogueWorker(npcId);
            if (worker != null)
            {
                worker.PauseForDialogue();
            }
        }

        private void ResumeDialogueWorker(string npcId)
        {
            AWorker worker = this.GetDialogueWorker(npcId);
            if (worker != null)
            {
                worker.ResumeFromDialogue();
            }
        }

        private GameStateContext.WorkerPromptInfo BuildWorkerPromptInfo(
            AWorker worker,
            AWorker.WorkerData workerData,
            WorkerConditionState conditionState)
        {
            Vector3Int posMap = TileMap.Instance == null
                ? Vector3Int.zero
                : TileMap.Instance.WorldPosToMapPos(worker.transform.position);

            return new GameStateContext.WorkerPromptInfo
            {
                workerName = worker.name,
                level = workerData.Level,
                hp = workerData.Hp,
                maxHp = workerData.MaxHp,
                positionText = FormatMapPos(posMap),
                conditionText = WorkerConditionTool.GetStateName(conditionState),
                stateText = worker.Manager == null
                    ? workerData.CurrentStateType.ToString()
                    : worker.Manager.CurrentStateType.ToString(),
                hungry = workerData.CurHungry,
                maxHungry = workerData.MaxHungry,
                tired = workerData.CurTired,
                maxTired = workerData.MaxTired,
                hasBed = worker.BedItem != null,
                taskText = BuildWorkerTaskText(workerData.Task),
                equipmentText = BuildWorkerEquipmentText(workerData),
                isSeeking = worker.Seek != null && worker.Seek.IsSeeking(),
                seekTargetText = worker.Seek == null ? string.Empty : FormatMapPos(worker.Seek.TargetMap),
                enabledTaskText = BuildEnabledTaskText(workerData.TaskToggle),
            };
        }

        private static string BuildWorkerTaskText(AWorkerTask task)
        {
            if (task == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.Append(WorkerTaskSummaryTool.GetTaskDisplayName(task.TaskType));
            if (!string.IsNullOrEmpty(task.Name) && task.Name != task.TaskType.ToString())
            {
                sb.Append('/');
                sb.Append(task.Name);
            }

            if (task.TaskId > 0)
            {
                sb.Append("(ID");
                sb.Append(task.TaskId);
                sb.Append(')');
            }

            if (task.TargetMap != null)
            {
                sb.Append('@');
                sb.Append(FormatMapPos(task.TargetMap));
            }

            return sb.ToString();
        }

        private static string BuildWorkerEquipmentText(AWorker.WorkerData workerData)
        {
            if (workerData == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            if (workerData.Weapon != null)
            {
                AppendEquipmentName(sb, workerData.Weapon.Id);
            }

            Dictionary<AEquipment.EquipTypeEnum, AEquipment> equipments = workerData.GetEquipments();
            if (equipments != null)
            {
                foreach (KeyValuePair<AEquipment.EquipTypeEnum, AEquipment> pair in equipments)
                {
                    if (pair.Value == null)
                    {
                        continue;
                    }

                    AppendEquipmentName(sb, pair.Value.Id);
                }
            }

            return sb.Length == 0 ? "无" : sb.ToString();
        }

        private static void AppendEquipmentName(StringBuilder sb, int itemId)
        {
            if (sb.Length > 0)
            {
                sb.Append('/');
            }

            string itemName = "未知装备";
            if (ItemDataManager.Instance != null)
            {
                ItemData itemData = ItemDataManager.Instance.GetById(itemId);
                if (itemData != null && !string.IsNullOrEmpty(itemData.CnName))
                {
                    itemName = itemData.CnName;
                }
            }

            sb.Append(itemName);
        }

        private static string BuildEnabledTaskText(bool[] taskToggle)
        {
            if (taskToggle == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (WorkerTaskType taskType in
                Enum.GetValues(typeof(WorkerTaskType)))
            {
                int index = (int)taskType;
                if (index < 0 || index >= taskToggle.Length || !taskToggle[index])
                {
                    continue;
                }

                if (sb.Length > 0)
                {
                    sb.Append('/');
                }

                sb.Append(WorkerTaskSummaryTool.GetTaskDisplayName(taskType));
            }

            return sb.ToString();
        }

        private static string FormatMapPos(Vector3Int posMap)
        {
            return "(" + posMap.x + "," + posMap.y + ")";
        }

        private static string FormatMapPos(Vector3IntLAB posMap)
        {
            return posMap == null ? string.Empty : "(" + posMap.X + "," + posMap.Y + ")";
        }

        private void LogRequest(
            string npcId,
            DialogueSession session,
            string playerInput,
            List<ChatMessage> messages,
            GameStateContext gameContext,
            List<GameKnowledgeEntry> ragResults,
            List<ChatMessage> history)
        {
            NPCPromptProfile profile = session.profile;
            if (this.requestLogTemplateLoader == null)
            {
                this.requestLogTemplateLoader = new PromptTemplateLoader();
            }

            var replacements = new Dictionary<string, string>
            {
                { "NPC_ID", npcId },
                { "NPC_NAME", profile != null ? profile.npcName : "未知" },
                { "NPC_ROLE", profile != null ? profile.npcRole : "未知" },
                { "LLM_PARAMS", BuildLLMParamsText(session) },
                { "NPC_PROFILE", BuildNPCProfileText(profile) },
                { "GAME_STATE", BuildGameStateText(gameContext) },
                { "RAG_KNOWLEDGE", BuildRAGText(ragResults) },
                { "HISTORY", BuildHistoryText(history) },
                { "PLAYER_INPUT", "  " + playerInput },
                { "SYSTEM_PROMPT", BuildSystemPromptText(messages) },
            };

            string logText = this.requestLogTemplateLoader.FillTemplate(
                "RequestLogTemplate", replacements);

            if (string.IsNullOrEmpty(logText))
            {
                logText = BuildInlineLog(replacements);
            }

            AWorkerTask.LogProvider(logText, LogManager.LogLevelEnum.Info);
        }

        private static string BuildInlineLog(Dictionary<string, string> r)
        {
            var sb = new StringBuilder();
            sb.AppendLine("===== AI 请求参数 =====");
            sb.Append("[NPC] "); sb.Append(r["NPC_NAME"]);
            sb.Append(" ("); sb.Append(r["NPC_ROLE"]);
            sb.Append(") | npcId="); sb.AppendLine(r["NPC_ID"]);
            sb.Append("[生成参数] "); sb.AppendLine(r["LLM_PARAMS"]);
            sb.AppendLine("--- NPC 身份 ---");
            sb.Append(r["NPC_PROFILE"]);
            sb.AppendLine("--- 自身状态 ---");
            sb.AppendLine(r["GAME_STATE"]);
            sb.AppendLine("--- RAG 知识 ---");
            sb.Append(r["RAG_KNOWLEDGE"]);
            sb.AppendLine("--- 对话历史 ---");
            sb.Append(r["HISTORY"]);
            sb.AppendLine("--- 当前输入 ---");
            sb.AppendLine(r["PLAYER_INPUT"]);
            sb.AppendLine("--- 最终系统提示词 ---");
            sb.Append(r["SYSTEM_PROMPT"]);
            sb.Append("=========================");
            return sb.ToString();
        }

        private static string BuildLLMParamsText(DialogueSession session)
        {
            return "temperature=" + session.options.temperature
                + " maxTokens=" + session.options.maxTokens
                + " topP=" + session.options.topP
                + " repeatPenalty=" + session.options.repeatPenalty
                + " deepThinking=" + (session.options.deepThinking ? "ON" : "OFF")
                + " stream=" + (session.options.stream ? "ON" : "OFF");
        }

        private static string BuildNPCProfileText(NPCPromptProfile profile)
        {
            if (profile == null)
            {
                return "  (无)\n";
            }

            var sb = new StringBuilder();
            sb.Append("  姓名: "); sb.AppendLine(profile.npcName);
            sb.Append("  角色: "); sb.AppendLine(profile.npcRole);
            sb.Append("  地点: "); sb.AppendLine(profile.npcLocation);
            sb.Append("  性格: "); sb.AppendLine(profile.personalityDescription);
            if (!string.IsNullOrEmpty(profile.backgroundStory))
            {
                sb.Append("  背景: "); sb.AppendLine(profile.backgroundStory);
            }

            sb.Append("  说话风格: "); sb.AppendLine(StripSpeakingPrefix(profile.speakingStyle));
            sb.Append("  最大句数: "); sb.Append(profile.maxSentences.ToString());
            return sb.ToString();
        }

        private static string BuildGameStateText(GameStateContext gameContext)
        {
            return gameContext != null ? gameContext.ToPromptText() : "(空)";
        }

        private static string BuildRAGText(List<GameKnowledgeEntry> ragResults)
        {
            if (ragResults == null || ragResults.Count == 0)
            {
                return "  (无命中)\n";
            }

            var sb = new StringBuilder();
            sb.Append("  命中 "); sb.Append(ragResults.Count); sb.AppendLine(" 条:");
            foreach (GameKnowledgeEntry entry in ragResults)
            {
                sb.Append("    - "); sb.AppendLine(entry.ToPromptText());
            }

            return sb.ToString();
        }

        private static string BuildHistoryText(List<ChatMessage> history)
        {
            if (history == null || history.Count == 0)
            {
                return "  (无历史，仅发送当前消息)\n";
            }

            var sb = new StringBuilder();
            sb.Append("  共 "); sb.Append(history.Count / 2); sb.AppendLine(" 轮:");
            foreach (ChatMessage msg in history)
            {
                sb.Append("    [");
                sb.Append(msg.role);
                sb.Append("] ");
                sb.AppendLine(msg.content);
            }

            return sb.ToString();
        }

        private static string BuildSystemPromptText(List<ChatMessage> messages)
        {
            if (messages == null || messages.Count == 0 || messages[0].role != "system")
            {
                return "  (无)\n";
            }

            return messages[0].content;
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

        private static string StripSpeakingPrefix(string speakingStyle)
        {
            if (string.IsNullOrWhiteSpace(speakingStyle))
            {
                return "简洁";
            }

            speakingStyle = speakingStyle.Trim();
            return speakingStyle.StartsWith("说话")
                ? speakingStyle.Substring("说话".Length).Trim()
                : speakingStyle;
        }
    }
}
