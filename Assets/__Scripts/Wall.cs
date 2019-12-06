using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wall : MonoBehaviour {
    private Vector3     lastPos = new Vector3(-9999, -9999, -9999);
    private Bounds      _bounds;

    public Bounds bounds {
        get {
            // This caches the _bounds so that they're not calculated every time they're requested
            if (transform.position != lastPos) {
                _bounds = new Bounds(transform.position, transform.lossyScale + Vector3.one);
                // the + Vector3.one above is to account for the radius around the Agent
            }
            return _bounds;
        }
    }


}
