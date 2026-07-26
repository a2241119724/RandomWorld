namespace LAB2D.UnityAdapter
{
    using LAB2D.Domain.Common;
    using LAB2D.Domain.Player;
    using UnityEngine;

    /// <summary>
    /// IPlayerView 的 Unity 实现。
    /// 封装 Animator、Rigidbody2D、SpriteRenderer、Camera 等 Unity 表现组件的操作，
    /// 使 Player.cs 不再直接操作这些组件。
    ///
    /// 由 Player.Start() 创建并通过构造函数注入所有需要的组件引用。
    /// </summary>
    public sealed class PlayerViewAdapter : IPlayerView
    {
        private readonly Animator animator;
        private readonly Rigidbody2D rb;
        private readonly SpriteRenderer spriteRenderer;
        private readonly Transform transform;
        private readonly CameraMove mainCamera;
        private readonly CameraMove miniCamera;
        private readonly LAB2D.Character.Character playerIdentity;
        private readonly Color originalColor;

        private float hitFlashTimer = -1f;

        /// <summary>
        /// 创建 PlayerViewAdapter 实例。
        /// </summary>
        /// <param name="animator">玩家 Animator 组件。</param>
        /// <param name="rb">玩家 Rigidbody2D 组件。</param>
        /// <param name="spriteRenderer">玩家 SpriteRenderer 组件。</param>
        /// <param name="transform">玩家 Transform 组件。</param>
        /// <param name="mainCamera">主摄像机 CameraMove 组件。</param>
        /// <param name="miniCamera">小地图摄像机 CameraMove 组件。</param>
        /// <param name="playerIdentity">玩家实例引用，用于摄像机跟随的去重判断。</param>
        /// <param name="originalColor">受击闪烁恢复的原始颜色。</param>
        public PlayerViewAdapter(
            Animator animator,
            Rigidbody2D rb,
            SpriteRenderer spriteRenderer,
            Transform transform,
            CameraMove mainCamera,
            CameraMove miniCamera,
            LAB2D.Character.Character playerIdentity,
            Color originalColor)
        {
            this.animator = animator;
            this.rb = rb;
            this.spriteRenderer = spriteRenderer;
            this.transform = transform;
            this.mainCamera = mainCamera;
            this.miniCamera = miniCamera;
            this.playerIdentity = playerIdentity;
            this.originalColor = originalColor;
        }

        /// <inheritdoc/>
        public void ApplyMoveAnimation(PlayerMoveCommand command, PlayerMoveResult moveResult)
        {
            this.animator.SetInteger("Action", command.IsRunning ? 1 : 0);

            int animDir;
            if (command.Direction.Y > 0)
            {
                animDir = 0;
            }
            else if (command.Direction.X > 0)
            {
                animDir = 1;
            }
            else if (command.Direction.Y < 0)
            {
                animDir = 2;
            }
            else
            {
                animDir = 3;
            }

            this.animator.SetInteger("Direction", animDir);
            this.rb.velocity = new Vector2(moveResult.Velocity.X, moveResult.Velocity.Y);
        }

        /// <inheritdoc/>
        public void ApplyIdleAnimation()
        {
            this.animator.SetInteger("Action", 2);
            this.rb.velocity = Vector3.zero;
        }

        /// <inheritdoc/>
        public void EnsureCameraFollow(GameVector2 position)
        {
            Vector3 worldPos = new Vector3(position.X, position.Y, 0f);

            if (this.mainCamera != null && this.mainCamera.Character != this.playerIdentity)
            {
                this.mainCamera.DirectToPosition(worldPos);
                this.mainCamera.Character = this.playerIdentity;
            }

            if (this.miniCamera != null && this.miniCamera.Character != this.playerIdentity)
            {
                this.miniCamera.DirectToPosition(worldPos);
                this.miniCamera.Character = this.playerIdentity;
            }
        }

        /// <inheritdoc/>
        public void PlayHitFlash()
        {
            this.spriteRenderer.color = Color.red;
            this.hitFlashTimer = 0.2f;
        }

        /// <inheritdoc/>
        public void Tick(float deltaTime)
        {
            // 受击闪烁恢复
            if (this.hitFlashTimer > 0f)
            {
                this.hitFlashTimer -= deltaTime;
                if (this.hitFlashTimer <= 0f)
                {
                    this.spriteRenderer.color = this.originalColor;
                }
            }

            // 边缘特效
            if (this.spriteRenderer.sprite != null)
            {
                this.spriteRenderer.material.SetTexture("_MainTex", this.spriteRenderer.sprite.texture);
            }
        }

        /// <inheritdoc/>
        public void TogglePerspective(bool is2_5D)
        {
            float rotationX = 0f;
            if (is2_5D)
            {
                rotationX = -45f;
            }

            this.transform.rotation = Quaternion.Euler(rotationX, this.transform.rotation.y, this.transform.rotation.z);
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                mainCam.transform.rotation = Quaternion.Euler(new Vector3(rotationX, 0f, 0f));
            }

            if (is2_5D)
            {
                if (mainCam != null)
                {
                    mainCam.orthographic = false;
                    mainCam.fieldOfView = 100;
                }

                this.mainCamera.Offset = new Vector3(0f, -6f, 14f);
            }
            else
            {
                if (mainCam != null)
                {
                    mainCam.orthographic = true;
                    mainCam.orthographicSize = 10;
                }

                this.mainCamera.Offset = new Vector3(0f, 0f, 0f);
            }
        }
    }
}
