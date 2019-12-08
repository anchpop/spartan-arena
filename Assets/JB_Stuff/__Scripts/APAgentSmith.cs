using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using System.Linq;
using UnityEngine.Assertions;


// What increases our score:
// shooting enemies (+1 point)
// killing enemies  (+5 points)
// staying alive    (+1 point per 10 seconds)

// We also would prefer to avoid getting killed because it subtracts 5 points
// I don't think there's any good way to avoid getting killed besides making sure you remain topped up on health
// Maybe you could try and get behind cover but since you can't feel where your shots are coming from doing any kind of 
// counterattack is very tricky.

// Arena knowledge lasts forever and is important bc of that. Even if it's only worth 2 util a minute,
// over the course of an hour that's 120 utils for something that really doesn't take that long.
// So lets put a high priority on gathering game knowlege, especially at the beginning of the match.
// Lets say gathering game info is worth 120 utils for the first 10 minutes, otherwise 0.

// When we see someone, lets calculate the expected value of chasing them down and killing them.
// This is the odds of us being able to kill them, times (their health plus 5).
// I want to do something where I remember who is an easy kill and who isn't, but for now I'll just say each point of health of theirs
// will take 3 bullets + 1 second to deplete. So someone with 10 health will take us 30 bullets and 10 seconds to kill for 15 points. 
// We'll know they've been killed if they were solidly within our sight and then they disappear. \

// Bullets are worth nothing on their own, but they are required for kills. So lets say the utility for getting ammo is (100-bulletsremaining)/10. 
// Health is ez because we can just say getting health is worth 15 utils whenever our health is less than 3, or 7 whenever health is less than 6. 
// We can calculate the time taken to get somewhere as (difference in x + difference in y)/agentspeed. 

// Now how do we actually decide what to do.
// We weigh everything we could be doing, think of how many utils its worth and divide by how much time it will take. Then do whichever is the highest.
// sometimes we can do two things at once, like try to grab health while chasing someone, lets make sure to take that into account.


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
    public int health;

    public BotPosition(string name, Vector3 pos, float timeSeen, bool seenLastFrame, int health)
    {
        this.name = name;
        this.pos = pos;
        this.timeSeen = timeSeen;
        this.seenLastFrame = seenLastFrame;
        this.health = health;
    }
}

public struct SpawnPointInfo
{
    public PickUp.eType typ;
    public Vector3 pos;

    public SpawnPointInfo(PickUp.eType typ, Vector3 pos)
    {
        this.typ = typ;
        this.pos = pos;
    }
}


public struct ClosestPosToExploreAndUtility
{
    public Vector3 pos;
    public float utility;

    public ClosestPosToExploreAndUtility(Vector3 pos, float utility)
    {
        this.pos = pos;
        this.utility = utility;
    }

    public override string ToString()
    {
        return "Explore: " + utility.ToString();
    }
}

public struct EnemyToHuntAndUtility
{
    public BotPosition pos;
    public Vector3 shootAtPosition;
    public float utility;

    public EnemyToHuntAndUtility(BotPosition pos, Vector3 shootAtPosition, float utility)
    {
        this.pos = pos;
        this.shootAtPosition = shootAtPosition;
        this.utility = utility;
    }

    public override string ToString()
    {
        return "Hunting: " + utility.ToString();
    }
}

public struct PickupToSeekAndUtility
{
    public Vector3 pos;
    public float utility;

    public PickupToSeekAndUtility(Vector3 pos, float utility)
    {
        this.pos = pos;
        this.utility = utility;
    }

    public override string ToString()
    {
        return "Pickup: " + utility.ToString();
    }
}

public struct SpawnPointVisited
{
    public Vector3 pos;
    public float time;

    public SpawnPointVisited(Vector3 pos, float time)
    {
        this.pos = pos;
        this.time = time;
    }
}

public struct Memory
{
    public List<BotPosition> observedPositions;
    public List<SpawnPointInfo> observedSpawnPoints;
    public List<SpawnPointVisited> spawnPointsVisited;
}

