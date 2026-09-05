namespace LAB2D.AI.Dialogue.Core
{
    using LAB2D;
    using LAB2D.AI.Dialogue.Prompt;
    using LAB2D.Core;
    using LAB2D.Character.Player;
    using LAB2D.Character.Worker.Task;
    using LAB2D.UnityAdapter;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// NPC 对话触发器，挂载在 NPC GameObject 上，玩家靠近按 I 键开始对话
    /// </summary>
    public class NPCDialogueTrigger : MonoBehaviour
    {
        /// <summary>
        /// NPC 配置名称（对应 Resources/SO/AI/ 下的 NPCPromptProfile 资源名）
        /// </summary>
        public string profileName = "Worker";

        /// <summary>
        /// 交互距离
        /// </summary>
        public float interactionRadius = 3f;

        /// <summary>
        /// 交互按键（默认 I，键位统一收口 InputKeyConstant.NpcDialogueInteract，勿与科技面板 T 冲突）
        /// </summary>
        public KeyCode interactKey = Constant.InputKeyConstant.NpcDialogueInteract;

        private NPCPromptProfile cachedProfile;
        private string npcId;
        private bool isDialogueOpen;

        internal static System.Func<string, NPCPromptProfile> PromptBuilderProvider { get; set; }
            = (name) => ServiceLocator.Get<PromptBuilder>().GetProfile(name);
        internal static System.Func<Player> PlayerMineProvider { get; set; }
            = () => ServiceLocator.Get<PlayerManager>()?.Mine;
        internal static System.Action<string, LogManager.LogLevelEnum> LogProvider { get; set; }
            = (msg, lv) => ServiceLocator.Get<LogManager>()?.Log(msg, lv);

        /// <summary>
        /// 每个 profileName 只输出一次缺失警告
        /// </summary>
        private static readonly HashSet<string> MissingProfileWarned = new HashSet<string>();

        public void Awake()
        {
            this.npcId = this.GetInstanceID().ToString();
        }

        public void Start()
        {
            if (this.cachedProfile == null)
            {
                this.cachedProfile = CreateRuntimeProfile(PromptBuilderProvider(this.profileName));
            }

            if (this.cachedProfile == null)
            {
                if (!MissingProfileWarned.Contains(this.profileName))
                {
                    MissingProfileWarned.Add(this.profileName);
                    AWorkerTask.LogProvider(
                        "NPCDialogueTrigger: 未找到 NPC 配置 " + this.profileName + "，将使用默认配置",
                        LogManager.LogLevelEnum.Warning);
                }
            }
        }

        public void Update()
        {
            if (!UnityGlobalInputAdapter.GetNpcDialogueInteractDown(this.interactKey))
            {
                return;
            }

            if (this.isDialogueOpen)
            {
                return;
            }

            Player player = PlayerMineProvider();
            if (player == null)
            {
                return;
            }

            float sqrDist = (this.transform.position - player.transform.position).sqrMagnitude;
            if (sqrDist > this.interactionRadius * this.interactionRadius)
            {
                return;
            }

            if (this.cachedProfile == null)
            {
                this.cachedProfile = CreateRuntimeProfile(PromptBuilderProvider(this.profileName));
            }

            this.StartDialogue();
        }

        public void OnDestroy()
        {
            ServiceLocator.Get<DialogueManager>().OnDialogueEnded -= this.OnDialogueEndedHandler;
        }

        private void StartDialogue()
        {
            this.isDialogueOpen = true;
            // 回退：如果未找到配置，创建默认配置
            if (this.cachedProfile == null)
            {
                this.cachedProfile = ScriptableObject.CreateInstance<NPCPromptProfile>();
                this.cachedProfile.npcName = this.gameObject.name;
                this.cachedProfile.npcRole = "村民";
                this.cachedProfile.personalityDescription = "友善的村民";
                this.cachedProfile.speakingStyle = "说话简洁";
            }

            // 用 GameObject 名称覆盖 profile 中的 NPC 名称
            if (!string.IsNullOrEmpty(this.gameObject.name))
            {
                this.cachedProfile.npcName = this.gameObject.name;
            }

            ServiceLocator.Get<DialogueManager>().RegisterDialogueWorker(this.npcId, this.GetComponent<AWorker>());
            ServiceLocator.Get<DialogueManager>().StartDialogue(this.npcId, this.cachedProfile);
            ServiceLocator.Get<PanelController>().Show(DialoguePanel.Instance);
            DialoguePanelUI dialoguePanelUI = DialoguePanelUI.Ensure();
            if (dialoguePanelUI == null)
            {
                LogProvider(
                    "NPCDialogueTrigger: DialoguePanelUI is missing from Game.unity.",
                    LogManager.LogLevelEnum.Error);
                ServiceLocator.Get<DialogueManager>().EndDialogue(this.npcId);
                this.isDialogueOpen = false;
                return;
            }

            dialoguePanelUI.Open(this.npcId, this.cachedProfile);

            ServiceLocator.Get<DialogueManager>().OnDialogueEnded += this.OnDialogueEndedHandler;
        }

        private void OnDialogueEndedHandler(string npcId)
        {
            if (npcId == this.npcId)
            {
                this.isDialogueOpen = false;
                ServiceLocator.Get<DialogueManager>().OnDialogueEnded -= this.OnDialogueEndedHandler;
            }
        }

        private static NPCPromptProfile CreateRuntimeProfile(NPCPromptProfile profile)
        {
            return profile == null ? null : UnityEngine.Object.Instantiate(profile);
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(this.transform.position, this.interactionRadius);
        }
#endif
    }
}
