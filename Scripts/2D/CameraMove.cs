using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace LAB2D
{
    public class CameraMove : MonoBehaviour 
    {
        public Vector3 Offset;
        public Vector3 Target { set; get; } // 相机跟随位置
        public Character Character { get; set; } // 跟随的角色
        public bool IsEdgeMode { get; set; }

        private const float cameraSpeed = 5.0f;//相机跟随速度
        private const float edgeSpeed = 50.0f;//边界跟随速度
        private const float scrollSpeed = 100.0f;//缩放速度
        private const float mouseSpeed = 2.0f;//跟随鼠标速度
        private const float edgeSize = 1.0f;
        /// <summary>
        /// 缩放的最小视角与最大视角
        /// </summary>
        private readonly float[] scaleThreshold = new float[] { 8, 40 };

        private bool isDown;
        private Vector3 lastMousePos;

        private void LateUpdate()
        {
            // 仅主相机检测
            if (Camera.main == gameObject.GetComponent<Camera>() && Input.anyKeyDown)
            {
                Character = null;
            }
            // 跟随的角色存在，那么跟随
            if (Character != null)
            {
                Target = Character.transform.position;
            }
            Vector3 ultimateTarget = new Vector3(Target.x + Offset.x, Target.y + Offset.y, Target.z + Offset.z);
            transform.position = Vector3.Lerp(transform.position, ultimateTarget, Time.deltaTime * cameraSpeed); // 设置相机的位置
            transform.position = new Vector3(transform.position.x, transform.position.y, -20 + Offset.z); // 固定相机z轴的位置
            //if (gameObject.GetComponent<Camera>() == Camera.main) return;
            // 跟随边缘鼠标移动(LOL)
            if (IsEdgeMode)
            {
                if (Input.mousePosition.x > Screen.width - edgeSize)
                {
                    Target = new Vector3(Target.x + Time.deltaTime * edgeSpeed, Target.y, 0);
                }
                else if (Input.mousePosition.x < edgeSize)
                {
                    Target = new Vector3(Target.x - Time.deltaTime * edgeSpeed, Target.y, 0);
                }
                else if (Input.mousePosition.y > Screen.height - edgeSize)
                {
                    Target = new Vector3(Target.x, Target.y + Time.deltaTime * edgeSpeed, 0);
                }
                else if (Input.mousePosition.y < edgeSize)
                {
                    Target = new Vector3(Target.x, Target.y - Time.deltaTime * edgeSpeed, 0);
                }
            }
            // 视角缩放
            if (Camera.main.orthographic && Input.mouseScrollDelta.y> 0 
                && Camera.main.orthographicSize > scaleThreshold[0])
            {
                Camera.main.orthographicSize -= Time.deltaTime * scrollSpeed;
                if(Camera.main.orthographicSize < scaleThreshold[0])
                {
                    Camera.main.orthographicSize = scaleThreshold[0];
                }
            }
            else if(Camera.main.orthographic && Input.mouseScrollDelta.y < 0 
                && Camera.main.orthographicSize < scaleThreshold[1])
            {
                Camera.main.orthographicSize += Time.deltaTime * scrollSpeed;
                if (Camera.main.orthographicSize > scaleThreshold[1])
                {
                    Camera.main.orthographicSize = scaleThreshold[1];
                }
            }
            // 根据鼠标滑动移动
            if (Input.GetMouseButtonDown(2))
            {
                List<RaycastResult> results = Tool.getUIByMousePos();
                // 过滤不是滑动主屏幕的动作
                if(results.Count > 0 && results[0].gameObject.name.Equals("Foreground"))
                {
                    lastMousePos = Input.mousePosition;
                    isDown = true;
                }
            }
            else if (isDown)
            {
                float detx = -(Input.mousePosition.x - lastMousePos.x) * mouseSpeed;
                float dety = -(Input.mousePosition.y - lastMousePos.y) * mouseSpeed;
                Target = new Vector3(Target.x + Time.deltaTime * detx, Target.y + Time.deltaTime * dety, 0);
                lastMousePos = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(2))
            {
                isDown = false;
            }
        }

        public void directToPosition(Vector3 target) {
            //Mathf.Clamp(value,min,max) 夹逼函数,返回min与max之间的数
            // 将镜头直接到玩家身上,消除镜头初始移动的bug
            transform.position = new Vector3(target.x + Offset.x, target.y + Offset.y, -20 + Offset.z);
            Target = target;
        }
    }
}