public class APAgentSmith : Agent {
    List<BotPosition> botsSeenLastFrame;
    
    public Transform        navMeshTarget;
    public float            targetProximity = 1f;


    [SerializeField]
    private Vector3         navMeshTargetLoc = new Vector3(-99999,-99999,-99999);
    private SpawnPoint      sPoint;

    static Memory MEM;


    [TextArea]
    [Tooltip("Doesn't do anything. Just comments shown in inspector")]
    public string Notes = "This component shouldn't be removed, it does important stuff.";



    int bulletsToDoOneDamage = 3;
    int snagRadius = 5;
    int healthPickupUtilityDanger = 15;
    int healthPickup6UtilitySafer = 7;
    int aggressiveness = 1;
    int healthPackAttraction = 1;
    int ammoPackAttraction = 1;

    private void Start()
    {
        base.Start();
        APAgentSmith.MEM.observedPositions = new List<BotPosition>();
        if (APAgentSmith.MEM.observedSpawnPoints == null)
        {
            APAgentSmith.MEM.observedSpawnPoints = new List<SpawnPointInfo>();
        }
        if (APAgentSmith.MEM.spawnPointsVisited == null)
        {
            APAgentSmith.MEM.spawnPointsVisited = new List<SpawnPointVisited>();
        }
        botsSeenLastFrame = new List<BotPosition>();
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

    public override void AIUpdate(List<SensoryInput> inputs)
    {
        Notes = "";
        if (health > 0)
        {
            {
                List<SpawnPoint> sPoints = SpawnPoint.GET_SPAWN_POINTS(SpawnPoint.eType.item);
                var nearPoints = (from point in sPoints
                                  where (point.pos - transform.position).magnitude < targetProximity
                                  select new SpawnPointVisited(point.pos, Time.time)).ToList();
                if (nearPoints.Count > 0)
                {
                    APAgentSmith.MEM.spawnPointsVisited = (from point in APAgentSmith.MEM.spawnPointsVisited
                                                           where point.pos != nearPoints[0].pos
                                                           where point.time > Time.time - 60
                                                           select point).ToList();
                    APAgentSmith.MEM.spawnPointsVisited.Add(nearPoints[0]);
                }
            }

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
                            var info = new BotPosition(si.name, si.pos, Time.time, botsSeenLastFrame.Any(oldInfo => oldInfo.name == si.name), si.health);
                            botsSeenThisFrame.Add(info);
                            APAgentSmith.MEM.observedPositions.Add(info);
                        }
                        else if (si.type == eSensedObjectType.item && si.obj is PickUp)
                        {
                            PickUp pu = si.obj as PickUp;
                            var info = new SpawnPointInfo(pu.puType, si.pos);
                            if (!APAgentSmith.MEM.observedSpawnPoints.Contains(info))
                            {
                                APAgentSmith.MEM.observedSpawnPoints.Add(info);
                            }
                        }
                        break;
                }
            }





            var exploration = getExplorationUtility();
            var hunting = getHuntingUtility(botsSeenThisFrame);
            var pickup = getSeekPickupUtility();
            
