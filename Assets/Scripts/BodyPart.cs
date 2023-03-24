using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BodyPart : NetworkBehaviour {
    [SerializeField] protected BoxCollider2D collider;
     public NetworkObject networkObject;

    public virtual bool CheckPointWithinBounds(Vector3 point) {
        if(collider != null)
            return collider.bounds.Contains(point);
        return false;
    }
}
