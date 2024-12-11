using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class WorkerCreator : CharacterCreator<WorkerCreator>
    {
        public Vector3 Position { set; get; }

        protected override GameObject _create()
        {
            GameObject g = PhotonNetwork.Instantiate(ResourcesManager.Instance.getPath("Worker.prefab"),
                new Vector3(Position.x + TileMap.Instance.transform.position.x, 
                Position.y + TileMap.Instance.transform.position.y, 
                TileMap.Instance.transform.position.z), 
                Quaternion.identity);
            if (g == null)
            {
                Debug.LogError("worker Instantiate Error!!!");
                return null;
            }
            // …Ë÷√≤„º∂
            g.layer = LayerMask.NameToLayer("Worker");
            return g;
        }
    }
}
