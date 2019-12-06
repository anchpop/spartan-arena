using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface SensingObject {
    void AIUpdate(List<SensoryInput> inputs);
}
