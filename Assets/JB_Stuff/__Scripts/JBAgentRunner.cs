using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class JBAgentRunner : Agent {
    public enum eBehavior { runAway, toSpawn };

    [Header("Inscribed")]
    public eBehavior        behavior;
    public float            targetProximity = 1f;
    public int              sensedCount;
    public float            runawayTime = 1f;

    [SerializeField]
    private Vector3         navMeshTargetLoc = new Vector3(-99999,-99999,-99999);
    internal bool           runningAway = false;
    internal float          noLongerAfraid;
    private Vector3         cachedTarget;
    private SpawnPoint      sPoint;
    internal Vector3        runVec;

    void Update () {
        if (runningAway && Time.time >= noLongerAfraid) {
            StopRunning();
        }
        switch (behavior) {
            case eBehavior.toSpawn:
                if (sPoint == null || (transform.position - navMeshTargetLoc).magnitude < targetProximity ) {
                    if (runningAway) {
                        RunAlongVec();
//                        navMeshTargetLoc = pos + runVec;
//                        nmAgent.SetDestination(navMeshTargetLoc);
                    } else {
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
        sensedCount = sensed.Count;

        // Loop through each sensed input and find an enemy
        foreach (SensoryInput si in sensed) {
            if (si.obj is Agent) {
                if (!runningAway && !(si.obj is JBAgentRunner)) {
//                    print("OH MYYYYYY "+GetName()+" "+si.sense+"ed "+si.obj.GetName());
                    // RUN AWAY!
                    //Vector3 runAwayLoc = pos + (pos - si.obj.pos)*10;
                    cachedTarget = navMeshTargetLoc;
                    runVec = (pos - si.obj.pos).normalized * 2;
                    RunAlongVec(runVec);
//                    navMeshTargetLoc = pos + runVec;
//                    nmAgent.SetDestination(navMeshTargetLoc);
                    runningAway = true;
                    noLongerAfraid = Time.time + runawayTime;
//                    Invoke("StopRunning", runawayTime);
                }
                if (!runningAway && si.sense == SensoryInput.eSense.hearing && (si.obj is JBAgentRunner)) {
                    JBAgentRunner jBar = si.obj as JBAgentRunner;
                    if (jBar.runningAway) {
                        cachedTarget = navMeshTargetLoc;
                        runVec = jBar.runVec;
                        Vector3 runFrom = jBar.pos - jBar.runVec * 2;
                        runVec = (pos - runFrom).normalized * 2;
                        RunAlongVec();
//                        navMeshTargetLoc = pos + runVec;
//                        nmAgent.SetDestination(navMeshTargetLoc);
                        runningAway = true;
                        noLongerAfraid = jBar.noLongerAfraid;
                    }
                }
            }
        }
    }

    void RunAlongVec() {
        RunAlongVec(runVec);
    }

    void RunAlongVec(Vector3 v) {
        runVec = v;
        NavMeshHit hit;
        if (nmAgent.Raycast( pos + runVec, out hit )) {
            navMeshTargetLoc = hit.position;
        } else {
            navMeshTargetLoc = pos+runVec;
        }
        nmAgent.SetDestination(navMeshTargetLoc);
    }

    void StopRunning() {
        navMeshTargetLoc = cachedTarget;
        runningAway = false;
        nmAgent.SetDestination(navMeshTargetLoc);
    }
}
