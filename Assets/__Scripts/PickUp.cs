using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PickUp : MonoBehaviour, SensedObject {
    public enum eType { none, ammo, health };
    public delegate void CallbackHandler(PickUp pu);

    static public Color[]   COLORS = new Color[] { new Color(.5f, .5f, .5f), new Color(.5f,0,0), new Color(0,.5f,0) };
    static public Color[]   T_COLORS = new Color[] { Color.white, new Color(1,0,0), new Color(0,1,0) };
    static public string[]  LETTERS = new string[] { "", "A", "H" };
    static float            ROT_SPEED = 20;
    static Transform        PICKUP_PARENT;

    [SerializeField]
    private eType       _puType;

    private Transform   tTrans;
    private TextMesh    tMesh;
    private Renderer    spRend, tRend;

    public event CallbackHandler    DestroyedCallback;

	// Use this for initialization
	void Awake () {
        spRend = GetComponent<Renderer>();
        tTrans = transform.Find("Text");
        tMesh = tTrans.GetComponent<TextMesh>();
        tRend = tTrans.GetComponent<Renderer>();

        if (PICKUP_PARENT == null) {
            PICKUP_PARENT = new GameObject().transform;
            PICKUP_PARENT.gameObject.name = "PickUp Parent";
        }
        transform.SetParent(PICKUP_PARENT, true);

        ArenaManager.ADD_SENSABLE(this);
	}
	
    public eType puType {
        get { return _puType; }
        set {
            _puType = value;
            if (tTrans == null) {
                Awake();
            }
            int pT = (int) _puType;

            spRend.material.SetColor("_EmissionColor", COLORS[pT]);
            tMesh.text = LETTERS[pT];
            tRend.material.color = T_COLORS[pT];
        }
    }

    void Update() {
        transform.Rotate(0, ROT_SPEED*Time.deltaTime, 0);
    }

    void OnDestroy() {
        ArenaManager.REMOVE_SENSABLE(this);
        if (DestroyedCallback != null) {
            DestroyedCallback(this);
        }
    }

    #region SensedObject implementation
    public string GetName() {
        return gameObject.name;
    }
    public eSensedObjectType type {
        get {
            return eSensedObjectType.item;
        }
    }
    public Vector3 pos {
        get {
            // Returns a position on the floor
            return transform.position+Vector3.down;
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

    #region Static Methods
    static int numAmmo=0, numHealth=0;
    static public PickUp.eType RandomType() {
        eType[] types = (eType[]) System.Enum.GetValues( typeof(eType) );
        eType pickUpType;

        // This is ugly, but it works fine – JB
        if (numAmmo - numHealth > 1) {
            pickUpType = eType.health;
        } else if (numHealth - numAmmo > 1) {
            pickUpType = eType.ammo;
        } else {
            pickUpType = types[1 + Random.Range(0, types.Length-1)];
        }

        if (pickUpType == eType.ammo) numAmmo++;
        if (pickUpType == eType.health) numHealth++;
            
        return pickUpType;
    }
    #endregion

}
