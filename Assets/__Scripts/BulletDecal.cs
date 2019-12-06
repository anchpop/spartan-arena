using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletDecal : MonoBehaviour {
	void Start () {
        Invoke("DieOff", ArenaManager.AGENT_SETTINGS.bulletDecalLifetime);
	}
	
	void DieOff () {
        Destroy(gameObject);
	}
}
