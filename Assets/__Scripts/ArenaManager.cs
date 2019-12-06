using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArenaManager : MonoBehaviour {
    static private ArenaManager S;
    static List<Agent>      AGENTS;
    static List<Wall>       WALLS;
    static List<SensedObject> SENSABLE_OBJS;
    static Transform        _DECAL_PARENT;

    [Header("Inscribed")]
//    public bool             debugSenses = true;
//    public bool             buildWalls = true;
//    public float            hearingDist = 4;
//    public float            visionDist = 20;
//    public float            visionHalfArcDeg = 45;
//    public int              numRandomWalls = 100;
//    public int              wallMaxX = 8, wallMaxZ = 8;
//    public float            spawnPointProtectedRadius = 2;
//    public GameObject       wallPrefab;
    public bool             spawnAgents = false;
    public AgentSettings    agentSettingsSO;

    private Transform       wallAnchor;

    void Awake() {
        S = this;

        if (agentSettingsSO.resetStatsOnLaunch) {
            agentSettingsSO.ResetStats();
        }

        Random.InitState(System.DateTime.Now.Millisecond);
    }

    void Start() {
        // Because FloorPiece.PIECES is populated on FloorPiece.Awake(), we know that it's already populated before this.Start() is called
        GenerateWalls();

        // Reset all the competitors
        foreach (Competitor c in agentSettingsSO.competitors) {
            c.activeAgent = null;
            c.deathTime = -(agentSettingsSO.agentRespawnDelay - 1); // Spawn all agents in 1 second
        }
    }

    void GenerateWalls() {
        if (!agentSettingsSO.buildWalls) 
            return;
        
        wallAnchor = new GameObject("WallAnchor").transform;

        // Generate random walls
        GameObject wGO;
        Wall wal;
        FloorPiece fp;
        Bounds bnd;
        Vector3 size;
        bool okToAdd;
        int numWalls = agentSettingsSO.numWallsPerFloor * SpawnPoint.ALL_SPAWN_POINTS.Count;
        for (int i=0; i<numWalls; i++) {
            wGO = Instantiate<GameObject>(agentSettingsSO.wallPrefab);
            wal = wGO.GetComponent<Wall>();
            // Randomly scale the wall
            float halfHeight = agentSettingsSO.wallMaxScale.y/2;
            wal.transform.localScale = new Vector3( 
                (int) Random.Range(1,agentSettingsSO.wallMaxScale.x),
                halfHeight*2,
                (int) Random.Range(1,agentSettingsSO.wallMaxScale.z)
            );
            // Choose a random FloorPiece
            fp = FloorPiece.RandomFloorPiece();
            bnd = fp.bounds;
            size = bnd.size;
            size.x -= wal.transform.localScale.x;
            size.z -= wal.transform.localScale.z;
            bnd.size = size;
            wal.transform.position = bnd.RandomLocWithin() + Vector3.up*halfHeight;
            // Before adding the wall, make sure it's not colliding with a SpawnPoint
            // Because Physics.CheckSphere will NOT return any info on what was hit, we need to use Bounds instead
            bnd = wal.bounds; // Change bnd to the Wall bounds
            okToAdd = true;
            foreach (SpawnPoint sp in SpawnPoint.ALL_SPAWN_POINTS) {
                // Check to see whether the sp is within the bounds of the Wall
                if (bnd.Contains(sp.transform.position + Vector3.up*halfHeight)) {
                    okToAdd = false;
                    break;
                }
            }
            if (okToAdd) {
                ADD_WALL(wal);
                wal.transform.SetParent(wallAnchor, true);
            } else {
                Destroy(wGO);
            }
        }
    }

    void SpawnAgents() {
        if (!spawnAgents) {
            return;
        }

        foreach (Competitor c in agentSettingsSO.competitors) {
            if (!c.enabled) {
                continue;
            }
            // Determine whether an Agent needs to be spawned
            if (c.activeAgent != null || Time.time < c.deathTime+agentSettingsSO.agentRespawnDelay) {
                continue; // No need to spawn an Agent
            }

            // We need to spawn an Agent
            // Get the proper Script type
            if (c.prefabWithAgent == null) {
                Debug.LogError("Competitor "+c.name+" prefabWithAgent is null.");
                continue;
            }
            Agent a = c.prefabWithAgent.GetComponent<Agent>();
            if (a == null) {
                Debug.LogError("Competitor "+c.name+" prefabWithAgent does not have a valid Agent component.");
                continue;
            }
            System.Type t = a.GetType();

            // Spawn the Agent prefab
            // It is critical that this GameObject NOT have an Agent script already attached
            GameObject go = Instantiate<GameObject>(agentSettingsSO.agentPrefab);
            // Choose a location for the Agent
            SpawnPoint sp = SpawnPoint.GET_RANDOM_SPAWN_POINT(SpawnPoint.eType.agent);
            go.transform.position = sp.pos;

            // Add the proper Agent to this GameObject
            Agent addedAgent = go.AddComponent(t) as Agent;
            addedAgent.SetCompetitor(c);

            go.name = c.name+"-Agent";
        }
    }

	void FixedUpdate () {
        // Spawn agents that need to be spawned
        SpawnAgents();

        if (AGENTS == null) return;

//        float[,] distances = new float[AGENTS.Count, SENSABLE_OBJS.Count];
//        for (int i=0; i<AGENTS.Count; i++) {
//            for (int j=0; j<AGENTS.Count; j++) {
//                if (distances[i,j] == 0) {
//                    distances[i,j] = (AGENTS[i].pos - AGENTS[j].pos).magnitude;
//                    distances[j,i] = distances[i,j];
//                }
//            }
//        }

        List<SensoryInput> senses = new List<SensoryInput>();
        float cosVision = Mathf.Cos( Mathf.Deg2Rad * VISION_HALF_ARC_DEG );
        float cosAngle, dist;
        SensoryInput si;
        
        // Do sensing for the Agent
        // Iterate through each agent
        for (int i=0; i<AGENTS.Count; i++) {
            AGENTS[i].ClearSenses();
            senses.Clear();
            for (int j=0; j<SENSABLE_OBJS.Count; j++) {
//                if (AGENTS[i] == SENSABLE_OBJS[j]) continue;
                dist = (AGENTS[i].pos - SENSABLE_OBJS[j].pos).magnitude;
                if (dist == 0) // This avoids sensing onesself
                    continue;
                // Hearing / Proximity
                if (dist < HEARING_DIST) {
                    si = new SensoryInput(SensoryInput.eSense.hearing, SENSABLE_OBJS[j]);
                    senses.Add( si );
                }
                // Vision
                if (dist < VISION_DIST) {
                    // Use a dot product to see whether we're facing the AGENT[j]
                    Vector3 dirToOther = (SENSABLE_OBJS[j].pos - AGENTS[i].pos).normalized;
                    cosAngle = Vector3.Dot(AGENTS[i].dir, dirToOther);
                    // If cosAngle >= cosVision, then the angle to the other is less than visionHalfArcDeg 
                    if ( cosVision <= cosAngle ) {
                        // Check to see if there is a wall in the way
                        RaycastHit hitInfo;
                        if (Physics.Raycast(AGENTS[i].pos, dirToOther, out hitInfo, VISION_DIST)) {
                            // Something may have been in the way
                            if (hitInfo.collider.GetComponentInParent<Wall>() != null) {
                                continue;
                            }
                        }

                        senses.Add( new SensoryInput(SensoryInput.eSense.vision, SENSABLE_OBJS[j]) );
                    }
                }
            }
            // All touch senses are handled by each Agent
            // Pass the sensory information to the Agent
            try {
                AGENTS[i].AIUpdate(senses);
            } catch (System.Exception ex) {
                
            }
        }

        // NOTE: It would improve efficiency to not calculate this every FixedUpdate() – JB
        foreach (Competitor com in agentSettingsSO.competitors) {
            com.CalculatePoints();
        }
    }

    static public void DEATH(Agent a) {
        Debug.Log("Agent "+a.name+" has died.");
        REMOVE_AGENT(a);
        Destroy(a.gameObject);
    }

    static public void ADD_AGENT(Agent a) {
        if (AGENTS == null) {
            AGENTS = new List<Agent>();
        }
        if (AGENTS.IndexOf(a) == -1) {
            AGENTS.Add(a);
        }
    }

    static public void REMOVE_AGENT(Agent a) {
        if (AGENTS == null) {
            AGENTS = new List<Agent>();
        }
        while (AGENTS.IndexOf(a) != -1) {
            AGENTS.Remove(a);
        }
        REMOVE_SENSABLE(a);
    }


    static public void ADD_WALL(Wall w) {
        if (WALLS == null) {
            WALLS = new List<Wall>();
        }
        if (WALLS.IndexOf(w) == -1) {
            WALLS.Add(w);
        }
    }

    static public void REMOVE_WALL(Wall w) {
        if (WALLS == null) {
            WALLS = new List<Wall>();
        }
        if (WALLS.IndexOf(w) != -1) {
            WALLS.Remove(w);
        }
    }


    static public void ADD_SENSABLE(SensedObject w) {
        if (SENSABLE_OBJS == null) {
            SENSABLE_OBJS = new List<SensedObject>();
        }
        if (SENSABLE_OBJS.IndexOf(w) == -1) {
            SENSABLE_OBJS.Add(w);
        }
    }

    static public void REMOVE_SENSABLE(SensedObject w) {
        if (SENSABLE_OBJS == null) {
            SENSABLE_OBJS = new List<SensedObject>();
        }
        if (SENSABLE_OBJS.IndexOf(w) != -1) {
            SENSABLE_OBJS.Remove(w);
        }
    }

    static public float HEARING_DIST {
        get { return S.agentSettingsSO.agentHearingDist; }
    }

    static public float VISION_DIST {
        get { return S.agentSettingsSO.agentVisionDist; }
    }

    static public float VISION_HALF_ARC_DEG {
        get { return S.agentSettingsSO.agentVisionHalfArcDeg; }
    }

    static public bool DEBUG_SENSES {
        get { return S.agentSettingsSO.debugSenses; }
    }

    static public AgentSettings AGENT_SETTINGS {
        get {
            return S.agentSettingsSO;
        }
    }

    static public void REPORT_BULLET_HIT_ON_AGENT(Bullet bull) {
        // If bull.competitor is not null, report this as a valid hit
        if (bull.competitor != null) {
            bull.competitor.bulletHits++;
        }
    }

    static public void REPORT_BULLET_CAUSED_AGENT_DEATH(Bullet bull) {
        // If bull.competitor is not null, report this as a valid kill
        if (bull.competitor != null) {
            bull.competitor.kills++;
        }
    }

    static public Transform DECAL_PARENT {
        get {
            if (_DECAL_PARENT == null) {
                GameObject go = new GameObject();
                go.name = "Decal Parent";
                _DECAL_PARENT = go.transform;
            }
            return _DECAL_PARENT;
        }
    }
}
