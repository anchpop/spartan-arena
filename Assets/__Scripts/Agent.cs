#define DEBUG_SENSES

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[System.Serializable]
public class Agent : MonoBehaviour, SensedObject, SensingObject {
    static public bool      DEBUG_SENSES_TEXT = false;
    static private eSensedObjectType TYPE = eSensedObjectType.enemy;

    [Header("Dynamic Agent")]
    public int              health;
    public int              ammo;

    private Competitor      competitor;
    [SerializeField]
    private Color           _color;

    protected NavMeshAgent  nmAgent;
    protected Transform     headTrans;
    protected List<SensoryInput> sensed;
    private Transform       lEye, rEye;
    public Transform        gun { get; protected set; }

    private float           lastShot = -1;

    private float           spawnTime;
    private int             lifeTimeCount = 0;

    public void SetCompetitor( Competitor com ) {
        competitor = com;
        com.activeAgent = this;

        color = com.color;
    }

    public virtual void Awake() {
        spawnTime = Time.time;

        health = ArenaManager.AGENT_SETTINGS.agentHealthStart;
        ammo = ArenaManager.AGENT_SETTINGS.agentAmmoStart;

        nmAgent = GetComponent<NavMeshAgent>();
        headTrans = transform.Find("Head");
        gun = headTrans.Find("Gun");
        ArenaManager.ADD_AGENT(this);
        ArenaManager.ADD_SENSABLE(this);

        sensed = new List<SensoryInput>();

        // Add the SparkAndSmoke script to show visual effects for hits.
        GameObject sns = Instantiate<GameObject>(ArenaManager.AGENT_SETTINGS.sparkAndSmokePrefab);
        sns.transform.SetParent(transform, false);
    }

    public virtual void Start() {
        SpawnPoint sp = SpawnPoint.GET_RANDOM_SPAWN_POINT(SpawnPoint.eType.agent);
        if (sp != null) {
            transform.position = sp.pos;
        }
    }

    public void OnDestroy() {
        ArenaManager.REMOVE_AGENT(this);

        // Clear the Competitor so that another agent can be spawned
        if (competitor != null) {
            competitor.activeAgent = null;
            competitor.deathTime = Time.time;
            competitor.deaths++;
        }
    }

    void OnCollisionEnter( Collision coll ) {
        CollideWith( coll.collider, coll.contacts[0].point );
    }
    void OnTriggerEnter( Collider colld ) {
        CollideWith( colld, Vector3.zero );
    }

    void CollideWith( Collider colld, Vector3 hitLoc ) {
        SensedObject so = colld.transform.GetComponentInParent<SensedObject>();
        if (so != null) {
            SensoryInput si = new SensoryInput(SensoryInput.eSense.touch, so);
            si.hitLoc = hitLoc;
            List <SensoryInput> lSI = new List<SensoryInput>( new SensoryInput[] { si } );
            AIUpdate(lSI);

            so.Contact(this); // Let the SensedObject know that we made contact

            // If this was a bullet, report the hit
            if (so is Bullet) {
                ArenaManager.REPORT_BULLET_HIT_ON_AGENT(so as Bullet);
            }
        }
        
    }

    /// <summary>
    /// This method is called by ArenaManager:FixedUpdate() to clear this Agents sensed field 
    /// right before the sensed information for the new FixedUpdate() is input.
    /// Doing this fixes the issue we found with touch data wiping out other SensoryInput.
    /// </summary>
    public void ClearSenses() {
        sensed.Clear();
    }
	
    // This agent gets information about inputs. You should override this.
    public virtual void AIUpdate(List<SensoryInput> inputs) {
        // check lifetime
        int lifeTime = (int) ((Time.time - spawnTime)/ArenaManager.AGENT_SETTINGS.timeAliveSeconds);
        if (lifeTime > lifeTimeCount) {
            lifeTimeCount = lifeTime;
            if (competitor != null) {
                competitor.timeAliveCount++;
            }
        }

        if (inputs.Count == 0)
            return;

        // sensed = new List<SensoryInput>(inputs); // Moved into the foreach loop below
        // Now with the addition of ClearSenses() sensed is reset discretely, at the beginning of each
        // ArenaManager.FixedUpdate() and new inputs are just added to it.

        foreach (SensoryInput si in inputs) {
            // Add this SensoryInput to sensed
            if (sensed.Contains(si)) { // If the same SensoryInput is already in sensed, skip this input
                continue;
            }
            sensed.Add(si);

            if (si.sense == SensoryInput.eSense.touch) { // Only bullets touch us now
                if (si.type == eSensedObjectType.bullet) {
                    // OUCH!
                    health--;
                    if (health <= 0) {
                        ArenaManager.DEATH(this);
                        ArenaManager.REPORT_BULLET_CAUSED_AGENT_DEATH(si.obj as Bullet);
                    }
                }
                if (si.type == eSensedObjectType.item && si.obj is PickUp) {
                    PickUp pu = si.obj as PickUp;
                    switch (pu.puType) {
                        case PickUp.eType.ammo:
                            ammo = Mathf.Clamp( ammo+ArenaManager.AGENT_SETTINGS.pickUpAmmoAmt, 0,
                                ArenaManager.AGENT_SETTINGS.agentAmmoMax );
                            break;

                        case PickUp.eType.health:
                            health = Mathf.Clamp( health+ArenaManager.AGENT_SETTINGS.pickUpHealthAmt, 0,
                                ArenaManager.AGENT_SETTINGS.agentHealthMax );
                            break;
                    }
                }
            }
        }

#if DEBUG_SENSES
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        foreach (SensoryInput si in inputs) {
            sb.Append(si+"\n");
        }
        if (DEBUG_SENSES_TEXT) Debug.Log(sb);
#endif

    }


