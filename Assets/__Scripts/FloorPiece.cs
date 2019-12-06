using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FloorPiece : MonoBehaviour {
    static public List<FloorPiece> PIECES;

	// Use this for initialization
	void Awake () {
        if (PIECES == null) {
            PIECES = new List<FloorPiece>();
        }
        PIECES.Add(this);
	}
	
    public Bounds bounds {
        get {
            return new Bounds(transform.position, new Vector3(transform.localScale.x, 0, transform.localScale.z));
        }
    }

    static public FloorPiece RandomFloorPiece() {
        if (PIECES == null) return null;
        return PIECES[ Random.Range(0,PIECES.Count) ];
    }
}
