using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent( typeof( TMP_Text ) )]
public class StatsViewer : MonoBehaviour {
    List<Competitor>    comps;
    TMP_Text            tmpText;

    void Awake() {
        tmpText = GetComponent<TMP_Text>();
    }

	// Update is called once per frame
	void FixedUpdate () {
        if (comps == null) {
            if (ArenaManager.AGENT_SETTINGS == null || 
                ArenaManager.AGENT_SETTINGS.competitors == null) {
                return;
            }

            // Create a sorted version of the list of Competitors
            comps = new List<Competitor>(ArenaManager.AGENT_SETTINGS.competitors);
        }

        // Sort all the competitors by points and name
        // This shows calling the Sort(Comparison<T>) overload using 
        //   an anonymous method for the Comparison delegate. 
        // This method sorts by points with the greater points first.
        // If points are the same, it sorts alphabetically by name
        comps.Sort(delegate(Competitor a, Competitor b)
            {
                if (a.points < b.points) return 1;
                if (a.points > b.points) return -1;
                return a.name.CompareTo(b.name);
            });

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("Name\tPts\tKil\tDth\tBlt\tAlv\n");
        foreach (Competitor com in comps) {
            sb.Append("<color=");
            sb.Append(HexConverter(com.color));
            sb.Append('>');
            sb.Append(com.name);
            sb.Append("\t\t");
            sb.Append(com.points);
            sb.Append('\t');
            sb.Append(com.kills);
            sb.Append('\t');
            sb.Append(com.deaths);
            sb.Append('\t');
            sb.Append(com.bulletHits);
            sb.Append('\t');
            sb.Append(com.timeAliveCount);
            sb.Append("</color>");
            sb.Append('\n');
        }

        tmpText.text = sb.ToString();
	}

    string HexConverter(Color c)
    {
        string rtn = string.Empty;
        rtn = "#" + ToHex(c.r) + ToHex(c.g) + ToHex(c.b);
//        try
//        {
//        }
//        catch (System.Exception ex)
//        {
//            //doing nothing
//        }

        return rtn;
    }

    string ToHex(float f) {
        int i = (int) (f*255);
        return i.ToString("X2");
    }

}