    public void LookLeft() {
        Look(-1);
    }

    public void LookRight() {
        Look(1);
    }

    public void LookCenter() {
        float y = headRotY;
        float rotSpeed = ArenaManager.AGENT_SETTINGS.headAngularSpeed * Time.fixedDeltaTime;
        if (Mathf.Abs(y) < rotSpeed) {
            headRotY = 0;
        } else {
            if (y<0) {
                LookRight();
            } else {
                LookLeft();
            }
        }
    }

    public void Look(float posNeg) {
        posNeg = Mathf.Clamp(posNeg, -1, 1);
        float y = headRotY;
        float rotSpeed = ArenaManager.AGENT_SETTINGS.headAngularSpeed * Time.fixedDeltaTime;
        y = y+rotSpeed*posNeg;
        headRotY = y;
    }

    /// <summary>
    /// Attempt to rotate a number of degrees left (neg) or right (pos)
    /// </summary>
    /// <param name="deg">Deg.</param>
    public void LookTheta(float deg) {
        float rotSpeed = ArenaManager.AGENT_SETTINGS.headAngularSpeed * Time.fixedDeltaTime;
        deg = Mathf.Clamp(deg, -rotSpeed, rotSpeed);
        headRotY += deg;
    }

    public float headRotY {
        get {
            float y = headTrans.localRotation.eulerAngles.y;
            while (y<-180) {
                y += 360;
            }
            while (y>180) {
                y -= 360;
            }
            return y;
        }
        set {
            if (Mathf.Abs(value - headRotY) > 10) {
                Debug.LogAssertion("Agent:headRotY:set - attempting to set Y from "+headRotY+" to "+value);
            }
            value = Mathf.Clamp(value, -ArenaManager.AGENT_SETTINGS.headAngleMinMax, ArenaManager.AGENT_SETTINGS.headAngleMinMax);
            headTrans.localRotation = Quaternion.Euler(0,value,0);
        }
    }

    public eSensedObjectType type {
        get {
            return TYPE;
        }
    }

    public Vector3 pos {
        get { return transform.position; }
    }

    public Vector3 dir {
        get { return headTrans.forward; }
    }

    public string GetName() {
        return gameObject.name;
    }

    public void Contact(SensingObject so) {
        // Do nothing
    }

    public virtual void OnDrawGizmos() {
        if (Application.isEditor && Application.isPlaying && ArenaManager.DEBUG_SENSES) {
            if (lEye == null) {
                // Set up the Eye transforms for OnDrawGizmos
                Vector3 visionDist = Vector3.forward * ArenaManager.VISION_DIST;
                lEye = new GameObject("lEye").transform;
                lEye.SetParent(headTrans);
                Quaternion qL = Quaternion.Euler(0,-ArenaManager.VISION_HALF_ARC_DEG,0);
                lEye.localPosition = qL * visionDist;
                rEye = new GameObject("rEye").transform;
                rEye.SetParent(headTrans);
                Quaternion rL = Quaternion.Euler(0,ArenaManager.VISION_HALF_ARC_DEG,0);
                rEye.localPosition = rL * visionDist;
            }

            foreach (SensoryInput si in sensed) {
                if (!ArenaManager.AGENT_SETTINGS.drawLinesToBullets && si.type == eSensedObjectType.bullet) {
                    continue;
                }
                switch (si.sense) {
                    case SensoryInput.eSense.hearing:
                        Gizmos.color = Color.blue;
                        break;
                    case SensoryInput.eSense.vision:
                        Gizmos.color = Color.green;
                        break;
                    case SensoryInput.eSense.touch:
                        Gizmos.color = Color.red;
                        continue; // This stops a null ref exception below for bullets that have been destroyed
//                        break;
                }
                Gizmos.DrawLine(pos+Vector3.up, si.pos+Vector3.up);
//                Gizmos.DrawRay(pos+Vector3.up, (si.pos-pos).normalized*4);
            }
            // Draw Hearing
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(pos+Vector3.up, ArenaManager.HEARING_DIST);
            // Draw Vision
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pos+Vector3.up/2, lEye.position+Vector3.up/2);
            Gizmos.DrawLine(pos+Vector3.up/2, rEye.position+Vector3.up/2);
        }
    }

    /// <summary>
    /// Fire a bullet from the Gun
    /// </summary>
    protected void Fire() {
        if (Time.time < lastShot + ArenaManager.AGENT_SETTINGS.bulletShotDelay) {
            return;
        }

        if (gun == null) {
            Debug.LogWarning("Agent:Fire() – Attempted to fire when gun==null. name:"+gameObject.name);
            return;
        }

        if (ammo <= 0) {
            return; // No ammo to fire!
        }
        ammo--;

        GameObject bGO = Instantiate<GameObject>(ArenaManager.AGENT_SETTINGS.bulletPrefab);
        Bullet bull = bGO.GetComponent<Bullet>();
        bull.Fire(this, competitor);

        lastShot = Time.time;
    }


    public List<SensoryInput> GetTouchedBullets() {
        List<SensoryInput> l = new List<SensoryInput>();
        foreach (SensoryInput si in sensed) {
            if (si.sense == SensoryInput.eSense.touch && si.type == eSensedObjectType.bullet) {
                l.Add(si);
            }
        }
        return l;
    }


    public Color color {
        get { return _color; }
        set {
            _color = value;
            MeshRenderer[] rends = GetComponentsInChildren<MeshRenderer>();
            foreach (MeshRenderer rend in rends) {
                if (rend.material.color != Color.black) {
                    rend.material.color = _color;
                }
            }

        }
    }
}
