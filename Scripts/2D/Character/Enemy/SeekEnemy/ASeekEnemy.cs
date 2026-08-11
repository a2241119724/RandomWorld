namespace LAB2D.Character.Enemy.SeekEnemy
{
    using LAB2D;
    using LAB2D.Character.Enemy.SeekEnemy.State;
    using LAB2D.Constant;
    using LAB2D.Core.Seek;
    using Photon.Pun;
    using UnityEngine;

    public abstract class ASeekEnemy : AEnemy
    {
        private Vector3 direction;

        /// <summary>
        /// 寻路
        /// </summary>
        public ASeek Seek { get; private set; }

        /// <summary>
        /// 获取角色朝向的方向
        /// </summary>
        public override Vector3 Direction
        {
            get
            {
                if (this.Seek.Direction == Vector3.zero)
                {
                    // 攻击方向
                    return this.direction;
                }

                // 寻路方向
                return this.Seek.Direction;
            }

            set
            {
                this.direction = value;
            }
        }

        /// <summary>
        /// 敌人状态管理器.
        /// </summary>
        [HideInInspector]
        public SeekEnemyStateManager<ICharacterState, ASeekEnemyState.TypeEnum, ASeekEnemy> Manager { get; set; }

        /// <inheritdoc/>
        public override void Awake()
        {
            base.Awake();
            this.CharacterDataLAB.Weapon = (AWeapon)AWorkerTask.ItemFactoryProvider(PrefabConstant.CUSTOM_SWORD);
            this.Seek = new AStar(this);
            this.Manager = new SeekEnemyStateManager<ICharacterState, ASeekEnemyState.TypeEnum, ASeekEnemy>(this);
        }

        /// <inheritdoc/>
        public override void Start()
        {
            base.Start();
            this.AttackRange.SetActive(false);
        }

        public void Update()
        {
            // 执行当前状态的函数
            this.Manager.CurrentState.OnUpdate();
        }

        /// <inheritdoc/>
        public override void ReduceHp(float hp, Character attacker, bool isCRT = false)
        {
            if (this.Manager.CurrentStateType != ASeekEnemyState.TypeEnum.Attack ||
                (this.Manager.CurrentStateType == ASeekEnemyState.TypeEnum.Attack
                && ((SeekEnemyAttackState)this.Manager.CurrentState).AttackTime > ChangeTarget))
            {
                this.Manager.ChangeState(ASeekEnemyState.TypeEnum.Move);
            }

            base.ReduceHp(hp, attacker, isCRT);
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
        }

        /// <inheritdoc/>
        public override void ResetState()
        {
            base.ResetState();
            this.Manager.CurrentState.Reset();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return base.ToString() +
                $"Target:{this.Seek.TargetMap}\n" +
                $"SeekId:{this.CharacterDataLAB.SeekId}\n";
        }

        /// <inheritdoc/>
        protected override void Death()
        {
            base.Death();
            this.statusBar.UpdateStatus(this.CharacterDataLAB.Hp, this.CharacterDataLAB.MaxHp);
            if (!this.NetworkView.IsOnline || this.NetworkView.IsMasterClient)
            {
                Core.GameServices.EnemyRemoveProvider(this);
            }

            this.Manager.ChangeState(ASeekEnemyState.TypeEnum.Dead); // 进入死亡状态
        }

        /// <summary>
        /// GameObject 销毁时停止寻路线程，
        /// 防止关闭游戏时后台 ThreadPool 线程访问已销毁对象导致卡死。
        /// </summary>
        protected void OnDestroy()
        {
            this.Seek?.StopMove();

            // 清理 LineRenderer 的材质实例
            if (this.Seek?.LineRenderer != null)
            {
                Material mat = this.Seek.LineRenderer.material;
                if (mat != null)
                {
                    Destroy(mat);
                }
            }
        }
    }
}
