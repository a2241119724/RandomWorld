namespace LAB2D.Character.Player
{
    using UnityEngine;

    /// <summary>
    /// 小地图玩家标记 — 在小地图上以箭头图标代替角色本体显示。
    /// 初始化时将小地图摄像机的 cullingMask 剔除 Player 层（隐藏角色本体）、
    /// 显示 MinimapOnly 层；主摄像机剔除 MinimapOnly 层（游戏世界中不可见）。
    /// 箭头作为玩家子物体跟随移动，朝向随动画 Direction 参数旋转（0-Up 1-Right 2-Down 3-Left）。
    /// </summary>
    public class PlayerMinimapMarker : MonoBehaviour
    {
        private const string MarkerLayerName = "MinimapOnly";
        private const string SpriteResourcePath = "Images/UI/MinimapArrow";
        private const string SortingLayerName = "Highest";
        private const int SortingOrder = 100;

        /// <summary>
        /// 朝向 -> 箭头 z 轴欧拉角（素材默认朝上，z 正角为逆时针）。
        /// </summary>
        private static readonly float[] DirectionAngles = { 0f, -90f, 180f, 90f };

        private Animator animator;
        private Transform marker;
        private Camera miniCamera;
        private Camera mainCamera;
        private int miniCameraOriginalMask;
        private int mainCameraOriginalMask;

        private void Start()
        {
            this.animator = this.GetComponent<Animator>();

            int markerLayer = LayerMask.NameToLayer(MarkerLayerName);
            if (markerLayer < 0)
            {
                AWorkerTask.LogProvider($"[MiniMapDiag] 层 {MarkerLayerName} 不存在，小地图箭头未初始化", LogManager.LogLevelEnum.Error);
                this.enabled = false;
                return;
            }

            Sprite sprite = Resources.Load<Sprite>(SpriteResourcePath);
            if (sprite == null)
            {
                AWorkerTask.LogProvider($"[MiniMapDiag] 资源 {SpriteResourcePath} 加载失败，小地图箭头未初始化", LogManager.LogLevelEnum.Error);
                this.enabled = false;
                return;
            }

            // 箭头作为玩家子物体跟随移动；世界朝向由 LateUpdate 独立驱动，
            // 不受玩家本体 2.5D 视角切换（父级旋转）影响
            GameObject markerObject = new GameObject("MinimapMarker");
            markerObject.transform.SetParent(this.transform, false);
            markerObject.layer = markerLayer;
            SpriteRenderer renderer = markerObject.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingLayerName = SortingLayerName;
            renderer.sortingOrder = SortingOrder;
            this.marker = markerObject.transform;

            this.SetupCameraMasks(markerLayer);
        }

        /// <summary>
        /// 调整摄像机 cullingMask：小地图隐藏角色本体只显示箭头，主摄像机隐藏箭头。
        /// </summary>
        private void SetupCameraMasks(int markerLayer)
        {
            int playerMask = 1 << LayerMask.NameToLayer(LayerConstant.PLAYER_LAYER);
            int markerMask = 1 << markerLayer;

            GameObject miniObject = GameObject.FindGameObjectWithTag(TagConstant.MINIMAP_TAG);
            this.miniCamera = miniObject != null ? miniObject.GetComponent<Camera>() : null;
            if (this.miniCamera != null)
            {
                this.miniCameraOriginalMask = this.miniCamera.cullingMask;
                this.miniCamera.cullingMask = (this.miniCameraOriginalMask & ~playerMask) | markerMask;
            }

            this.mainCamera = Camera.main;
            if (this.mainCamera != null)
            {
                this.mainCameraOriginalMask = this.mainCamera.cullingMask;
                this.mainCamera.cullingMask = this.mainCameraOriginalMask & ~markerMask;
            }
        }

        private void LateUpdate()
        {
            if (this.marker == null)
            {
                return;
            }

            // 每帧以世界旋转覆盖，保证 2.5D 切换后箭头仍水平指向移动方向
            int direction = this.animator != null ? this.animator.GetInteger("Direction") : 0;
            float angle = direction >= 0 && direction < DirectionAngles.Length ? DirectionAngles[direction] : 0f;
            this.marker.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnDestroy()
        {
            // 玩家销毁（场景重建/退出）时还原摄像机遮罩，恢复角色本体在小地图中的显示
            if (this.miniCamera != null)
            {
                this.miniCamera.cullingMask = this.miniCameraOriginalMask;
            }

            if (this.mainCamera != null)
            {
                this.mainCamera.cullingMask = this.mainCameraOriginalMask;
            }
        }
    }
}
