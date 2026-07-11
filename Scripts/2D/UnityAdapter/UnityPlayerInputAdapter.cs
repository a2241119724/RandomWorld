namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// 将 Unity 输入（键盘、鼠标、摇杆）转换为领域 IGameCommand 实例。
    /// 从 Player.Update() 或类似的 MonoBehaviour 更新循环中调用。
    ///
    /// 在 Player.Update() 中的用法：
    ///   var cmd = UnityPlayerInputAdapter.PollMoveCommand(entityId, Time.deltaTime);
    ///   if (cmd != null) movementService.ProcessMove(cmd);
    ///
    ///   if (UnityPlayerInputAdapter.GetAttackDown(entityId, out var attackCmd))
    ///       combatService.ProcessAttack(attackCmd);
    ///
    ///   if (UnityPlayerInputAdapter.GetSkillDown(entityId, 0, out var skillCmd))
    ///       skillService.ActivateSkill(skillCmd);
    /// </summary>
    public static class UnityPlayerInputAdapter
    {
        // 移动按键
        private const KeyCode MoveUp = KeyCode.W;
        private const KeyCode MoveDown = KeyCode.S;
        private const KeyCode MoveLeft = KeyCode.A;
        private const KeyCode MoveRight = KeyCode.D;
        private const KeyCode RunKey = KeyCode.LeftShift;

        // 动作按键
        private const KeyCode AttackKey = KeyCode.J;
        private const int AttackMouseButton = 0;

        // 技能快捷键（与 SkillTool.GetHotkeyDisplayText 保持一致）
        private static readonly KeyCode[] SkillHotkeys =
        {
            InputKeyConstant.SkillHotkey1,  // Q
            InputKeyConstant.SkillHotkey2,  // E
            InputKeyConstant.SkillHotkey3,  // R
            InputKeyConstant.SkillHotkey4,  // F
        };

        /// <summary>
        /// 轮询移动输入并创建 PlayerMoveCommand。
        /// 没有移动输入时返回 null。
        /// </summary>
        /// <param name="entityId">玩家实体 ID。</param>
        /// <param name="deltaTime">来自调用方 MonoBehaviour 的 Time.deltaTime。</param>
        /// <returns>PlayerMoveCommand 或 null。</returns>
        public static PlayerMoveCommand PollMoveCommand(long entityId, float deltaTime)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(MoveRight) || Input.GetKey(KeyCode.D)) horizontal += 1f;
            if (Input.GetKey(MoveLeft) || Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (Input.GetKey(MoveUp) || Input.GetKey(KeyCode.W)) vertical += 1f;
            if (Input.GetKey(MoveDown) || Input.GetKey(KeyCode.S)) vertical -= 1f;

            // 同时支持方向键
            if (Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;

            if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
            {
                return null;
            }

            // 限制对角线移动
            GameVector2 direction = new GameVector2(
                Mathf.Clamp(horizontal, -1f, 1f),
                Mathf.Clamp(vertical, -1f, 1f));

            return new PlayerMoveCommand
            {
                EntityId = entityId,
                Direction = direction,
                IsRunning = Input.GetKey(RunKey),
                DeltaTime = deltaTime,
            };
        }

        /// <summary>
        /// 检测攻击输入。当攻击按键按下时返回 true 并创建命令。
        /// </summary>
        /// <param name="entityId">玩家实体 ID。</param>
        /// <param name="command">输出的攻击命令。</param>
        /// <returns>本帧触发攻击时返回 true。</returns>
        public static bool GetAttackDown(long entityId, out PlayerAttackCommand command)
        {
            command = null;
            if (Input.GetKeyDown(AttackKey) || Input.GetMouseButtonDown(AttackMouseButton))
            {
                command = new PlayerAttackCommand { EntityId = entityId };
                return true;
            }

            return false;
        }

        /// <summary>
        /// 检测技能激活输入。当技能快捷键按下时返回 true 并创建命令。
        /// </summary>
        /// <param name="entityId">玩家实体 ID。</param>
        /// <param name="slotIndex">技能槽位索引（0-3）。</param>
        /// <param name="command">输出的技能命令。</param>
        /// <returns>本帧激活技能时返回 true。</returns>
        public static bool GetSkillDown(long entityId, int slotIndex, out ActivateSkillCommand command)
        {
            command = null;
            if (slotIndex < 0 || slotIndex >= SkillHotkeys.Length)
            {
                return false;
            }

            if (Input.GetKeyDown(SkillHotkeys[slotIndex]))
            {
                command = new ActivateSkillCommand
                {
                    EntityId = entityId,
                    SlotIndex = slotIndex,
                };
                return true;
            }

            return false;
        }
    }
}