            Notes += (exploration.HasValue ? exploration.Value.ToString() : "Exploration: null") + "\n" + (hunting.HasValue ? hunting.Value.ToString() : "Hunting: null") + "\n" + (pickup.HasValue ? pickup.Value.ToString() : "Pickup: null"); 
            if (exploration.HasValue && hunting.HasValue && pickup.HasValue)
            {
                var explorationv = exploration.Value;
                var huntingv = hunting.Value;
                var pickupv = pickup.Value;
                if (huntingv.utility >= explorationv.utility && huntingv.utility >= pickupv.utility)
                    Hunt(huntingv);
                else
                {
                    if (explorationv.utility >= pickupv.utility)
                        Explore(explorationv);
                    else
                        SeekPickup(pickupv, botsSeenThisFrame);
                }
            }
            else if (exploration.HasValue && hunting.HasValue)
            {
                var explorationv = exploration.Value;
                var huntingv = hunting.Value;
                if (huntingv.utility >= explorationv.utility)
                    Hunt(huntingv);
                else
                    Explore(explorationv);
            }
            else if (hunting.HasValue && pickup.HasValue)
            {
                var huntingv = hunting.Value;
                var pickupv = pickup.Value;
                if (huntingv.utility >= pickupv.utility)
                    Hunt(huntingv);
                else SeekPickup(pickupv, botsSeenThisFrame);
            }
            else if (exploration.HasValue && pickup.HasValue)
            {
                var explorationv = exploration.Value;
                var pickupv = pickup.Value;
                if (explorationv.utility >= pickupv.utility)
                    Explore(explorationv);
                else SeekPickup(pickupv, botsSeenThisFrame);
            }
            else if (exploration.HasValue)
            {
                var explorationv = exploration.Value;
                Explore(explorationv);
            }
            else if (hunting.HasValue)
            {
                var huntingv = hunting.Value;
                Hunt(huntingv);
            }
            else if (pickup.HasValue)
            {
                var pickupv = pickup.Value;
                SeekPickup(pickupv, botsSeenThisFrame);
            }
            else
            {
                LookCenter();
                ExploreRandomly();
            }

