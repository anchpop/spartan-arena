using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class JBAgentShooter : Agent {
    public enum eBehavior { toSpawn };

    [Header("Inscribed JBAgentShooter")]
    public eBehavior        behavior;
    public Transform        navMeshTarget;
    public float            targetProximity = 1f;


    [SerializeField]
    private Vector3         navMeshTargetLoc = new Vector3(-99999,-99999,-99999);
    private SpawnPoint      sPoint;




    void Update () {
        switch (behavior) {
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
        // AIUpdate copies inputs into sensed
        bool sawSomeone = false;
        Vector3 toClosestEnemy = Vector3.one * 1000;
        foreach (SensoryInput si in sensed) {
            switch (si.sense) {
                case SensoryInput.eSense.vision:
                    if (si.type == eSensedObjectType.enemy) {
                        sawSomeone = true;
                        // Check to see whether the Enemy is within the firing arc
                        // The dot product of two vectors is the magnitude of A * the magnitude of B * cos(the angle between them)
                        Vector3 toEnemy = si.pos - pos;
                        if (toEnemy.magnitude < toClosestEnemy.magnitude) {
                            toClosestEnemy = toEnemy;
                        }
                        float dotProduct = Vector3.Dot(headTrans.forward, toEnemy.normalized);
                        float theta = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;
                        if (theta <= ArenaManager.AGENT_SETTINGS.bulletAimVarianceDeg) {
                            if (ammo > 0) {
                                Fire();
                            }
                        }
                    }
                    break;
                case (SensoryInput.eSense.hearing):
                    break;
            }
        }
        if (!sawSomeone) {
            LookCenter();
        } else {
            // Turn head to track enemies
            // How far is the enemy from current headTrans.forward
            float dotProdToClosest = Vector3.Dot(toClosestEnemy.normalized, headTrans.forward);
            float angToClosest = (dotProdToClosest == 1) ? 0 : Mathf.Acos( dotProdToClosest ) * Mathf.Rad2Deg;
            // The following is here because the Acos of 1 is NaN.
            if (float.IsNaN(angToClosest)) {
                angToClosest = 0;
            }
            // A dot product of toEnemy and transform.right will tell you whether to look left or right
            float posNeg = (Vector3.Dot(toClosestEnemy.normalized, headTrans.right) < 0) ? -1 : 1;
            angToClosest *= posNeg;
//            print("AngleToClosest: "+angToClosest);
            LookTheta(angToClosest);
        }

        if (health > 0) {
//            nmAgent.SetDestination(nmAgent.destination);
        }
    }


}
