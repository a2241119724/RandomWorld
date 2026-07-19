namespace LAB2D.AI.Dialogue.Core
{
    using LAB2D;
    using LAB2D.AI.Dialogue.Prompt;
    using LAB2D.UnityAdapter;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// NPC 对话触发器，挂载在 NPC GameObject 上，玩家靠近按 T 键开始对话
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
        /// 交互按键
        /// </summary>
        public KeyCode interactKey = KeyCode.T;

        private NPCPromptProfile cachedProfile;
        private string npcId;
        private bool isDialogueOpen;

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
                this.cachedProfile = CreateRuntimeProfile(PromptBuilder.Instance.GetProfile(this.profileName));
            }

            if (this.cachedProfile == null)
            {
                if (!MissingProfileWarned.Contains(this.profileName))
                {
                    MissingProfileWarned.Add(this.profileName);
                    LogManager.Instance.Log(
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

            Player player = PlayerManager.Instance?.Mine;
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
                this.cachedProfile = CreateRuntimeProfile(PromptBuilder.Instance.GetProfile(this.profileName));
            }

            this.StartDialogue();
        }

        public void OnDestroy()
        {
            DialogueManager.Instance.OnDialogueEnded -= this.OnDialogueEndedHandler;
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

            DialogueManager.Instance.RegisterDialogueWorker(this.npcId, this.GetComponent<AWorker>());
            DialogueManager.Instance.StartDialogue(this.npcId, this.cachedProfile);
            PanelController.Instance.Show(DialoguePanel.Instance);
            DialoguePanelUI dialoguePanelUI = DialoguePanelUI.Ensure();
            if (dialoguePanelUI == null)
            {
                LogManager.Instance?.Log(
                    "NPCDialogueTrigger: DialoguePanelUI is missing from Game.unity.",
                    LogManager.LogLevelEnum.Error);
                DialogueManager.Instance.EndDialogue(this.npcId);
                this.isDialogueOpen = false;
                return;
            }

            dialoguePanelUI.Open(this.npcId, this.cachedProfile);

            DialogueManager.Instance.OnDialogueEnded += this.OnDialogueEndedHandler;
        }

        private void OnDialogueEndedHandler(string npcId)
        {
            if (npcId == this.npcId)
            {
                this.isDialogueOpen = false;
                DialogueManager.Instance.OnDialogueEnded -= this.OnDialogueEndedHandler;
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
