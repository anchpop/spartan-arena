using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class JBAgentSimple : Agent {
    public enum eBehavior { toNavMeshTarget, toSpawn };

    [Header("Inscribed")]
    public eBehavior        behavior;
    public Transform        navMeshTarget;
    public float            targetProximity = 1f;

    [SerializeField]
    private Vector3         navMeshTargetLoc = new Vector3(-99999,-99999,-99999);
    private SpawnPoint      sPoint;




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

    public override void OnDrawGizmos() {
        base.OnDrawGizmos();
        if (Application.isPlaying) {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position+transform.up/2, navMeshTargetLoc+transform.up/2);
        }
    }

    public override void AIUpdate(List<SensoryInput> inputs) {
        base.AIUpdate(inputs);
    }
}
