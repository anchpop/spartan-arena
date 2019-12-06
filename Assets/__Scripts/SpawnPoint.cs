using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour, SensedObject {
    public enum eType { none, item, agent };

    static private eSensedObjectType TYPE = eSensedObjectType.spawn;
    static protected Dictionary<SpawnPoint.eType, List<SpawnPoint>> SPAWN_DICT;
    static public List<SpawnPoint> ALL_SPAWN_POINTS;
    static public Color[] COLORS = new Color[] { Color.gray, Color.red, Color.green,
        Color.blue, Color.yellow, Color.cyan, Color.magenta };

    public eType        spType {get; private set;}
    private PickUp.eType pickUpType;

    private PickUp      spawnedPickUp;
    private Flag        flag;

	// Use this for initialization
	void Awake () {
        spType = RANDOM_SPAWN_POINT_TYPE();
        pickUpType = (spType == eType.item) ? PickUp.RandomType() : PickUp.eType.none;
        if (spType == eType.item) {
            if (ArenaManager.AGENT_SETTINGS.spawnAtStart) {
                Spawn();
            } else {
                Invoke("Spawn", ArenaManager.AGENT_SETTINGS.spawnDelay);
            }
        }
            
        ADD_SPAWN_POINT(this);
        ArenaManager.ADD_SENSABLE(this);

        GameObject go = Instantiate<GameObject>(ArenaManager.AGENT_SETTINGS.flagPrefab);
        go.transform.SetParent(transform, true);
        go.transform.localPosition = Vector3.zero;
        flag = go.GetComponent<Flag>();
        flag.type = spType;
	}
	
	void Spawn () {
        GameObject puGO = Instantiate<GameObject>(ArenaManager.AGENT_SETTINGS.pickUpPrefab);
        spawnedPickUp = puGO.GetComponent<PickUp>();
        spawnedPickUp.puType = pickUpType;
        spawnedPickUp.transform.position = pos + Vector3.up;
        spawnedPickUp.DestroyedCallback += PickUpDestroyed;
        spawnedPickUp.gameObject.name = name+" : "+pickUpType.ToString();
	}

    void OnDestroy() {
        if (spawnedPickUp != null) {
            spawnedPickUp.DestroyedCallback -= PickUpDestroyed;
        }
    }

    void PickUpDestroyed(PickUp pu) {
        // NOTE: At this point, nothing is done with the PickUp passed in
        // NOTE: I don't believe there is any need to remove this method from pu.DestroyedCallback
        Invoke("Spawn", ArenaManager.AGENT_SETTINGS.spawnDelay);
        spawnedPickUp = null;
    }

    void OnDrawGizmos() {
        Gizmos.color = COLORS[(int) spType];
        Gizmos.DrawLine(transform.position, transform.position + transform.up*2);
        Gizmos.DrawSphere(transform.position + transform.up*2, 0.25f);
    }

    #region SensedObject implementation
    public eSensedObjectType type {
        get {
            return TYPE;
        }
    }

    public string GetName() {
        return "SpawnPoint "+gameObject.name;
    }
    public Vector3 pos {
        get {
            return transform.position;
        }
    }
    public Vector3 dir {
        get {
            return transform.forward;
        }
    }

    public void Contact(SensingObject so) {
        // Do Nothing
    }
    #endregion

    static int SPAWN_POINT_TYPE_VARIANCE_ALLOWANCE = 2;
    static int NUM_SP_ITEM = 0;
    static int NUM_SP_AGENT = 0;
    static public eType RANDOM_SPAWN_POINT_TYPE(bool allowNone=false) {
        eType[] types = (eType[]) System.Enum.GetValues( typeof(SpawnPoint.eType) );
        // Pick a random eType from types
        if (allowNone) {
            return types[Random.Range(0,types.Length)];
        } else {
            // This forces there not to be a huge imbalance between agent and item SpawnPoint counts
            int diff = NUM_SP_ITEM - NUM_SP_AGENT;
            SpawnPoint.eType type;
            if (Mathf.Abs(diff) > SPAWN_POINT_TYPE_VARIANCE_ALLOWANCE) {
                if (diff < 0) {
                    type = SpawnPoint.eType.item;
                } else {
                    type = SpawnPoint.eType.agent;
                }
            } else {
                type = types[Random.Range(1,types.Length)];
            }
            if (type == eType.agent) NUM_SP_AGENT++;
            if (type == eType.item) NUM_SP_ITEM++;
//            print("Agent:"+NUM_SP_AGENT+"\tItem:"+NUM_SP_ITEM);
            return type;
        }
//        // Generate a list from all the possible eTypes
//        List<eType> types = new List<eType>();
//        foreach (var name in System.Enum.GetNames(typeof(SpawnPoint.eType))) {
//            types.Add( (eType) System.Enum.Parse(SpawnPoint.eType, name));
//        }
    }

    static void ADD_SPAWN_POINT(SpawnPoint sp) {
        if (SPAWN_DICT == null) {
            SPAWN_DICT = new Dictionary<eType, List<SpawnPoint>>();
        }

        eType t = sp.spType;
        if ( !SPAWN_DICT.ContainsKey(t) ) {
            SPAWN_DICT.Add(t, new List<SpawnPoint>());
        }

        if (SPAWN_DICT[t].IndexOf(sp) == -1) {
            SPAWN_DICT[t].Add(sp);
        }

        // Also add it to ALL_SPAWN_POINTS
        if (ALL_SPAWN_POINTS == null) {
            ALL_SPAWN_POINTS = new List<SpawnPoint>();
        }
        if (ALL_SPAWN_POINTS.IndexOf(sp) == -1) {
            ALL_SPAWN_POINTS.Add(sp);
        }
    }

    static void REMOVE_SPAWN_POINT(SpawnPoint sp) {
        if (SPAWN_DICT == null) {
            SPAWN_DICT = new Dictionary<eType, List<SpawnPoint>>();
        }

        eType t = sp.spType;
        if ( !SPAWN_DICT.ContainsKey(t) ) {
            SPAWN_DICT.Add(t, new List<SpawnPoint>());
        }

        if (SPAWN_DICT[t].IndexOf(sp) != -1) {
            SPAWN_DICT[t].Remove(sp);
        }

        // Also add it to ALL_SPAWN_POINTS
        if (ALL_SPAWN_POINTS == null) {
            ALL_SPAWN_POINTS = new List<SpawnPoint>();
        }
        if (ALL_SPAWN_POINTS.IndexOf(sp) != -1) {
            ALL_SPAWN_POINTS.Remove(sp);
        }
    }

    static public List<SpawnPoint> GET_SPAWN_POINTS(eType t) {
        if ( !SPAWN_DICT.ContainsKey(t) ) {
            SPAWN_DICT.Add(t, new List<SpawnPoint>());
        }
        return SPAWN_DICT[t];
    }

    static public SpawnPoint GET_RANDOM_SPAWN_POINT(eType t) {
        List<SpawnPoint> sPL = GET_SPAWN_POINTS(t);
        if (sPL.Count == 0)
            return null;
        return sPL[Random.Range(0,sPL.Count)];
    }
}
