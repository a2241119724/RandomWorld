// ----------------------------------------------------------------------------
// <copyright file="PhotonTransformViewClassic.cs" company="Exit Games GmbH">
//   PhotonNetwork Framework for Unity - Copyright (C) 2018 Exit Games GmbH
// </copyright>
// <summary>
//   通过PUN PhotonView同步Transform的组件。
// </summary>
// <author>developer@exitgames.com</author>
// ----------------------------------------------------------------------------


namespace Photon.Pun
{
    using System.Collections.Generic;
    using UnityEngine;


    /// <summary>
    /// 此类帮助你同步GameObject的位置、旋转和缩放。
    /// 它还提供了许多不同的选项，使同步值看起来平滑，
    /// 即使数据每秒只发送几次。
    /// 只需将此组件添加到你的GameObject，并确保
    /// PhotonTransformViewClassic被添加到观察组件列表中。
    /// </summary>
    [AddComponentMenu("Photon Networking/Photon Transform View Classic")]
    public class PhotonTransformViewClassic : MonoBehaviourPun, IPunObservable
    {
        //由于此组件非常复杂，我们将其分离为多个类。
        //PositionModel、RotationModel和ScaleModel存储你可以在Inspector中
        //配置的数据，而下面的"control"对象则实际负责
        //移动对象并计算所有的插值和外推。

        [HideInInspector]
        public PhotonTransformViewPositionModel m_PositionModel = new PhotonTransformViewPositionModel();

        [HideInInspector]
        public PhotonTransformViewRotationModel m_RotationModel = new PhotonTransformViewRotationModel();

        [HideInInspector]
        public PhotonTransformViewScaleModel m_ScaleModel = new PhotonTransformViewScaleModel();

        PhotonTransformViewPositionControl m_PositionControl;
        PhotonTransformViewRotationControl m_RotationControl;
        PhotonTransformViewScaleControl m_ScaleControl;

        PhotonView m_PhotonView;

        bool m_ReceivedNetworkUpdate = false;

        /// <summary>
        /// 标志用于在实例化对象时跳过初始数据，转而使用第一次反序列化的数据。
        /// </summary>
        bool m_firstTake = false;

        void Awake()
        {
            this.m_PhotonView = GetComponent<PhotonView>();

            this.m_PositionControl = new PhotonTransformViewPositionControl(this.m_PositionModel);
            this.m_RotationControl = new PhotonTransformViewRotationControl(this.m_RotationModel);
            this.m_ScaleControl = new PhotonTransformViewScaleControl(this.m_ScaleModel);
        }

        void OnEnable()
        {
            m_firstTake = true;
        }

        void Update()
        {
            if (this.m_PhotonView == null || this.m_PhotonView.IsMine == true || PhotonNetwork.IsConnectedAndReady == false)
            {
                return;
            }

            this.UpdatePosition();
            this.UpdateRotation();
            this.UpdateScale();
        }

        void UpdatePosition()
        {
            if (this.m_PositionModel.SynchronizeEnabled == false || this.m_ReceivedNetworkUpdate == false)
            {
                return;
            }

            transform.localPosition = this.m_PositionControl.UpdatePosition(transform.localPosition);
        }

        void UpdateRotation()
        {
            if (this.m_RotationModel.SynchronizeEnabled == false || this.m_ReceivedNetworkUpdate == false)
            {
                return;
            }

            transform.localRotation = this.m_RotationControl.GetRotation(transform.localRotation);
        }

        void UpdateScale()
        {
            if (this.m_ScaleModel.SynchronizeEnabled == false || this.m_ReceivedNetworkUpdate == false)
            {
                return;
            }

            transform.localScale = this.m_ScaleControl.GetScale(transform.localScale);
        }

