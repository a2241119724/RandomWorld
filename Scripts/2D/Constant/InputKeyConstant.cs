namespace LAB2D.Constant
{
    using LAB2D;
    using UnityEngine;

    /// <summary>
    /// 输入按键常量
    /// 集中管理游戏中所有按键绑定，按功能分区组织。
    /// 修改任何按键值会影响对应功能的热键响应，请在 Play Mode 中验证。
    /// </summary>
    public static class InputKeyConstant
    {
        #region 玩家移动

        /// <summary>
        /// 玩家向左移动 (A键)
        /// 配合 Input.GetAxisRaw("Horizontal") 提供负方向输入。
        /// 摇杆输入为备选控制方式。
        /// </summary>
        public const KeyCode MoveLeft = KeyCode.A;

        /// <summary>
        /// 玩家向右移动 (D键)
        /// 配合 Input.GetAxisRaw("Horizontal") 提供正方向输入。
        /// 摇杆输入为备选控制方式。
        /// </summary>
        public const KeyCode MoveRight = KeyCode.D;

        /// <summary>
        /// 玩家向上移动 (W键)
        /// 配合 Input.GetAxisRaw("Vertical") 提供正方向输入。
        /// 摇杆输入为备选控制方式。
        /// </summary>
        public const KeyCode MoveUp = KeyCode.W;

        /// <summary>
        /// 玩家向下移动 (S键)
        /// 配合 Input.GetAxisRaw("Vertical") 提供负方向输入。
        /// 摇杆输入为备选控制方式。
        /// </summary>
        public const KeyCode MoveDown = KeyCode.S;

        /// <summary>
        /// 玩家奔跑键 (左Shift)
        /// 按住Shift时玩家移动速度切换为奔跑状态（Action=1），松开恢复行走（Action=0）。
        /// 仅在移动中生效。
        /// </summary>
        public const KeyCode Run = KeyCode.LeftShift;

        #endregion

        #region 通用面板操作

        /// <summary>
        /// 关闭当前面板 / 打开建造菜单 (Esc键)
        /// 当无面板打开时显示建造菜单；有面板时返回上一级面板。
        /// 同时用于隐藏 WorkerBedUI 和 AddWearTaskUI 等操作面板。
        /// 仅在无 UI 输入框聚焦时生效。
        /// </summary>
        public const KeyCode CloseOrBuildMenu = KeyCode.Escape;

        /// <summary>
        /// 显示地块信息提示 (左Ctrl键，按住)
        /// 按住左Ctrl时鼠标悬停地块可显示地块类型名称。
        /// 松开左Ctrl键时隐藏提示信息。
        /// 仅在无 UI 输入框聚焦时生效。
        /// </summary>
        public const KeyCode ShowTileInfo = KeyCode.LeftControl;

        #endregion

        #region HUD 显示/隐藏切换 (F1-F8功能键)

        /// <summary>
        /// 游戏统计 HUD 显示/隐藏 (F1键)
        /// 切换显示波次计数、击杀数、游戏时间等实时统计数据。
        /// 仅在无 UI 输入框聚焦时生效。
        /// </summary>
        public const KeyCode ToggleGameplayStatsHud = KeyCode.F1;

        /// <summary>
        /// 体验中枢 HUD 显示/隐藏 (F2键)
        /// 切换显示当前经验值、等级、属性加成等体验中枢核心信息。
        /// 仅在无 UI 输入框聚焦时生效。
        /// </summary>
        public const KeyCode ToggleExperienceHubHud = KeyCode.F2;

        /// <summary>
        /// 体验中枢结算/预览面板 显示/隐藏 (F3键)
        /// 切换显示最近波次的经验结算详情或实时预览。
        /// 仅在无 UI 输入框聚焦时生效。
        /// </summary>
        public const KeyCode ToggleExperienceResultPanel = KeyCode.F3;

        /// <summary>
        /// 天气玩法 HUD 显示/隐藏 (F4键)
        /// 切换显示当前天气类型、效果描述和持续时间。
        /// 仅在无 UI 输入框聚焦时生效。
        /// </summary>
        public const KeyCode ToggleWeatherHud = KeyCode.F4;

        /// <summary>
        /// 工人状态 HUD 显示/隐藏 (F5键)
        /// 切换显示工人的饥饿值、疲劳值及对应的移动/工作效率惩罚。
        /// 仅在无 UI 输入框聚焦时生效。
        /// </summary>
        public const KeyCode ToggleWorkerConditionHud = KeyCode.F5;

        /// <summary>
        /// 工人补给缺口 HUD 显示/隐藏 (F6键)
        /// 切换显示工人食物与床位补给缺口分析及解决建议。
        /// 仅在无 UI 输入框聚焦时生效。
        /// </summary>
        public const KeyCode ToggleWorkerSupplyHud = KeyCode.F6;

        /// <summary>
        /// 工人任务队列 HUD + 成就面板 显示/隐藏 (数字6键)
        /// 同时切换工人任务队列 HUD 和成就系统的成就列表面板。
        /// 两者分别通过 CanUseHotkey() 和 LAB2D.Tool.Tool.IsUIInputActive() 守卫控制。
        /// 仅在无 UI 输入框聚焦时生效。
        /// </summary>
        public const KeyCode ToggleWorkerTaskAndAchievementHud = KeyCode.Alpha9;

        /// <summary>
        /// 工人任务队列 HUD 显示/隐藏 (F10键)
        /// 单独切换工人任务队列 HUD 的显示和隐藏。
        /// 仅在无 UI 输入框聚焦时生效。
        /// </summary>
        public const KeyCode ToggleWorkerTaskQueueHud = KeyCode.F10;

        /// <summary>
        /// 任务栏列表 HUD 显示/隐藏 (F7键)
        /// 切换显示任务栏中存放的物品列表和所属信息。
        /// 仅在无 UI 输入框聚焦时生效。
        /// </summary>
        public const KeyCode ToggleTaskBoardHud = KeyCode.F7;

        /// <summary>
        /// 殖民地指挥中心 HUD 显示/隐藏 (F9键)
        /// 切换显示殖民地运营综合摘要、任务阻塞预警和资源状态。
        /// 仅在无 UI 输入框聚焦时生效。
        /// </summary>
        public const KeyCode ToggleColonyCommandCenterHud = KeyCode.F9;

        #endregion

        #region 主动技能快捷键 (Q/E/R/F)

        /// <summary>
        /// 主动技能槽位1快捷键 (Q键)
        /// 激活技能栏第1个技能（默认：旋风斩）。
        /// 仅在非UI输入模式下生效，且技能冷却就绪、法力充足时激活。
        /// </summary>
        public const KeyCode SkillHotkey1 = KeyCode.Q;

        /// <summary>
        /// 主动技能槽位2快捷键 (E键)
        /// 激活技能栏第2个技能（默认：冲刺）。
        /// </summary>
        public const KeyCode SkillHotkey2 = KeyCode.E;

        /// <summary>
        /// 主动技能槽位3快捷键 (R键)
        /// 激活技能栏第3个技能（默认：力量爆发）。
        /// </summary>
        public const KeyCode SkillHotkey3 = KeyCode.R;

        /// <summary>
        /// 主动技能槽位4快捷键 (F键)
        /// 激活技能栏第4个技能（默认：治疗之光）。
        /// </summary>
        public const KeyCode SkillHotkey4 = KeyCode.F;

        #endregion

        #region 快捷选择 (数字键 1-9)

        /// <summary>
        /// 工具菜单快捷选择 - 第1项 (数字1键)
        /// 在前景面板中打开或关闭工具菜单第1个功能按钮对应的面板。
        /// </summary>
        public const KeyCode ToolMenuSlot1 = KeyCode.Alpha1;

        /// <summary>
        /// 工具菜单快捷选择 - 第2项 (数字2键)
        /// </summary>
        public const KeyCode ToolMenuSlot2 = KeyCode.Alpha2;

        /// <summary>
        /// 工具菜单快捷选择 - 第3项 (数字3键)
        /// </summary>
        public const KeyCode ToolMenuSlot3 = KeyCode.Alpha3;

        /// <summary>
        /// 工具菜单快捷选择 - 第4项 (数字4键)
        /// </summary>
        public const KeyCode ToolMenuSlot4 = KeyCode.Alpha4;

        /// <summary>
        /// 工具菜单快捷选择 - 第5项 (数字5键)
        /// </summary>
        public const KeyCode ToolMenuSlot5 = KeyCode.Alpha5;

        /// <summary>
        /// 工具菜单快捷选择 - 第6项 (数字6键)
        /// </summary>
        public const KeyCode ToolMenuSlot6 = KeyCode.Alpha6;

        /// <summary>
        /// 工具菜单快捷选择 - 第7项 (数字7键)
        /// </summary>
        public const KeyCode ToolMenuSlot7 = KeyCode.Alpha7;

        /// <summary>
        /// 工具菜单快捷选择 - 第8项 (数字8键)
        /// </summary>
        public const KeyCode ToolMenuSlot8 = KeyCode.Alpha8;

        /// <summary>
        /// 工具菜单快捷选择 - 第9项 (数字9键)
        /// </summary>
        public const KeyCode ToolMenuSlot9 = KeyCode.Alpha9;

        /// <summary>
        /// 工具菜单所有快捷选择键（按菜单顺序排列，索引 0-8 对应 Alpha1-Alpha9）
        /// </summary>
        public static readonly KeyCode[] ToolMenuKeys =
        {
            KeyCode.Alpha1, KeyCode.Alpha2,
            KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7,
            KeyCode.Alpha8, KeyCode.Alpha9,
        };

        /// <summary>
        /// 波间奖励选项 - 第1项 (数字1键)
        /// 在波间奖励选择面板中选取第1个强化选项。
        /// 面板打开时生效，不与工具菜单的 Alpha1 冲突。
        /// </summary>
        public const KeyCode BossRewardOption1 = KeyCode.Alpha1;

        /// <summary>
        /// 波间奖励选项 - 第2项 (数字2键)
        /// 在波间奖励选择面板中选取第2个强化选项。
        /// </summary>
        public const KeyCode BossRewardOption2 = KeyCode.Alpha2;

        /// <summary>
        /// 波间奖励选项 - 第3项 (数字3键)
        /// 在波间奖励选择面板中选取第3个强化选项。
        /// </summary>
        public const KeyCode BossRewardOption3 = KeyCode.Alpha3;

        #endregion

        #region 按键功能摘要

        /// <summary>
        /// 所有按键功能的格式化摘要文本。
        /// 可在设置界面、帮助面板或调试日志中使用。
        /// 修改按键值后记得同步更新此处的显示文本。
        /// </summary>
        public static string GetKeyBindingSummary()
        {
            return @"━━━ 按键操作说明 ━━━

【玩家移动】
  W/A/S/D  —— 上下左右移动
  Shift    —— 按住奔跑（移动中生效）
  摇杆      —— 备选移动方式

【通用操作】
  Esc      —— 关闭面板 / 打开建造菜单
  左Ctrl   —— 按住查看地块信息

【主动技能】
  Q        —— 技能槽位1 (旋风斩)
  E        —— 技能槽位2 (冲刺)
  R        —— 技能槽位3 (力量爆发)
  F        —— 技能槽位4 (治疗之光)

【HUD 切换 (F1-F7, F9-F10, 数字键)】
  F1       —— 游戏统计面板 (波次/击杀/时间)
  F2       —— 体验中枢 HUD (经验/等级/属性)
  F3       —— 体验中枢结算预览面板
  F4       —— 天气玩法 HUD (天气类型/效果)
  F5       —— 工人状态 HUD (饥饿/疲劳)
  F6       —— 工人补给缺口 HUD (食物/床位)
  F7       —— 任务栏列表 HUD
  F9       —— 殖民地指挥中心 HUD
  F10      —— 工人任务队列 HUD
  6        —— 房间列表面板
  9        —— 成就面板
  0        —— 装备面板切换

【快捷选择 (数字键)】
  1-9      —— 工具菜单快捷切换
  1/2/3    —— 波间奖励选项选择

【鼠标操作】
  左键     —— 攻击 / 选择建造 / 关闭信息
  右键     —— 物品详情 / 取消操作
  中键     —— 拖拽移动镜头 / 关闭信息
  滚轮     —— 缩放视野";
        }

        #endregion
    }
}
