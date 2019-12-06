using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Competitor {
    public string       name;
    public bool         enabled = true;
    public Color        color;
    public GameObject   prefabWithAgent;
    [Header("Dynamic")]
    public int          points =0;
    public int          kills = 0;
    public int          deaths = 0;
    public int          bulletHits = 0;
    public int          timeAliveCount = 0;
    public Agent        activeAgent = null;
    public float        deathTime;

    public int CalculatePoints() {
        points = kills * ArenaManager.AGENT_SETTINGS.pointsPerKill;
        points += deaths * ArenaManager.AGENT_SETTINGS.pointsPerDeath;
        points += bulletHits * ArenaManager.AGENT_SETTINGS.pointsPerBulletHit;
        points += timeAliveCount * ArenaManager.AGENT_SETTINGS.pointsPerTimeAlive;
        return points;
    }
}


[CreateAssetMenu(menuName = "AgentSettings ScriptableObject", fileName = "AgentSettings.asset")]
public class AgentSettings : ScriptableObject {

    [Header("Agent Settings")]
    public bool         debugSenses = true;
    public bool         drawLinesToBullets = false;
    public float        agentSpeed = 10f;
    public float        agentAngularSpeed = 180f;
    public float        agentAccel = 40f;
    public int          agentAmmoStart = 50;
    public int          agentAmmoMax = 100;
    public int          agentHealthStart = 10;
    public int          agentHealthMax = 10;
    public float        headAngularSpeed = 360f;
    public float        headAngleMinMax = 90f;

    public float        agentHearingDist = 4;
    public float        agentVisionDist = 20;
    public float        agentVisionHalfArcDeg = 45;

    public GameObject   agentPrefab;
    public float        agentRespawnDelay = 4;

    [Header("Arena Settings")]
    public bool         buildWalls = true;
    public int          numWallsPerFloor = 2;
    [Tooltip("The X and Z scales are max.\nThe Y is the fixed height of all walls.")]
    public Vector3      wallMaxScale = new Vector3(8,2,8);
    public float        spawnPointProtectedRange = 2;
    public GameObject   wallPrefab;

    [Header("Bullet Settings")]
    public float        bulletSpeed = 20f;
    public float        bulletShotDelay = 0.2f;
    public float        bulletAimVarianceDeg = 10;
    public float        bulletLifetime = 10f;
    public GameObject   bulletPrefab;

    [Header("PickUp Settings")]
    public bool         spawnAtStart = false;
    public float        spawnDelay = 5f;
    public GameObject   pickUpPrefab;
    public int          pickUpAmmoAmt = 50;
    public int          pickUpHealthAmt = 5;

    [Header("Point Values")]
    public int          pointsPerKill = 5;
    public int          pointsPerDeath = -5;
    public int          pointsPerBulletHit = 1;
    public int          pointsPerTimeAlive = 1;
    public float        timeAliveSeconds = 10;


    [Header("Other Settings")]
    public GameObject   flagPrefab;
    public GameObject   sparkAndSmokePrefab;
    public GameObject   bulletDecalPrefab;
    public float        bulletDecalLifetime = 60;

    [Header("Competitors!")]
    public bool         resetStatsOnLaunch = true;
    public List<Competitor> competitors;


    public void ResetStats() {
        foreach (Competitor com in competitors) {
            com.kills = 0;
            com.deaths = 0;
            com.bulletHits = 0;
            com.timeAliveCount = 0;
        }
    }
}
