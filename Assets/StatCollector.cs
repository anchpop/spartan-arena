using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatCollector : MonoBehaviour
{

    [TextArea]
    [Tooltip("Doesn't do anything. Just comments shown in inspector")]
    public string Stats = "This component shouldn't be removed, it does important stuff.";
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Stats  = "zd0: " + Andre.APAgentSmith.MEM.Stats + "\n";
        Stats += "zd1: " + Andre1.APAgentSmith1.MEM.Stats + "\n";
        Stats += "zd2: " + Andre2.APAgentSmith2.MEM.Stats + "\n";
        Stats += "zd3: " + Andre3.APAgentSmith3.MEM.Stats + "\n";
        Stats += "zd4: " + Andre4.APAgentSmith4.MEM.Stats + "\n";
        Stats += "zd5: " + Andre5.APAgentSmith5.MEM.Stats + "\n";
        Stats += "zd6: " + Andre6.APAgentSmith6.MEM.Stats + "\n";
        Stats += "zd7: " + Andre7.APAgentSmith7.MEM.Stats + "\n";
        Stats += "zd8: " + Andre8.APAgentSmith8.MEM.Stats + "\n";
        Stats += "zd9: " + Andre9.APAgentSmith9.MEM.Stats + "\n";
        Stats += "zd10: " + Andre10.APAgentSmith10.MEM.Stats + "\n";
    }
}
