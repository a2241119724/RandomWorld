namespace LAB2D
{
    using UnityEngine;

    /// <summary>
    /// Translates Unity Input (keyboard, mouse, joystick) into domain IGameCommand instances.
    /// Called from Player.Update() or similar MonoBehaviour update loop.
    ///
    /// Usage in Player.Update():
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
        // Movement keys
        private const KeyCode MoveUp = KeyCode.W;
        private const KeyCode MoveDown = KeyCode.S;
        private const KeyCode MoveLeft = KeyCode.A;
        private const KeyCode MoveRight = KeyCode.D;
        private const KeyCode RunKey = KeyCode.LeftShift;

        // Action keys
        private const KeyCode AttackKey = KeyCode.J;
        private const int AttackMouseButton = 0;

        // Skill hotkeys (matching SkillTool.GetHotkeyDisplayText)
        private static readonly KeyCode[] SkillHotkeys =
        {
            InputKeyConstant.SkillHotkey1,  // Q
            InputKeyConstant.SkillHotkey2,  // E
            InputKeyConstant.SkillHotkey3,  // R
            InputKeyConstant.SkillHotkey4,  // F
        };

        /// <summary>
        /// Poll movement input and create a PlayerMoveCommand.
        /// Returns null when no movement input is active.
        /// </summary>
        /// <param name="entityId">Player entity ID.</param>
        /// <param name="deltaTime">Time.deltaTime from the calling MonoBehaviour.</param>
        /// <returns>A PlayerMoveCommand or null.</returns>
        public static PlayerMoveCommand PollMoveCommand(long entityId, float deltaTime)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (Input.GetKey(MoveRight) || Input.GetKey(KeyCode.D)) horizontal += 1f;
            if (Input.GetKey(MoveLeft) || Input.GetKey(KeyCode.A)) horizontal -= 1f;
            if (Input.GetKey(MoveUp) || Input.GetKey(KeyCode.W)) vertical += 1f;
            if (Input.GetKey(MoveDown) || Input.GetKey(KeyCode.S)) vertical -= 1f;

            // Also support arrow keys
            if (Input.GetKey(KeyCode.RightArrow)) horizontal += 1f;
            if (Input.GetKey(KeyCode.LeftArrow)) horizontal -= 1f;
            if (Input.GetKey(KeyCode.UpArrow)) vertical += 1f;
            if (Input.GetKey(KeyCode.DownArrow)) vertical -= 1f;

            if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f))
            {
                return null;
            }

            // Clamp diagonal movement
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
        /// Check for attack input. Returns true and creates command when attack is pressed.
        /// </summary>
        /// <param name="entityId">Player entity ID.</param>
        /// <param name="command">Output attack command.</param>
        /// <returns>True when attack is triggered this frame.</returns>
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
        /// Check for skill activation input. Returns true and creates command when skill hotkey is pressed.
        /// </summary>
        /// <param name="entityId">Player entity ID.</param>
        /// <param name="slotIndex">Skill slot index (0-3).</param>
        /// <param name="command">Output skill command.</param>
        /// <returns>True when skill is activated this frame.</returns>
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
