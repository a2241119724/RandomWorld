namespace Photon.Pun
{
    using Photon.Realtime;
    using UnityEngine;

    /// <summary>定义OnPhotonSerializeView方法，使可观察脚本能够轻松正确地实现同步。</summary>
    /// \ingroup callbacks
    public interface IPunObservable
    {
        /// <summary>
        /// PUN每秒调用多次，使你的脚本可以写入和读取PhotonView的同步数据。
        /// </summary>
        /// <remarks>
        /// 此方法将在被指定为PhotonView的Observed组件的脚本中调用。<br/>
        /// PhotonNetwork.SerializationRate影响此方法被调用的频率。<br/>
        /// PhotonNetwork.SendRate影响此客户端发送数据包的频率。<br/>
        ///
        /// 实现此方法，你可以自定义PhotonView定期同步的数据。
        /// 你的代码定义了发送的内容以及接收客户端如何使用数据。
        ///
        /// 与其他回调不同，<i>OnPhotonSerializeView仅在它被分配给
        /// PhotonView作为PhotonView.observed脚本时才会被调用</i>。
        ///
        /// 要使用此方法，PhotonStream是必不可少的。它在控制PhotonView的客户端上
        /// 处于"写入模式"(PhotonStream.IsWriting == true)，在仅接收控制客户端发送的
        /// 数据的远程客户端上处于"读取模式"。
        ///
        /// 如果你跳过向流中写入任何值，PUN将跳过此次更新。谨慎使用可以
        /// 节省带宽和消息（每个房间每秒有限额）。
        ///
        /// 请注意，当发送方不发送任何更新时，远程客户端不会调用OnPhotonSerializeView。
        /// 这不能用作"每秒x次的Update()"。
        /// </remarks>
        /// \ingroup publicApi
        void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info);
    }


    /// <summary>
    /// 所有权变更的全局回调接口。这些回调会在任何PhotonView发生变更时触发。
    /// 如果只需要监听特定PhotonView的回调，请考虑使用IOnPhotonViewControllerChange。
    /// </summary>
    public interface IPunOwnershipCallbacks
    {
        /// <summary>
        /// 当另一个玩家请求PhotonView的所有权时调用。
        /// 在所有客户端上调用，因此请检查(targetView.IsMine)或(targetView.Owner == PhotonNetwork.LocalPlayer)
        /// 来确定是否应该响应targetView.TransferOwnership(requestingPlayer)。
        /// </summary>
        /// <remarks>
        /// 参数viewAndPlayer包含：
        ///
        /// PhotonView view = viewAndPlayer[0] as PhotonView;
        ///
        /// Player requestingPlayer = viewAndPlayer[1] as Player;
        /// </remarks>
        /// <param name="targetView">被请求所有权的PhotonView。</param>
        /// <param name="requestingPlayer">请求所有权的玩家。</param>
        void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer);

        /// <summary>
        /// 当PhotonView的所有权转移给另一个玩家时调用。
        /// </summary>
        /// <remarks>
        /// 参数viewAndPlayers包含：
        ///
        /// PhotonView view = viewAndPlayers[0] as PhotonView;
        ///
        /// Player newOwner = viewAndPlayers[1] as Player;
        ///
        /// Player oldOwner = viewAndPlayers[2] as Player;
        /// </remarks>
        /// <example>void OnOwnershipTransfered(object[] viewAndPlayers) {} //</example>
        /// <param name="targetView">所有权变更的PhotonView。</param>
        /// <param name="previousOwner">之前的所有者（如果没有则为null）。</param>
        void OnOwnershipTransfered(PhotonView targetView, Player previousOwner);

        /// <summary>
        /// 当具有"takeover"设置的对象的所有权请求失败时调用。
        /// </summary>
        /// <remarks>
        /// 每个请求都要求从特定的控制玩家那里获取所有权。如果在此请求到达之前
        /// 其他人短暂地获取了所有权，则请求可能会失败。
        /// </remarks>
        /// <param name="targetView"></param>
        /// <param name="senderOfFailedRequest"></param>
        void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest);
    }

    /// \ingroup callbacks
    public interface IPunInstantiateMagicCallback
    {
        void OnPhotonInstantiate(PhotonMessageInfo info);
    }

    /// <summary>
    /// 定义对象池接口，用于PhotonNetwork.Instantiate和PhotonNetwork.Destroy。
    /// </summary>
    /// <remarks>
    /// 要应用你的自定义IPunPrefabPool，请设置PhotonNetwork.PrefabPool。
    ///
    /// 当PUN调用Instantiate时，对象池必须返回一个有效的、已禁用的GameObject。
    /// 此外，位置和旋转必须被应用。
    ///
    /// 请注意，Unity只在第一次调用Awake和Start，因此被复用的GameObject上的脚本
    /// 应使用OnEnable和/或OnDisable。当OnEnable被调用时，PhotonView
    /// 已经更新为新的值。
    ///
    /// 为了能够启用一个GameObject，Instantiate必须返回一个非活动状态的对象。
    ///
    /// 在PUN"销毁"GameObject之前，它会先禁用它们。
    ///
    /// 如果一个组件实现了IPunInstantiateMagicCallback，当网络对象被实例化时，
    /// PUN会调用OnPhotonInstantiate。如果预制体上没有组件实现此接口，
    /// PUN将优化实例化过程，不再通过GetComponents查找IPunInstantiateMagicCallback。
    /// </remarks>
    public interface IPunPrefabPool
    {
        /// <summary>
        /// 调用以获取预制体的实例。必须返回带有PhotonView的有效、已禁用的GameObject。
        /// </summary>
        /// <param name="prefabId">此预制体的ID。</param>
        /// <param name="position">实例的位置。</param>
        /// <param name="rotation">实例的旋转。</param>
        /// <returns>供PUN使用的已禁用实例，如果prefabId未知则返回null。</returns>
        GameObject Instantiate(string prefabId, Vector3 position, Quaternion rotation);

        /// <summary>
        /// 调用以销毁（或仅归还）预制体的实例。实例被禁用后，对象池可以重置并缓存它以供后续Instantiate使用。
        /// </summary>
        /// <remarks>
        /// 对象池需要某种方式来确定通过Destroy()归还的是哪种类型的GameObject。
        /// 可以使用标签、名称、组件或任何类似的方式。
        /// </remarks>
        /// <param name="gameObject">要销毁的实例。</param>
        void Destroy(GameObject gameObject);
    }
}