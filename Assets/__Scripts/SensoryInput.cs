using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct SensoryInput {
    public enum eSense { vision, hearing, touch, none }

    public eSense               sense;
    public SensedObject         obj;
    public Vector3              pos;
    public Vector3              dir;
    public eSensedObjectType    type;
    public string               name;
    public int                  health;
    public Vector3              hitLoc;

    public SensoryInput( eSense eSen, SensedObject eObj ) {
        sense = eSen;
        obj = eObj;
        if (obj != null) {
            pos = obj.pos;
            dir = obj.dir;
            type = obj.type;
            name = obj.GetName();
            if (eObj is Agent) {
                health = (eObj as Agent).health;
            } else {
                health = -1;
            }
        } else {
            pos = Vector3.zero;
            dir = Vector3.forward;
            type = eSensedObjectType.none;
            name = "";
            health = -1;
        }
        hitLoc = Vector3.zero;
    }

    override public string ToString() {
        return sense+": "+name;
    }
}
