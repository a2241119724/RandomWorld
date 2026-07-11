namespace LAB2D
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.EventSystems;

    /// <summary>
    /// 摄像机跟随目标移动.
    /// </summary>
    public class CameraMove : MonoBehaviour
    {
        private const float CameraSpeed = 5.0f; // 相机跟随速度
        private const float EdgeSize = 15.0f; // 相机边缘跟随鼠标的大小
        private const float EdgeSpeed = 50.0f; // 相机边缘跟随速度
        private const float MouseSpeed = 2.0f; // 相机跟随鼠标速度[鼠标中键]
        private const float ScrollSpeed = 100.0f; // 相机缩放速度
        private readonly float[] scaleThreshold = new float[] { 10, 40 }; // 相机缩放阈值
        private bool isDown; // 是否按下鼠标中键
        private Vector3 lastMousePos; // 上一次鼠标位置[鼠标中键拖动相机]

        /// <summary>
        /// 鼠标在相机边缘滑动时[相机跟随鼠标移动]
        /// </summary>
        public static bool IsEdgeMode { get; set; }

        /// <summary>
        /// 相机跟随的目标.
        /// </summary>
        public Character Character { get; set; }

        /// <summary>
        /// 相机相对目标的偏移量.
        /// </summary>
        public Vector3 Offset { get; set; }

        /// <summary>
        /// 相机需要移动到的目标位置.
        /// </summary>
        public Vector3 Target { get; set; }

        /// <summary>
        /// 将镜头直接到目标,不进行过度,消除镜头初始移动的bug.
        /// </summary>
        /// <param name="target">目标<see cref="Vector3"/>位置.</param>
        public void DirectToPosition(Vector3 target)
        {
            // Mathf.Clamp(value,min,max) 夹逼函数,返回min与max之间的数
            // 将镜头直接到玩家身上,消除镜头初始移动的bug
            this.transform.position = new Vector3(target.x + this.Offset.x, target.y + this.Offset.y, -20 + this.Offset.z);
            this.Target = target;
        }

        private void LateUpdate()
        {
            // 跟随的角色存在，那么跟随
            if (this.Character != null)
            {
                this.Target = this.Character.transform.position;
            }

            Vector3 ultimateTarget = new (this.Target.x + this.Offset.x, this.Target.y + this.Offset.y, this.Target.z + this.Offset.z);
            this.transform.position = Vector3.Lerp(this.transform.position, ultimateTarget, Time.deltaTime * CameraSpeed); // 设置相机的位置
            this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, -20 + this.Offset.z); // 固定相机z轴的位置

            // if (gameObject.GetComponent<Camera>() == Camera.main) return;
            // 相机边缘跟随鼠标移动
            if (CameraMove.IsEdgeMode)
            {
                this.Character = null;

                // 真实坐标x对应地图坐标y
                Vector3Int posMap = TileMap.Instance.WorldPosToMapPos(this.Target);
                float offset = Time.deltaTime * EdgeSpeed;
                if (Input.mousePosition.x > Screen.width - EdgeSize && posMap.y < TileMap.Instance.TileMapDataLAB.Width)
                {
                    this.Target = new Vector3(this.Target.x + offset, this.Target.y, 0);
                }
                else if (Input.mousePosition.x < EdgeSize && posMap.y > 0)
                {
                    this.Target = new Vector3(this.Target.x - offset, this.Target.y, 0);
                }
                else if (Input.mousePosition.y > Screen.height - EdgeSize && posMap.x < TileMap.Instance.TileMapDataLAB.Height)
                {
                    this.Target = new Vector3(this.Target.x, this.Target.y + offset, 0);
                }
                else if (Input.mousePosition.y < EdgeSize && posMap.x > 0)
                {
                    this.Target = new Vector3(this.Target.x, this.Target.y - offset, 0);
                }
            }

            // 视角缩放（仅在游戏区域Foreground上时缩放，UI面板上不缩放）
            List<RaycastResult> uiResults = Tool.GetUIByMousePos();
            if (Camera.main.orthographic && Input.mouseScrollDelta.y != 0
                && (uiResults.Count == 0 || uiResults[0].gameObject.name.Equals("Foreground")))
            {
                if (Input.mouseScrollDelta.y > 0 && Camera.main.orthographicSize > this.scaleThreshold[0])
                {
                    Camera.main.orthographicSize -= Time.deltaTime * ScrollSpeed;
                    WeatherManager.Instance.Scale(Camera.main.orthographicSize / 10);
                    if (Camera.main.orthographicSize < this.scaleThreshold[0])
                    {
                        Camera.main.orthographicSize = this.scaleThreshold[0];
                    }
                }
                else if (Input.mouseScrollDelta.y < 0 && Camera.main.orthographicSize < this.scaleThreshold[1])
                {
                    Camera.main.orthographicSize += Time.deltaTime * ScrollSpeed;
                    WeatherManager.Instance.Scale(Camera.main.orthographicSize / 10);
                    if (Camera.main.orthographicSize > this.scaleThreshold[1])
                    {
                        Camera.main.orthographicSize = this.scaleThreshold[1];
                    }
                }
            }

            // 根据鼠标滑动移动
            if (Input.GetMouseButtonDown(2))
            {
                this.Character = null;

                // 过滤不是滑动主屏幕的动作
                if (uiResults.Count > 0 && uiResults[0].gameObject.name.Equals("Foreground"))
                {
                    this.lastMousePos = Input.mousePosition;
                    this.isDown = true;
                }
            }
            else if (this.isDown)
            {
                float mouseSpeed = MouseSpeed * Camera.main.orthographicSize / 10;
                float detx = -(Input.mousePosition.x - this.lastMousePos.x) * mouseSpeed * Time.deltaTime;
                float dety = -(Input.mousePosition.y - this.lastMousePos.y) * mouseSpeed * Time.deltaTime;
                this.Target = new Vector3(this.Target.x + detx, this.Target.y + dety, 0);
                this.lastMousePos = Input.mousePosition;
            }

            if (Input.GetMouseButtonUp(2))
            {
                this.isDown = false;
            }
        }
    }
}
