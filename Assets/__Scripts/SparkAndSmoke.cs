using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script handles the visual effects for Agent health and Bullet hits on Agents.
/// </summary>
public class SparkAndSmoke : MonoBehaviour {
    public float        minSmokeEmission = 0.5f;
    public float        maxSmokeEmission = 20;

    private Agent       agent;
    private int         lastHealth;
    ParticleSystem      parts;
    ParticleSystem.EmissionModule   emitter;

	// Use this for initialization
	void Start () {
        agent = GetComponentInParent<Agent>();
        lastHealth = agent.health;
        parts = GetComponent<ParticleSystem>();
        emitter = parts.emission;
        emitter.rateOverTime = 0;
	}
	
	// FixedUpdate is called 
	void Update () {
        // If health == lastHealth, we don't need to do anything
        if (agent.health == lastHealth) return;
        lastHealth = agent.health;

        // If health != lastHealth, then we've been hit or gained health this FixedUpdate()
        // Get the bullets that hit us (should just be one, but just in case)
        List<SensoryInput> lSI = agent.GetTouchedBullets();
        // TODO: Spawn a Spark & Smoke @ each bullet point
        foreach (SensoryInput si in lSI) {
            parts.Emit(4);
            GameObject decal = Instantiate<GameObject>(ArenaManager.AGENT_SETTINGS.bulletDecalPrefab);
            Vector3 toBullet = si.hitLoc - agent.pos;
            toBullet.y = 0;
            toBullet.Normalize();
            toBullet *= 0.5f;
            Vector3 height = new Vector3(0,si.pos.y,0);
            decal.transform.position = agent.pos + toBullet + height;
            decal.transform.LookAt(decal.transform.position + toBullet);
            decal.transform.SetParent(agent.transform, true);
        }

        // Check health vs. max health and turn it into a number 0 to 1
        float u = 1 - ((float) lastHealth / (float) ArenaManager.AGENT_SETTINGS.agentHealthMax);
        u = u*u;
        // Interpolate the emitter.rateOverTime
        emitter.rateOverTime = (1-u)*minSmokeEmission + u*maxSmokeEmission;
	}
}
