// ----------------------------------------------------------------------------
// <copyright file="Enums.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
// 封装了PUN的多个枚举。
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------


namespace Photon.Pun
{
    /// <summary>调用了哪个PhotonNetwork方法来连接（这会影响我们需要ping的区域）。</summary>
    /// <remarks>PhotonNetwork.ConnectUsingSettings会根据设置调用ConnectToMaster、ConnectToRegion或ConnectToBest。</remarks>
    public enum ConnectMethod { NotCalled, ConnectToMaster, ConnectToRegion, ConnectToBest }


    /// <summary>用于定义PUN类创建的日志输出级别。可以记录错误、信息（更多内容）或全部。</summary>
    /// \ingroup publicApi
    public enum PunLogLevel
    {
        /// <summary>仅显示错误。最小输出。注意：有些可能是你需要预期的"运行时错误"。</summary>
        ErrorsOnly,

        /// <summary>记录一些工作流程、调用和结果。</summary>
        Informational,

        /// <summary>所有可用的日志调用都会输出到控制台/日志。仅用于调试。</summary>
        Full
    }


    /// <summary>RPC的"目标"选项枚举。这些定义了哪些远程客户端会收到你的RPC调用。</summary>
    /// \ingroup publicApi
    public enum RpcTarget
    {
        /// <summary>将RPC发送给其他所有人，并立即在本地客户端执行。后续加入的玩家不会执行此RPC。</summary>
        All,

        /// <summary>将RPC发送给其他所有人。本地客户端不执行此RPC。后续加入的玩家不会执行此RPC。</summary>
        Others,

        /// <summary>仅将RPC发送给MasterClient。注意：MasterClient可能在执行RPC之前断开连接，这可能导致RPC丢失。</summary>
        MasterClient,

        /// <summary>将RPC发送给其他所有人，并立即在本地客户端执行。新玩家加入时会收到此RPC，因为它会被缓冲（直到本地客户端离开）。</summary>
        AllBuffered,

        /// <summary>将RPC发送给所有人。本地客户端不执行此RPC。新玩家加入时会收到此RPC，因为它会被缓冲（直到本地客户端离开）。</summary>
        OthersBuffered,

        /// <summary>通过服务器将RPC发送给所有人（包括本地客户端）。</summary>
        /// <remarks>
        /// 本地客户端在从服务器收到RPC时像其他客户端一样执行它。
        /// 优点：服务器发送RPC的顺序在所有客户端上是相同的。
        /// </remarks>
        AllViaServer,

        /// <summary>通过服务器将RPC发送给所有人（包括本地客户端），并为后续加入的玩家缓冲。</summary>
        /// <remarks>
        /// 本地客户端在从服务器收到RPC时像其他客户端一样执行它。
        /// 优点：服务器发送RPC的顺序在所有客户端上是相同的。
        /// </remarks>
        AllBufferedViaServer
    }


    public enum ViewSynchronization { Off, ReliableDeltaCompressed, Unreliable, UnreliableOnChange }


    /// <summary>
    /// 定义每个PhotonView的所有权转移处理方式的选项。
    /// </summary>
    /// <remarks>
    /// 此设置影响RequestOwnership和TransferOwnership在运行时的行为。
    /// </remarks>
    public enum OwnershipOption
    {
        /// <summary>
        /// 所有权是固定的。实例化的对象保留其创建者的所有权，房间对象始终属于Master Client。
        /// </summary>
        Fixed,
        /// <summary>
        /// 可以从当前所有者那里夺取所有权，当前所有者无法拒绝。
        /// </summary>
        Takeover,
        /// <summary>
        /// 可以通过PhotonView.RequestOwnership请求所有权，但当前所有者必须同意放弃所有权。
        /// </summary>
        /// <remarks>当前所有者必须实现IPunCallbacks.OnOwnershipRequest来响应所有权请求。</remarks>
        Request
    }
}