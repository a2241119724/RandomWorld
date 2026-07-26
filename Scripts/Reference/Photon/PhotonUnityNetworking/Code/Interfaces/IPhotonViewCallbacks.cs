namespace Photon.Pun
{
    using Photon.Realtime;

    /// <summary>
    /// 所有PhotonView回调的基类接口。
    /// </summary>
    public interface IPhotonViewCallback
    {

    }

    /// <summary>
    /// 此接口定义了一个回调，在PhotonNetwork销毁PhotonView和GameObject之前触发。
    /// </summary>
    public interface IOnPhotonViewPreNetDestroy : IPhotonViewCallback
    {
        /// <summary>
        /// 在网络对象发起Destroy()之前调用此方法。
        /// </summary>
        /// <param name="rootView"></param>
        void OnPreNetDestroy(PhotonView rootView);
    }

    /// <summary>
    /// 此接口定义了PhotonView所有者变更的回调。
    /// </summary>
    public interface IOnPhotonViewOwnerChange : IPhotonViewCallback
    {
        /// <summary>
        /// 当PhotonView的所有者变更时调用此方法。
        /// </summary>
        /// <param name="newOwner"></param>
        /// <param name="previousOwner"></param>
        void OnOwnerChange(Player newOwner, Player previousOwner);
    }

    /// <summary>
    /// 此接口定义了PhotonView控制器变更的回调。
    /// </summary>
    public interface IOnPhotonViewControllerChange : IPhotonViewCallback
    {
        /// <summary>
        /// 当PhotonView的控制器变更时调用此方法。
        /// </summary>
        /// <param name="newOwner"></param>
        /// <param name="previousOwner"></param>
        void OnControllerChange(Player newController, Player previousController);
    }
}