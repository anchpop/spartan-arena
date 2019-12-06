using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour, SensedObject {
    static private Transform            ANCHOR;
    static private eSensedObjectType    TYPE = eSensedObjectType.bullet;



    public  Agent           agent { get; private set; }
    public  Competitor      competitor { get; private set; }
    private Renderer        rend;
    private Rigidbody       rigid;
    private TrailRenderer   trail;
    private Vector3         origin;

    void Awake() {
        if (ANCHOR == null) {
            GameObject go = new GameObject();
            go.name = "Bullet Anchor";
            ANCHOR = go.transform;
            ANCHOR.position = Vector3.zero;
        }

        rend = transform.Find("Capsule").GetComponent<Renderer>();
        rigid = GetComponent<Rigidbody>();
        trail = GetComponent<TrailRenderer>();
        trail.enabled = false;

        transform.SetParent(ANCHOR, true);
    }

    public void Fire(Agent eA, Competitor eC) {
        agent = eA;
        competitor = eC;
        rigid = GetComponent<Rigidbody>();
        Vector3 pos = agent.gun.position;
        pos.y = 1;
        transform.position = pos;
        transform.rotation = agent.gun.rotation;
//        trail.material.color = agent.color;
        trail.startColor = trail.endColor = agent.color;
        trail.enabled = true;
        rend.material.color = agent.color;

        float deviation = Mathf.Pow(Random.value, 2); // Give the deviation a bell-curve distribution
        deviation *= (Random.value < 0.5f) ? -1 : 1;
        Quaternion rot = transform.rotation;

        float deg = ArenaManager.AGENT_SETTINGS.bulletAimVarianceDeg;
        // This line just varies around the Y axis
//        rot = rot * Quaternion.Euler( 0, deg * deviation, 0 );
        // This line varies in a cone
        rot = rot * Quaternion.Euler( 2 * deviation, deg * deviation, 0 );
        //rot = rot * Quaternion.Euler( ArenaManager.AGENT_SETTINGS.bulletAimVarianceDeg * deviation, 0, Random.Range(0,360) );

        transform.rotation = rot;
        rigid.velocity = rot * Vector3.forward * ArenaManager.AGENT_SETTINGS.bulletSpeed;

        name = "Bullet "+agent.GetName();

        origin = transform.position;
        Invoke("DieOff", ArenaManager.AGENT_SETTINGS.bulletLifetime);

        ArenaManager.ADD_SENSABLE(this);
    }

    void OnCollisionEnter( Collision coll ) {
        SensingObject so = coll.transform.GetComponentInParent<SensingObject>();
        // A SensingObject will handle the Bullet on its own, so we don't need to continue
        if (so != null) {
            return;   
        }

        // This was replaced by the Collider.Raycast() stuff below, but I wanted you to see that it was another option.
//        // Sometimes the collision normal is backwards. Fix this using the dot product
//        Vector3 cNormal = coll.contacts[0].normal;
//        Vector3 cPoint = coll.contacts[0].point;
//        if (Vector3.Dot(cNormal, cPoint-coll.transform.position) < 0) {
//            cNormal *= -1;
//        }
        
        // Sometimes the collision information is trash, so make use of Collider.Raycast!
        Ray bRay = new Ray(origin, dir);
        RaycastHit hitInfo;
        if (coll.collider.Raycast( bRay, out hitInfo, 100000 )) {
            // Now we have much more accurate info about where the hit occurred (no mistakes due to fixedDeltaTime)
            // Leave behind a Bullet Decal
            GameObject decal = Instantiate<GameObject>(ArenaManager.AGENT_SETTINGS.bulletDecalPrefab);
            decal.transform.position = hitInfo.point + hitInfo.normal*0.01f;
            decal.transform.LookAt(decal.transform.position + hitInfo.normal);
            // Make this decal a child of the decal parent
            decal.transform.SetParent(ArenaManager.DECAL_PARENT, true);
        }


        // Because this didn't hit a SensingObject (e.g., it hit a Wall), it needs to destroy iteslf
        // TODO: Could add a decal to the wall if we wanted to.
        Destroy(this.gameObject);

//        if (so != null) {
//            SensoryInput si = new SensoryInput(SensoryInput.eSense.touch, this);
//            so.AIUpdate(new List<SensoryInput>( new SensoryInput[] { si } ));
//        }
//        Destroy(this.gameObject); // Bullet disappears after hitting anything
    }

    void DieOff() {
        Destroy(this.gameObject);
    }

    void OnDestroy() {
        ArenaManager.REMOVE_SENSABLE(this);
    }

    #region SensedObject implementation
    public eSensedObjectType type {
        get {
            return TYPE;
        }
    }
    public string GetName() {
        return gameObject.name;
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
        if (so is Agent) {
            Destroy(this.gameObject);
        }
    }
    #endregion

}
