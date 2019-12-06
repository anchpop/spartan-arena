using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AgentSimple : MonoBehaviour {
    public enum eBehavior { toNavMeshTarget, toSpawn };

    [Header("Inscribed")]
    public eBehavior        behavior;
    public Transform        navMeshTarget;
    public float            targetProximity = 1f;

    [SerializeField]
    private Vector3         navMeshTargetLoc = new Vector3(-99999,-99999,-99999);
    private NavMeshAgent    nmAgent;
    private SpawnPoint      sPoint;
	
    void Awake() {
        nmAgent = GetComponent<NavMeshAgent>();
    }


	void Update () {
        switch (behavior) {
            case eBehavior.toNavMeshTarget:
                if (navMeshTarget.position != navMeshTargetLoc) {
                    // Repath to the target
                    navMeshTargetLoc = navMeshTarget.position;
                    nmAgent.SetDestination(navMeshTargetLoc);
                    print("AgentSimple:Update() – Set destination to "+navMeshTargetLoc);
                }
                break;
            case eBehavior.toSpawn:
                if (sPoint == null || (transform.position - sPoint.transform.position).magnitude < targetProximity ) {
                    SpawnPoint.eType t = SpawnPoint.RANDOM_SPAWN_POINT_TYPE();
                    List<SpawnPoint> sPoints = SpawnPoint.GET_SPAWN_POINTS(t);
                    if (sPoints.Count == 0) {
                        sPoint = null;
                        break;
                    }
                    sPoint = sPoints[Random.Range(0,sPoints.Count)];
                    navMeshTargetLoc = sPoint.transform.position;
                    nmAgent.SetDestination(navMeshTargetLoc);
                }
                break;
        }
	}

    void OnDrawGizmos() {
        if (Application.isPlaying) {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position+transform.up, navMeshTargetLoc+transform.up);
        }
    }
}
