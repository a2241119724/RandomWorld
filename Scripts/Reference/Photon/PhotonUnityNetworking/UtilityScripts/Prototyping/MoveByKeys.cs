// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OnJoinedInstantiate.cs" company="Exit Games GmbH">
//   Part of: Photon Unity Utilities,
// </copyright>
// <summary>
//  使用WASD和空格键移动GameObject的非常基础的组件。
// </summary>
// <remarks>
// 需要PhotonView。
// 在Start时对不属于自己的GameObject禁用自身。
//
// Speed影响移动速度。
// JumpForce定义对象"跳跃"的高度。
// JumpTimeout定义多少秒后可以再次跳跃。
// </remarks>
// <author>developer@exitgames.com</author>
// --------------------------------------------------------------------------------------------------------------------


using UnityEngine;

namespace Photon.Pun.UtilityScripts
{

    /// <summary>
    /// 使用WASD和空格键移动GameObject的非常基础的组件。
    /// </summary>
    /// <remarks>
    /// 需要PhotonView。
    /// 在Start时对不属于自己的GameObject禁用自身。
    ///
    /// Speed影响移动速度。
    /// JumpForce定义对象"跳跃"的高度。
    /// JumpTimeout定义多少秒后可以再次跳跃。
    /// </remarks>
    [RequireComponent(typeof(PhotonView))]
    public class MoveByKeys : Photon.Pun.MonoBehaviourPun
    {
        public float Speed = 10f;
        public float JumpForce = 200f;
        public float JumpTimeout = 0.5f;

        private bool isSprite;
        private float jumpingTime;
        private Rigidbody body;
        private Rigidbody2D body2d;

        public void Start()
        {
            //enabled = photonView.isMine;
            this.isSprite = (GetComponent<SpriteRenderer>() != null);

            this.body2d = GetComponent<Rigidbody2D>();
            this.body = GetComponent<Rigidbody>();
        }


        // Update is called once per frame
        public void FixedUpdate()
        {
            if (!pv.IsMine)
            {
                return;
            }

            if ((Input.GetAxisRaw("Horizontal") < -0.1f) || (Input.GetAxisRaw("Horizontal") > 0.1f))
            {
                transform.position += Vector3.right * (Speed * Time.deltaTime) * Input.GetAxisRaw("Horizontal");
            }

            // 跳跃有一个简单的"冷却"时间，但你也可以在空中跳跃
            if (this.jumpingTime <= 0.0f)
            {
                if (this.body != null || this.body2d != null)
                {
                    // 对象有Rigidbody可以跳跃（使用AddForce）
                    if (Input.GetKey(KeyCode.Space))
                    {
                        this.jumpingTime = this.JumpTimeout;

                        Vector2 jump = Vector2.up * this.JumpForce;
                        if (this.body2d != null)
                        {
                            this.body2d.AddForce(jump);
                        }
                        else if (this.body != null)
                        {
                            this.body.AddForce(jump);
                        }
                    }
                }
            }
            else
            {
                this.jumpingTime -= Time.deltaTime;
            }

            // 2D对象无法在3D"前进"方向上移动
            if (!this.isSprite)
            {
                if ((Input.GetAxisRaw("Vertical") < -0.1f) || (Input.GetAxisRaw("Vertical") > 0.1f))
                {
                    transform.position += Vector3.forward * (Speed * Time.deltaTime) * Input.GetAxisRaw("Vertical");
                }
            }
        }
    }
}