        /// <summary>
        /// 这些值在插值模式或外推模式使用SynchronizeValues时同步到远程对象。
        /// 你的移动脚本应传入当前速度（单位/秒）和转向速度（角度/秒），
        /// 以便远程对象可以使用它们来预测对象的移动。
        /// </summary>
        /// <param name="speed">对象当前的移动向量（单位/秒）。</param>
        /// <param name="turnSpeed">对象当前的转向速度（角度/秒）。</param>
        public void SetSynchronizedValues(Vector3 speed, float turnSpeed)
        {
            this.m_PositionControl.SetSynchronizedValues(speed, turnSpeed);
        }


        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            this.m_PositionControl.OnPhotonSerializeView(transform.localPosition, stream, info);
            this.m_RotationControl.OnPhotonSerializeView(transform.localRotation, stream, info);
            this.m_ScaleControl.OnPhotonSerializeView(transform.localScale, stream, info);

            if (stream.IsReading == true)
            {
                this.m_ReceivedNetworkUpdate = true;

                // 强制使用最新数据以避免玩家实例化时的初始漂移。
                if (m_firstTake)
                {
                    m_firstTake = false;

                    if (this.m_PositionModel.SynchronizeEnabled)
                    {
                        this.transform.localPosition = this.m_PositionControl.GetNetworkPosition();
                    }

                    if (this.m_RotationModel.SynchronizeEnabled)
                    {
                        this.transform.localRotation = this.m_RotationControl.GetNetworkRotation();
                    }

                    if (this.m_ScaleModel.SynchronizeEnabled)
                    {
                        this.transform.localScale = this.m_ScaleControl.GetNetworkScale();
                    }
                }
            }
        }
    }


    [System.Serializable]
    public class PhotonTransformViewPositionModel
    {
        public enum InterpolateOptions
        {
            Disabled,
            FixedSpeed,
            EstimatedSpeed,
            SynchronizeValues,
            Lerp
        }


        public enum ExtrapolateOptions
        {
            Disabled,
            SynchronizeValues,
            EstimateSpeedAndTurn,
            FixedSpeed,
        }


        public bool SynchronizeEnabled;

        public bool TeleportEnabled = true;
        public float TeleportIfDistanceGreaterThan = 3f;

        public InterpolateOptions InterpolateOption = InterpolateOptions.EstimatedSpeed;
        public float InterpolateMoveTowardsSpeed = 1f;

        public float InterpolateLerpSpeed = 1f;

        public ExtrapolateOptions ExtrapolateOption = ExtrapolateOptions.Disabled;
        public float ExtrapolateSpeed = 1f;
        public bool ExtrapolateIncludingRoundTripTime = true;
        public int ExtrapolateNumberOfStoredPositions = 1;
    }

    public class PhotonTransformViewPositionControl
    {
        PhotonTransformViewPositionModel m_Model;
        float m_CurrentSpeed;
        double m_LastSerializeTime;
        Vector3 m_SynchronizedSpeed = Vector3.zero;
        float m_SynchronizedTurnSpeed = 0;

        Vector3 m_NetworkPosition;
        Queue<Vector3> m_OldNetworkPositions = new Queue<Vector3>();

        bool m_UpdatedPositionAfterOnSerialize = true;

        public PhotonTransformViewPositionControl(PhotonTransformViewPositionModel model)
        {
            m_Model = model;
        }

        Vector3 GetOldestStoredNetworkPosition()
        {
            Vector3 oldPosition = m_NetworkPosition;

            if (m_OldNetworkPositions.Count > 0)
            {
                oldPosition = m_OldNetworkPositions.Peek();
            }

            return oldPosition;
        }

        /// <summary>
        /// 这些值在插值模式或外推模式使用SynchronizeValues时同步到远程对象。
        /// 你的移动脚本应传入当前速度（单位/秒）和转向速度（角度/秒），
        /// 以便远程对象可以使用它们来预测对象的移动。
        /// </summary>
        /// <param name="speed">对象当前的移动向量（单位/秒）。</param>
        /// <param name="turnSpeed">对象当前的转向速度（角度/秒）。</param>
        public void SetSynchronizedValues(Vector3 speed, float turnSpeed)
        {
            m_SynchronizedSpeed = speed;
            m_SynchronizedTurnSpeed = turnSpeed;
        }

        /// <summary>
        /// 根据Inspector中设置的值计算新位置。
        /// </summary>
        /// <param name="currentPosition">当前位置。</param>
        /// <returns>新位置。</returns>
        public Vector3 UpdatePosition(Vector3 currentPosition)
        {
            Vector3 targetPosition = GetNetworkPosition() + GetExtrapolatedPositionOffset();

            switch (m_Model.InterpolateOption)
            {
                case PhotonTransformViewPositionModel.InterpolateOptions.Disabled:
                    if (m_UpdatedPositionAfterOnSerialize == false)
                    {
                        currentPosition = targetPosition;
                        m_UpdatedPositionAfterOnSerialize = true;
                    }

                    break;

                case PhotonTransformViewPositionModel.InterpolateOptions.FixedSpeed:
                    currentPosition = Vector3.MoveTowards(currentPosition, targetPosition, Time.deltaTime * m_Model.InterpolateMoveTowardsSpeed);
                    break;

                case PhotonTransformViewPositionModel.InterpolateOptions.EstimatedSpeed:
                    if (m_OldNetworkPositions.Count == 0)
                    {
                        // 特殊情况：内存中没有先前的更新，因此我们无法估算速度！
                        break;
                    }

                    // 知道最后一个（传入的）位置和之前的��个，我们可以估算速度。
                    // 注意，速度要乘以sendRateOnSerialize！我们每秒发送X次更新，所以我们的估算必须考虑到这一点。
                    float estimatedSpeed = (Vector3.Distance(m_NetworkPosition, GetOldestStoredNetworkPosition()) / m_OldNetworkPositions.Count) * PhotonNetwork.SerializationRate;

                    // 以从最新更新计算出的速度向targetPosition移动（如果激活了估算，则包含估算）。
                    currentPosition = Vector3.MoveTowards(currentPosition, targetPosition, Time.deltaTime * estimatedSpeed);
                    break;

                case PhotonTransformViewPositionModel.InterpolateOptions.SynchronizeValues:
                    if (m_SynchronizedSpeed.magnitude == 0)
                    {
                        currentPosition = targetPosition;
                    }
                    else
                    {
                        currentPosition = Vector3.MoveTowards(currentPosition, targetPosition, Time.deltaTime * m_SynchronizedSpeed.magnitude);
                    }

                    break;

                case PhotonTransformViewPositionModel.InterpolateOptions.Lerp:
                    currentPosition = Vector3.Lerp(currentPosition, targetPosition, Time.deltaTime * m_Model.InterpolateLerpSpeed);
                    break;
            }

            if (m_Model.TeleportEnabled == true)
            {
                if (Vector3.Distance(currentPosition, GetNetworkPosition()) > m_Model.TeleportIfDistanceGreaterThan)
                {
                    currentPosition = GetNetworkPosition();
                }
            }

            return currentPosition;
        }

        /// <summary>
        /// 获取通过网络接收到的最后一个位置。
        /// </summary>
        /// <returns></returns>
        public Vector3 GetNetworkPosition()
        {
            return m_NetworkPosition;
        }

        /// <summary>
        /// 基于最后同步的位置、接收到最后位置的时间以及对象的移动速度计算估算位置。
        /// </summary>
        /// <returns>远程对象的估算位置</returns>
        public Vector3 GetExtrapolatedPositionOffset()
        {
            float timePassed = (float)(PhotonNetwork.Time - m_LastSerializeTime);

            if (m_Model.ExtrapolateIncludingRoundTripTime == true)
            {
                timePassed += (float)PhotonNetwork.GetPing() / 1000f;
            }

            Vector3 extrapolatePosition = Vector3.zero;

            switch (m_Model.ExtrapolateOption)
            {
                case PhotonTransformViewPositionModel.ExtrapolateOptions.SynchronizeValues:
                    Quaternion turnRotation = Quaternion.Euler(0, m_SynchronizedTurnSpeed * timePassed, 0);
                    extrapolatePosition = turnRotation * (m_SynchronizedSpeed * timePassed);
                    break;
                case PhotonTransformViewPositionModel.ExtrapolateOptions.FixedSpeed:
                    Vector3 moveDirection = (m_NetworkPosition - GetOldestStoredNetworkPosition()).normalized;

                    extrapolatePosition = moveDirection * m_Model.ExtrapolateSpeed * timePassed;
                    break;
                case PhotonTransformViewPositionModel.ExtrapolateOptions.EstimateSpeedAndTurn:
                    Vector3 moveDelta = (m_NetworkPosition - GetOldestStoredNetworkPosition()) * PhotonNetwork.SerializationRate;
                    extrapolatePosition = moveDelta * timePassed;
                    break;
            }

            return extrapolatePosition;
        }

        public void OnPhotonSerializeView(Vector3 currentPosition, PhotonStream stream, PhotonMessageInfo info)
        {
            if (m_Model.SynchronizeEnabled == false)
            {
                return;
            }

            if (stream.IsWriting == true)
            {
                SerializeData(currentPosition, stream, info);
            }
            else
            {
                DeserializeData(stream, info);
            }

            m_LastSerializeTime = PhotonNetwork.Time;
            m_UpdatedPositionAfterOnSerialize = false;
        }

        void SerializeData(Vector3 currentPosition, PhotonStream stream, PhotonMessageInfo info)
        {
            stream.SendNext(currentPosition);
            m_NetworkPosition = currentPosition;

            if (m_Model.ExtrapolateOption == PhotonTransformViewPositionModel.ExtrapolateOptions.SynchronizeValues ||
                m_Model.InterpolateOption == PhotonTransformViewPositionModel.InterpolateOptions.SynchronizeValues)
            {
                stream.SendNext(m_SynchronizedSpeed);
                stream.SendNext(m_SynchronizedTurnSpeed);
            }
        }

        void DeserializeData(PhotonStream stream, PhotonMessageInfo info)
        {
            Vector3 readPosition = (Vector3)stream.ReceiveNext();
            if (m_Model.ExtrapolateOption == PhotonTransformViewPositionModel.ExtrapolateOptions.SynchronizeValues ||
                m_Model.InterpolateOption == PhotonTransformViewPositionModel.InterpolateOptions.SynchronizeValues)
            {
                m_SynchronizedSpeed = (Vector3)stream.ReceiveNext();
                m_SynchronizedTurnSpeed = (float)stream.ReceiveNext();
            }

            if (m_OldNetworkPositions.Count == 0)
            {
                // 如果还没有旧位置，这是客户端读取的第一个更新。让我们将此同时用作当前和旧位置。
                m_NetworkPosition = readPosition;
            }

            // 之前接收到的位置变为旧的并进入队列。新的位置是m_NetworkPosition
            m_OldNetworkPositions.Enqueue(m_NetworkPosition);
            m_NetworkPosition = readPosition;

            // 将队列中的项目减少到定义的存储位置数量。
            while (m_OldNetworkPositions.Count > m_Model.ExtrapolateNumberOfStoredPositions)
            {
                m_OldNetworkPositions.Dequeue();
            }
        }
    }


    [System.Serializable]
    public class PhotonTransformViewRotationModel
    {
        public enum InterpolateOptions
        {
            Disabled,
            RotateTowards,
            Lerp,
        }


        public bool SynchronizeEnabled;

        public InterpolateOptions InterpolateOption = InterpolateOptions.RotateTowards;
        public float InterpolateRotateTowardsSpeed = 180;
        public float InterpolateLerpSpeed = 5;
    }

    public class PhotonTransformViewRotationControl
    {
        PhotonTransformViewRotationModel m_Model;
        Quaternion m_NetworkRotation;

        public PhotonTransformViewRotationControl(PhotonTransformViewRotationModel model)
        {
            m_Model = model;
        }

        /// <summary>
        /// 获取通过网络接收到的最后一个旋转。
        /// </summary>
        /// <returns></returns>
        public Quaternion GetNetworkRotation()
        {
            return m_NetworkRotation;
        }

        public Quaternion GetRotation(Quaternion currentRotation)
        {
            switch (m_Model.InterpolateOption)
            {
                default:
                case PhotonTransformViewRotationModel.InterpolateOptions.Disabled:
                    return m_NetworkRotation;
                case PhotonTransformViewRotationModel.InterpolateOptions.RotateTowards:
                    return Quaternion.RotateTowards(currentRotation, m_NetworkRotation, m_Model.InterpolateRotateTowardsSpeed * Time.deltaTime);
                case PhotonTransformViewRotationModel.InterpolateOptions.Lerp:
                    return Quaternion.Lerp(currentRotation, m_NetworkRotation, m_Model.InterpolateLerpSpeed * Time.deltaTime);
            }
        }

        public void OnPhotonSerializeView(Quaternion currentRotation, PhotonStream stream, PhotonMessageInfo info)
        {
            if (m_Model.SynchronizeEnabled == false)
            {
                return;
            }

            if (stream.IsWriting == true)
            {
                stream.SendNext(currentRotation);
                m_NetworkRotation = currentRotation;
            }
            else
            {
                m_NetworkRotation = (Quaternion)stream.ReceiveNext();
            }
        }
    }


    [System.Serializable]
    public class PhotonTransformViewScaleModel
    {
        public enum InterpolateOptions
        {
            Disabled,
            MoveTowards,
            Lerp,
        }


        public bool SynchronizeEnabled;

        public InterpolateOptions InterpolateOption = InterpolateOptions.Disabled;
        public float InterpolateMoveTowardsSpeed = 1f;
        public float InterpolateLerpSpeed;
    }

    public class PhotonTransformViewScaleControl
    {
        PhotonTransformViewScaleModel m_Model;
        Vector3 m_NetworkScale = Vector3.one;

        public PhotonTransformViewScaleControl(PhotonTransformViewScaleModel model)
        {
            m_Model = model;
        }

        /// <summary>
        /// 获取通过网络接收到的最后一个缩放。
        /// </summary>
        /// <returns></returns>
        public Vector3 GetNetworkScale()
        {
            return m_NetworkScale;
        }

        public Vector3 GetScale(Vector3 currentScale)
        {
            switch (m_Model.InterpolateOption)
            {
                default:
                case PhotonTransformViewScaleModel.InterpolateOptions.Disabled:
                    return m_NetworkScale;
                case PhotonTransformViewScaleModel.InterpolateOptions.MoveTowards:
                    return Vector3.MoveTowards(currentScale, m_NetworkScale, m_Model.InterpolateMoveTowardsSpeed * Time.deltaTime);
                case PhotonTransformViewScaleModel.InterpolateOptions.Lerp:
                    return Vector3.Lerp(currentScale, m_NetworkScale, m_Model.InterpolateLerpSpeed * Time.deltaTime);
            }
        }

        public void OnPhotonSerializeView(Vector3 currentScale, PhotonStream stream, PhotonMessageInfo info)
        {
            if (m_Model.SynchronizeEnabled == false)
            {
                return;
            }

            if (stream.IsWriting == true)
            {
                stream.SendNext(currentScale);
                m_NetworkScale = currentScale;
            }
            else
            {
                m_NetworkScale = (Vector3)stream.ReceiveNext();
            }
        }
    }
}