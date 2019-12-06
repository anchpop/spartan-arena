using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Flag : MonoBehaviour {
    private SpawnPoint.eType    _type;
    private Renderer[]          rends;

	// Use this for initialization
	void Awake () {
        rends = GetComponentsInChildren<Renderer>();
	}
	
    public SpawnPoint.eType type {
        get { return _type; }
        set {
            _type = value;
            int t = (int) _type;
            foreach (Renderer rend in rends) {
                rend.material.color = SpawnPoint.COLORS[t];
            }
        }
    } 

}
