using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using UnityEngine.Assertions;




public struct NameAndPositionToShootAt
{
    public string name;
    public Vector3 pos;

    public NameAndPositionToShootAt(string name, Vector3 pos)
    {
        this.name = name;
        this.pos = pos;
    }
}

public struct BotPosition
{
    public string name;
    public Vector3 pos;
    public float timeSeen;
    public bool seenLastFrame;

    public BotPosition(string name, Vector3 pos, float timeSeen, bool seenLastFrame)
    {
        this.name = name;
        this.pos = pos;
        this.timeSeen = timeSeen;
        this.seenLastFrame = seenLastFrame;
    }
}

public struct Memory
{
    public List<BotPosition> observedPositions;
}

public class APStudent : Agent {
    public enum eBehavior { toSpawn };
    List<BotPosition> botsSeenLastFrame;

    [Header("Inscribed JBAgentShooter")]
    public eBehavior        behavior;
    public Transform        navMeshTarget;
    public float            targetProximity = 1f;


    [SerializeField]
    private Vector3         navMeshTargetLoc = new Vector3(-99999,-99999,-99999);
    private SpawnPoint      sPoint;

    static Memory MEM;

   

    private void Start()
    {
        base.Start();
        if (APStudent.MEM.observedPositions == null)
        {
            APStudent.MEM.observedPositions = new List<BotPosition>();
        }
        botsSeenLastFrame = new List<BotPosition>();
    }

    void Update ()
    {
        switch (behavior)
        {
            case eBehavior.toSpawn:
                if (sPoint == null || (transform.position - sPoint.transform.position).magnitude < targetProximity ) {
                    SpawnPoint.eType t = SpawnPoint.RANDOM_SPAWN_POINT_TYPE();
                    List<SpawnPoint> sPoints = SpawnPoint.GET_SPAWN_POINTS(t);
                    if (sPoints.Count == 0) {
                        sPoint = null;
                        break;
                    }
                    sPoint = sPoints[Random.Range(0,sPoints.Count)];
                    navMeshTargetLoc = sPoint.transform.position;
                    nmAgent.SetDestination(navMeshTargetLoc);
                }
                break;
        }
    }