            botsSeenLastFrame = botsSeenThisFrame;
        }
    }



    ClosestPosToExploreAndUtility? getExplorationUtility()
    {
        List < SpawnPoint > iPoints = SpawnPoint.GET_SPAWN_POINTS(SpawnPoint.eType.item);
        var pointsToExplore = (from point in iPoints
                               where !APAgentSmith.MEM.observedSpawnPoints.Any(info => info.pos == point.pos)
                               where !APAgentSmith.MEM.spawnPointsVisited.Any(info => info.pos == point.pos)
                               select point.pos).ToList();
        if (pointsToExplore.Count > 0)
        {
            var closestPoint = pointsToExplore[0];
            foreach (var point in pointsToExplore)
            {
                if (getTravelTimeTo(point) < getTravelTimeTo(closestPoint))
                {
                    closestPoint = point;
                }
            }
            return new ClosestPosToExploreAndUtility(closestPoint, (Time.time < 5 * 60 ? 120 : 1) / getTravelTimeTo(closestPoint));
        }
        return null;
    }
    
    Vector3? Lead(BotPosition bot)
    {
        if (bot.seenLastFrame)
        {
            // Get all observed data from this bot, with the most recent data first, 
            // up until the last time we didn't see them for two frames in a row
            var observedData = (from botPosition in APAgentSmith.MEM.observedPositions
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
                    Assert.IsTrue(newestPos.timeSeen >= oldestPos.timeSeen, "Targeting is using data in the wrong order!");
                    if (newestPos.timeSeen <= oldestPos.timeSeen)
                        print("???");
                    else if (newestPos.pos != oldestPos.pos)
                    {
                        var targetVelocity = (oldestPos.pos - newestPos.pos) / (oldestPos.timeSeen - newestPos.timeSeen);
                        // get the position we'd need to shoot at to hit the target
                        var shouldShootAt = FirstOrderIntercept(transform.position, Vector3.zero, ArenaManager.AGENT_SETTINGS.bulletSpeed, newestPos.pos, targetVelocity);
                        return shouldShootAt;
                    }

                }

            }

        }
        return null;
    }


    EnemyToHuntAndUtility? getHuntingUtility(List<BotPosition> botsSeenThisFrame)
    {
        if (botsSeenThisFrame.Count > 0)
        {
            var weakestBot = botsSeenThisFrame[0];
            foreach (var bot in botsSeenThisFrame)
            {
                if (bot.health < weakestBot.health)
                {
                    weakestBot = bot;
                }
            }
            var shotsRequired = weakestBot.health * bulletsToDoOneDamage;
            if (ammo < shotsRequired) return null;
            
           
            if (weakestBot.seenLastFrame)
            {
                var shouldShootAt = Lead(weakestBot);
                if (shouldShootAt.HasValue)
                {
                    return new EnemyToHuntAndUtility(weakestBot, shouldShootAt.Value, (shotsRequired + 5.0f * aggressiveness) / (shotsRequired * 3.0f));
                }


            }


            
        }
        
        return null;
    }


    PickupToSeekAndUtility? getSeekPickupUtility()
    {
        var seekAmmoUtility = ammo < 50 ? (100 - ammo) / 5 : (100 - ammo) / 20;
        var seekHealthUtility  = health < 3 ? healthPickupUtilityDanger : (health < 6 ? healthPickup6UtilitySafer : 0);
        var healthPickups = (from point in APAgentSmith.MEM.observedSpawnPoints
                             where point.typ == PickUp.eType.health
                             where !APAgentSmith.MEM.spawnPointsVisited.Any(info => info.pos == point.pos)
                             select point.pos).ToList();
        var ammoPickups = (from point in APAgentSmith.MEM.observedSpawnPoints
                           where point.typ == PickUp.eType.ammo
                           where !APAgentSmith.MEM.spawnPointsVisited.Any(info => info.pos == point.pos)
                           select point.pos).ToList();
        PickupToSeekAndUtility? healthUtil = null;
        PickupToSeekAndUtility? ammoUtil = null;
        if (healthPickups.Count > 0)
        {
            var cHealth = healthPickups[0];
            foreach (var health in healthPickups)
            {
                if (getTravelTimeTo(health) < getTravelTimeTo(cHealth))
                {
                    cHealth = health;
                }
            }
            healthUtil = new PickupToSeekAndUtility(cHealth, healthPackAttraction * seekHealthUtility / getTravelTimeTo(cHealth));
        }
        if (ammoPickups.Count > 0)
        {
            var cAmmo = ammoPickups[0];
            foreach (var ammo in ammoPickups)
            {
                if (getTravelTimeTo(ammo) < getTravelTimeTo(cAmmo))
                {
                    cAmmo = ammo;
                }
            }
            ammoUtil = new PickupToSeekAndUtility(cAmmo, ammoPackAttraction * seekAmmoUtility / getTravelTimeTo(cAmmo));
        }
        if (ammoUtil == null) return healthUtil;
        if (healthUtil == null) return ammoUtil;
        return healthUtil.Value.utility > ammoUtil.Value.utility ? healthUtil : ammoUtil;
    }


    float getAngleToBody(Vector3 pos)
    {
        return Vector3.SignedAngle(transform.forward, pos - transform.position, Vector3.up);
    }
    float getAngleToHead(Vector3 pos)
    {
        return Vector3.SignedAngle(headTrans.forward, pos - transform.position, Vector3.up);
    }

    void ExploreRandomly()
    {
        if (sPoint == null || (transform.position - sPoint.transform.position).magnitude<targetProximity)
        {
            SpawnPoint.eType t = SpawnPoint.RANDOM_SPAWN_POINT_TYPE();
            List<SpawnPoint> sPoints = SpawnPoint.GET_SPAWN_POINTS(t);
            if (sPoints.Count == 0)
            {
                sPoint = null;
                return;
            }
            sPoint = sPoints[Random.Range(0, sPoints.Count)];
            navMeshTargetLoc = sPoint.transform.position;
            nmAgent.SetDestination(navMeshTargetLoc);
        }
    }

    /// <summary>
    /// This function will instruct the bot to attempt to hunt down a target. It does this by moving to the target's position and trying to shoot where it's going to be.
    /// </summary>
    /// <param name="target"></param>
    void Hunt(EnemyToHuntAndUtility target)
    {
        var angleToShootAt = getAngleToHead(target.shootAtPosition);

        if (Mathf.Abs(angleToShootAt) <= ArenaManager.AGENT_SETTINGS.bulletAimVarianceDeg + .5)
        {
            Fire();
        }
        else
        {
            LookTheta(angleToShootAt);
        }
        
        var snaggablesWithinRange = (from point in APAgentSmith.MEM.observedSpawnPoints
                                     where !APAgentSmith.MEM.spawnPointsVisited.Any(info => info.pos == point.pos)
                                     where (point.pos - transform.position).magnitude < snagRadius
                                     where Vector3.Angle(point.pos - transform.position, target.pos.pos - transform.position) < 100
                                     where (point.pos - transform.position).sqrMagnitude < (target.pos.pos - transform.position).sqrMagnitude
                                     select point).ToList();
        if (snaggablesWithinRange.Count > 0 && 
              ((snaggablesWithinRange[0].typ == PickUp.eType.health && health < 10) || 
               (snaggablesWithinRange[0].typ == PickUp.eType.ammo   && ammo   < 100)))
        {
            print("snagging");
            sPoint = null;
            navMeshTargetLoc = snaggablesWithinRange[0].pos; 
            nmAgent.SetDestination(navMeshTargetLoc);
        }
        else
        {
            sPoint = null;
            navMeshTargetLoc = target.pos.pos; // we navigate to them rather than to where they're going to be because we want to stay behind them
            nmAgent.SetDestination(navMeshTargetLoc);
        }
    }

    void Explore(ClosestPosToExploreAndUtility explore)
    {
        LookCenter();
        sPoint = null;
        if (navMeshTargetLoc != explore.pos)
        {
            navMeshTargetLoc = explore.pos;
            nmAgent.SetDestination(navMeshTargetLoc);
        }
    }


    void SeekPickup(PickupToSeekAndUtility pickup, List<BotPosition> botsSeenThisFrame)
    {
        //print("seeking pickup at " + pickup.pos + " | health: " + health + " | ammo: " + ammo );
        var toShootAt = (from bot in botsSeenThisFrame
                         where Lead(bot).HasValue
                         select Lead(bot).Value).ToList();
        if (toShootAt.Count > 0 && ammo > 0)
        {
            var target = toShootAt[0];
            foreach (var possibleTarget in toShootAt)
            {
                if (Mathf.Abs(getAngleToHead(possibleTarget)) < Mathf.Abs(getAngleToHead(target)))
                {
                    target = possibleTarget;
                }
            }
            var angleToShootAt = getAngleToHead(target);
            if (Mathf.Abs(angleToShootAt) <= ArenaManager.AGENT_SETTINGS.bulletAimVarianceDeg + .5)
            {
                Fire();
            }
            else
            {
                LookTheta(angleToShootAt);
            }
        }
        else
        {
            LookCenter();
        }
        sPoint = null;
        if (navMeshTargetLoc != pickup.pos)
        {
            navMeshTargetLoc = pickup.pos;
            nmAgent.SetDestination(navMeshTargetLoc);
        }
    }

    float getTravelTimeTo(Vector3 pos)
    {
        bool GetPath(NavMeshPath p, Vector3 fromPos, Vector3 toPos, int passableMask)
        {
            p.ClearCorners();

            if (NavMesh.CalculatePath(fromPos, toPos, passableMask, p) == false)
                return false;

            return true;
        }

        float GetPathLength(NavMeshPath p)
        {
            float lng = 0.0f;

            if ((p.status != NavMeshPathStatus.PathInvalid) && (p.corners != null) && (p.corners.Length > 1))
            {
                for (int i = 1; i < p.corners.Length; ++i)
                {
                    lng += Vector3.Distance(p.corners[i - 1], p.corners[i]);
                }
            }
            return lng;
        }



        var path = new NavMeshPath();
        NavMesh.CalculatePath(transform.position, pos, NavMesh.AllAreas, path);
        return /*(Mathf.Abs(transform.position.x - pos.x) + Mathf.Abs(transform.position.y - pos.y))*/ GetPathLength(path) * ArenaManager.AGENT_SETTINGS.agentSpeed;
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


