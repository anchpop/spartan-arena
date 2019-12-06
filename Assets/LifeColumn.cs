using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LifeColumn : MonoBehaviour {
    Agent a;
    float birth;

	// Use this for initialization
	void Start () {
        birth = Time.time;
	}
	
	// Update is called once per frame
	void Update () {
        float life = Time.time - birth;
        transform.localScale = new Vector3(1,life,1);
	}
}
