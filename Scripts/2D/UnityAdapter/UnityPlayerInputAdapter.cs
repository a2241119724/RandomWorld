namespace LAB2D.UnityAdapter
{
    using LAB2D;
    using LAB2D.Domain.Common;
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
        /// Polls the current Player movement bindings without changing legacy
        /// behavior: UI input blocks movement, keyboard axes take priority, and
        /// joystick direction is used only when keyboard axes are neutral.
        /// </summary>
        public static PlayerMoveCommand PollCurrentPlayerMoveCommand(long entityId, float deltaTime)
        {
            if (LAB2D.Tool.Tool.IsUIInputActive())
            {
                return null;
            }

            bool hasKeyboardMove =
                Input.GetKey(InputKeyConstant.MoveLeft) ||
                Input.GetKey(InputKeyConstant.MoveUp) ||
                Input.GetKey(InputKeyConstant.MoveDown) ||
                Input.GetKey(InputKeyConstant.MoveRight);
            bool hasJoystickMove = Joystick.Instance && Joystick.Instance.Direction.sqrMagnitude > 0.02f;
            if (!hasKeyboardMove && !hasJoystickMove)
            {
                return null;
            }

            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            if (horizontal == 0.0f && vertical == 0.0f && Joystick.Instance != null)
            {
                horizontal = Joystick.Instance.Direction.x;
                vertical = Joystick.Instance.Direction.y;
            }

            return new PlayerMoveCommand
            {
                EntityId = entityId,
                Direction = new GameVector2(horizontal, vertical),
                IsRunning = Input.GetKey(InputKeyConstant.Run),
                DeltaTime = deltaTime,
            };
        }

        /// <summary>
        /// Detects attack input and creates a player attack command.
        /// </summary>
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
        /// Detects the existing player primary attack input without enabling
        /// additional keyboard bindings at the Player call site.
        /// </summary>
        public static bool GetPointerAttackDown(long entityId, out PlayerAttackCommand command)
        {
            command = null;
            if (Input.GetMouseButtonDown(AttackMouseButton))
            {
                command = new PlayerAttackCommand { EntityId = entityId };
                return true;
            }

            return false;
        }

        /// <summary>
        /// Detects skill input for active gameplay, preserving the existing UI
        /// input guard used by Player.
        /// </summary>
        public static bool GetGameplaySkillDown(long entityId, int slotIndex, out ActivateSkillCommand command)
        {
            command = null;
            if (LAB2D.Tool.Tool.IsUIInputActive())
            {
                return false;
            }

            return GetSkillDown(entityId, slotIndex, out command);
        }

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
