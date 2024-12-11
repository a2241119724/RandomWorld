using UnityEngine;

namespace LAB2D
{
    public class CameraMove : MonoBehaviour 
    {
        public Vector3 Offset;
        public Vector3 Target { private set; get; } // 相机跟随位置
        public bool IsEdgeMode { get; set; }

        private float cameraSpeed = 5.0f;//相机跟随速度
        private float edgeSpeed = 50.0f;//边界跟随速度
        private float scrollSpeed = 100.0f;//缩放速度
        private float mouseSpeed = 10.0f;//跟随鼠标速度
        private float edgeSize = 1.0f;
        private bool isDown;
        private Vector3 lastMousePos;

        private void LateUpdate()
        {
            Vector3 ultimateTarget = new Vector3(Target.x + Offset.x, Target.y + Offset.y, Target.z + Offset.z);
            transform.position = Vector3.Lerp(transform.position, ultimateTarget, Time.deltaTime * cameraSpeed); // 设置相机的位置
            transform.position = new Vector3(transform.position.x, transform.position.y, -20 + Offset.z); // 固定相机z轴的位置
            // 跟随边缘鼠标移动
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
            if (Camera.main.orthographic && Input.mouseScrollDelta.y> 0 && Camera.main.orthographicSize > 2)
            {
                Camera.main.orthographicSize -= Time.deltaTime * scrollSpeed;
            }
            else if(Camera.main.orthographic && Input.mouseScrollDelta.y < 0)
            {
                Camera.main.orthographicSize += Time.deltaTime * scrollSpeed;
            }
            // 根据鼠标滑动移动
            if (Input.GetMouseButtonDown(0))
            {
                lastMousePos = Input.mousePosition;
                isDown = true;
            }
            else if (isDown)
            {
                float detx = -(Input.mousePosition.x - lastMousePos.x) * mouseSpeed;
                float dety = -(Input.mousePosition.y - lastMousePos.y) * mouseSpeed;
                Target = new Vector3(Target.x + Time.deltaTime * detx, Target.y + Time.deltaTime * dety, 0);
                lastMousePos = Input.mousePosition;
            }
            if (Input.GetMouseButtonUp(0))
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