    public override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        if (Application.isPlaying)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawLine(transform.position+transform.up/2, navMeshTargetLoc+transform.up/2);
        }
    }

    public override void AIUpdate(List<SensoryInput> inputs) {
        List<BotPosition> botsSeenThisFrame = new List<BotPosition>();

        base.AIUpdate(inputs); // AIUpdate copies inputs into sensed
        bool sawSomeone = false;
        Vector3 toClosestEnemy = Vector3.one * 1000;
        foreach (SensoryInput si in sensed)
        {
            switch (si.sense)
            {
                case SensoryInput.eSense.vision:
                    if (si.type == eSensedObjectType.enemy)
                    {
                        sawSomeone = true;
                        var info = new BotPosition(si.name, si.pos, Time.time, botsSeenLastFrame.Any(oldInfo => oldInfo.name == si.name)); 
                        botsSeenThisFrame.Add(info);
                        APStudent.MEM.observedPositions.Add(info);

                        /*
                        // old shooting code
                        // Check to see whether the Enemy is within the firing arc
                        // The dot product of two vectors is the magnitude of A * the magnitude of B * cos(the angle between them)
                        Vector3 toEnemy = si.pos - pos;
                        if (toEnemy.magnitude < toClosestEnemy.magnitude) {
                            toClosestEnemy = toEnemy;
                        }

                        float dotProduct = Vector3.Dot(headTrans.forward, toEnemy.normalized);
                        float theta = Mathf.Acos(dotProduct) * Mathf.Rad2Deg;
                        if (theta <= ArenaManager.AGENT_SETTINGS.bulletAimVarianceDeg) {
                            if (ammo > 0) {
                                Fire();
                            }
                        }
                        */
                    }
                    break;
            }
        }
        var possibleTargetsToShootAt = new List<NameAndPositionToShootAt>();
        foreach (var bot in botsSeenThisFrame)
        {
            if (bot.seenLastFrame)
            {
                // Get all observed data from this bot, with the most recent data first, 
                // up until the last time we didn't see them for two frames in a row
                var observedData = (from botPosition in APStudent.MEM.observedPositions
                                   where botPosition.name == bot.name
                                   select botPosition).Reverse().TakeWhile(botPosition => botPosition.seenLastFrame).ToList();
                // We only care to shoot at a bot we've seen for at least 3 frames
                var linePoints = new List<BotPosition>();
                if (observedData.Count >= 3)
                {
                    // collect all the past info about the line the bot is traveling along
                    // past info is useful because when we get the position, we often get stale data for multiple frames in a row
                    // so by aggregating previous data we can get a much more accurate picture of the bot's velocity.
                    if (checkColinear(observedData[0].pos, observedData[1].pos, observedData[2].pos))
                    {
                        linePoints.Add(observedData[0]);
                        linePoints.Add(observedData[1]);
                        linePoints.Add(observedData[2]);
                        for (int i = 1; i < observedData.Count - 2; i++)
                        {
                            if (checkColinear(observedData[i].pos, observedData[i + 1].pos, observedData[i + 2].pos))
                            {
                                linePoints.Add(observedData[i + 2]);
                            }
                            else
                            {
                                break;
                            }
                        }

                        var newestPos = linePoints[0];
                        var oldestPos = linePoints.Last();
                        Assert.IsTrue(newestPos.timeSeen == Time.time, "Targeting based off stale data!");
                        Assert.IsTrue(newestPos.name == oldestPos.name, "Targeting is mixing differnt bots together!");
                        if (newestPos.pos != oldestPos.pos)
                        {
                            var targetVelocity = (oldestPos.pos - newestPos.pos) / (oldestPos.timeSeen - newestPos.timeSeen);
                            // get the position we'd need to shoot at to hit the target
                            var shouldShootAt = FirstOrderIntercept(transform.position, Vector3.zero, ArenaManager.AGENT_SETTINGS.bulletSpeed, newestPos.pos, targetVelocity);
                            possibleTargetsToShootAt.Add(new NameAndPositionToShootAt(newestPos.name, shouldShootAt));
                        }

                    }
                    
                }

            }
        }

        if (possibleTargetsToShootAt.Count > 0 && ammo > 0)
        {
            var withinFacingDir = (from pTarget in possibleTargetsToShootAt
                                   where Mathf.Abs(getAngleTo(pTarget.pos)) < ArenaManager.AGENT_SETTINGS.headAngleMinMax
                                   select pTarget).ToList();
            if (withinFacingDir.Count > 0)
            {
                var closest = withinFacingDir[0];
                foreach (var botPos in withinFacingDir)
                {
                    var closestAngle = Mathf.Abs(getAngleTo(closest.pos));
                    var botPosAngle = Mathf.Abs(getAngleTo(botPos.pos));
                    if (botPosAngle < closestAngle)
                    {
                        closest = botPos;
                    }
                }

                var angleToShootAt = getAngleTo(closest.pos);

                if (angleToShootAt <= ArenaManager.AGENT_SETTINGS.bulletAimVarianceDeg)
                {
                    Fire();
                }
                else
                {
                    LookTheta(angleToShootAt);
                }
            }

        }
        else
        {
            LookCenter();
        }



        
        if (health > 0) {
//            nmAgent.SetDestination(nmAgent.destination);
        }


        botsSeenLastFrame = botsSeenThisFrame;
    }


    float getAngleTo(Vector3 pos)
    {
        return Vector3.SignedAngle(transform.forward, pos - transform.position, Vector3.up);
    }

    /// <summary>
    /// This function will attempt to shoot at the target (rotating if this is not currently possible)
    /// </summary>
    /// <param name="target"></param>
    void ShootAt(SensoryInput target)
    {
        if (target.type == eSensedObjectType.enemy) // only shoot at enemies
        {

        }
    }



    // Code taken from https://wiki.unity3d.com/index.php/Calculating_Lead_For_Projectiles
    /// <summary>
    /// This attempts to find the position to shoot at to intercept a target ("leading shots"). Returns the target position if it is not possible to intercept the target
    /// </summary>
    /// <param name="shooterPosition"></param>
    /// <param name="shooterVelocity"></param>
    /// <param name="shotSpeed"></param>
    /// <param name="targetPosition"></param>
    /// <param name="targetVelocity"></param>
    /// <returns>The position to shoot at</returns>
    public static Vector3 FirstOrderIntercept
    (
        Vector3 shooterPosition,
        Vector3 shooterVelocity,
        float shotSpeed,
        Vector3 targetPosition,
        Vector3 targetVelocity
    )
    {
        Vector3 targetRelativePosition = targetPosition - shooterPosition;
        Vector3 targetRelativeVelocity = targetVelocity - shooterVelocity;
        float t = FirstOrderInterceptTime
        (
            shotSpeed,
            targetRelativePosition,
            targetRelativeVelocity
        );
        return targetPosition + t * (targetRelativeVelocity);
    }
    //first-order intercept using relative target position
    public static float FirstOrderInterceptTime
    (
        float shotSpeed,
        Vector3 targetRelativePosition,
        Vector3 targetRelativeVelocity
    )
    {
        float velocitySquared = targetRelativeVelocity.sqrMagnitude;
        if (velocitySquared < 0.001f)
            return 0f;

        float a = velocitySquared - shotSpeed * shotSpeed;

        //handle similar velocities
        if (Mathf.Abs(a) < 0.001f)
        {
            float t = -targetRelativePosition.sqrMagnitude /
            (
                2f * Vector3.Dot
                (
                    targetRelativeVelocity,
                    targetRelativePosition
                )
            );
            return Mathf.Max(t, 0f); //don't shoot back in time
        }

        float b = 2f * Vector3.Dot(targetRelativeVelocity, targetRelativePosition);
        float c = targetRelativePosition.sqrMagnitude;
        float determinant = b * b - 4f * a * c;

        if (determinant > 0f)
        { //determinant > 0; two intercept paths (most common)
            float t1 = (-b + Mathf.Sqrt(determinant)) / (2f * a),
                    t2 = (-b - Mathf.Sqrt(determinant)) / (2f * a);
            if (t1 > 0f)
            {
                if (t2 > 0f)
                    return Mathf.Min(t1, t2); //both are positive
                else
                    return t1; //only t1 is positive
            }
            else
                return Mathf.Max(t2, 0f); //don't shoot back in time
        }
        else if (determinant < 0f) //determinant < 0; no intercept path
            return 0f;
        else //determinant = 0; one intercept path, pretty much never happens
            return Mathf.Max(-b / (2f * a), 0f); //don't shoot back in time
    }

    /// <summary>
    /// Takes 3 vectors and checks if they're colinear. This isn't exact unfortunately due to unavoidable floating point errors.
    /// This assumes the points are laid out on the line like endpoint1----center----endpoint2
    /// </summary>
    /// <param name="endpoint1"></param>
    /// <param name="center"></param>
    /// <param name="endpoint2"></param>
    /// <returns></returns>
    public bool checkColinear(Vector3 endpoint1, Vector3 center, Vector3 endpoint2)
    {
        var angle = Vector3.Angle(endpoint1 - center, endpoint2 - center);
        return endpoint1 == center || center == endpoint2 || angle > 178;
    }




}


