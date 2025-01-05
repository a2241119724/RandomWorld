using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace LAB2D
{
    public class BuildingUI : MonoBehaviour
    {
        private void Update()
        {
            Vector3 posReal = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // תΪ�����±�
            Vector3Int centerMap = new Vector3Int(Mathf.RoundToInt(posReal.y),Mathf.RoundToInt(posReal.x), 0);
            IsAvailableMap.Instance.showRect(centerMap);
            // ����
            if (Input.GetMouseButtonDown(0))
            {
                // LAB_TODO
                new Room().addRoomBuildTask(centerMap);
            }
        }
    }
}
