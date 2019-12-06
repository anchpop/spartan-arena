using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MonitorComponents;

public class MonitorCompetitors : MonoBehaviour {
    static List<Monitor>            MONITORS;
    static List<CompetitorMonitor>  COM_MONITORS;


    public enum eType { points, kills, deaths, bulletHits, timeAlive }

    eType[] types;

	// Use this for initialization
	void Start () {
        MONITORS = new List<Monitor>();

        types = (eType[]) System.Enum.GetValues( typeof(eType) );
        foreach (eType t in types) {
            MONITORS.Add( new Monitor(t.ToString()) );
        }

        COM_MONITORS = new List<CompetitorMonitor>();
        // Iterate through all of the Competitors in AgentSettings
        foreach (Competitor c in ArenaManager.AGENT_SETTINGS.competitors) {
            CompetitorMonitor cm = new CompetitorMonitor(c);
            COM_MONITORS.Add(cm);
        }
	}
	
	// Update is called once per frame
	void Update () {
        foreach (CompetitorMonitor cm in COM_MONITORS) {
            cm.Update();
        }
	}


    class CompetitorMonitor {
        Competitor      com;
        MonitorInput    pointsMon;
        MonitorInput    killsMon;
        MonitorInput    deathsMon;
        MonitorInput    bulletHitsMon;
        MonitorInput    timeAliveCountMon;

        public CompetitorMonitor(Competitor c) {
            com = c;

            pointsMon = new MonitorInput(MONITORS[0], com.name+"_Pts", c.color);
            killsMon = new MonitorInput(MONITORS[1], com.name+"_Kills", c.color);
            deathsMon = new MonitorInput(MONITORS[2], com.name+"_Deaths", c.color);
            bulletHitsMon = new MonitorInput(MONITORS[3], com.name+"_BullHts", c.color);
            timeAliveCountMon = new MonitorInput(MONITORS[4], com.name+"_Alive", c.color);
        }

        public void Update() {
            pointsMon.Sample(com.points);
            killsMon.Sample(com.kills);
            deathsMon.Sample(com.deaths);
            bulletHitsMon.Sample(com.bulletHits);
            timeAliveCountMon.Sample(com.timeAliveCount);
        }
    }
}





//
//
//using UnityEngine;
//using System.Collections;
//using MonitorComponents;
//
//public class UsingAPI : MonoBehaviour 
//{
//    Monitor monitor;
//    MonitorInput highWaveMonitorInput;
//    MonitorInput lowWaveMonitorInput;
//
//    void Awake()
//    {
//        monitor = new Monitor("My monitor");
//        highWaveMonitorInput = new MonitorInput(monitor, "high wave", Color.red);
//        lowWaveMonitorInput = new MonitorInput(monitor, "low wave", Color.magenta);
//
//    }
//
//    void Update ()
//    {
//        highWaveMonitorInput.Sample(Mathf.Sin(Mathf.PI * 2f * Time.time));
//        lowWaveMonitorInput.Sample(Mathf.Sin(Mathf.PI * 2f * 1.1f * Time.time));
//
//        if (Input.GetKeyDown(KeyCode.Space))
//        {
//            monitor.Add(new MonitorEvent() {
//                text = "Space",
//                time = Time.time
//            });
//        }
//    }
//}