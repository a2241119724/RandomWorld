namespace LAB2D.AI.Dialogue.Core
{
    using LAB2D;
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
                this.ResumeDialogueWorker(npcId);
            }

            DialogueSession session = new DialogueSession(npcId, profile);
            this.activeSessions[npcId] = session;
            this.PauseDialogueWorker(npcId);

            LogManager.Instance.Log(
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
                this.ResumeDialogueWorker(npcId);
                this.dialogueWorkers.Remove(npcId);

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
            return this.BuildGameStateContext(string.Empty, profile);
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
                LogManager.Instance.Log(
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
            foreach (AWorkerTask.WorkerTaskTypeEnum taskType in
                Enum.GetValues(typeof(AWorkerTask.WorkerTaskTypeEnum)))
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
