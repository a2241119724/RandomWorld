using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LAB2D {
    public class WorkerCreator : CharacterCreator<WorkerCreator>
    {
        protected override GameObject _create(Vector3 worldPos,string name,string layer)
        {
            GameObject g = base._create(worldPos,"Worker", "Worker");
            g.name = NameGenertor.Instance.getRandomName();
            return g;
        }
    }
}
