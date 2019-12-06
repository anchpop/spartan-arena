using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum eSensedObjectType { item, enemy, bullet, spawn, none }

public interface SensedObject {
//    public enum eType { item, enemy }
    eSensedObjectType type {get;}
    Vector3 pos {get;}
    Vector3 dir {get;}
    string GetName();
    void Contact(SensingObject so);
}
