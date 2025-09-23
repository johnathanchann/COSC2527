using System.Collections.Generic;
using UnityEngine;
using SteeringCalcs;
using BehavorialTree;
using Globals;
using System;

public class Frog : MonoBehaviour
{
    // Frog status.
    public static int Health;

    public Pathfinding pathfinding;
    public AStarGrid astarGrid;
    public AvoidanceParams AvoidParams;
    public static int fly_eat;

    // Steering parameters.
    public float MaxSpeed;
    public float MaxAccel;
    private float currentSpeed;
    public float AccelTime;

    // The arrival radius is set up to be dynamic, depending on how far away
    // the player right-clicks from the frog. See the logic in Update().
    public float ArrivePct;
    public float MinArriveRadius;
    public float MaxArriveRadius;
    private float _arriveRadius;

    // Turn this off to make it easier to see overshooting when seek is used
    // instead of arrive.
    public bool HideFlagOnceReached;

    private float lastCooldown = -Mathf.Infinity;

    public int mode = 0;


    public float cooldown = 1.0f;

    // References to various objects in the scene that we want to be able to modify.
    private Transform _flag;
    private SpriteRenderer _flagSr;
    private DrawGUI _drawGUIScript;
    private Animator _animator;
    private Rigidbody2D _rb;

    public SpriteRenderer _frogSr;

    // Stores the last position that the player right-clicked. Initially null.
    public Vector2? _lastClickPos;

    //WEEK6
    //Used by DTs to make decision
    public float scaredRange;
    public float huntRange;
    public Fly closestFly;
    public ParentSnake closestSnake;
    public Fire closestFireball;
    public float distanceToClosestFly;
    public float distanceToClosestSnake;

    public float distanceToClosestFireball;
    public float anchorWeight;
    public Vector2 AnchorDims;

    public float bubble_speed = 5f;

    public GameObject bubblePrefab;

    public float PrevHealth;
    private Node[] path;
    private Waypoint[] waypoints;

    public static int input = 0;


    public static bool human = false;
    private Node fixedFinalNode;
    private int currentPathIndex;
    private BehaviourTree tree;
    // private Tree behaviourTree;
    public Vector2 behaviourVel;
    void Start()
    {

        // Initialise the various object references.
        _flag = GameObject.Find("Flag").transform;
        _flagSr = _flag.GetComponent<SpriteRenderer>();
        _flagSr.enabled = false;
        _frogSr = transform.GetComponent<SpriteRenderer>();

        Health = 3;
        GameObject uiManager = GameObject.Find("UIManager");
        if (uiManager != null)
        {
            _drawGUIScript = uiManager.GetComponent<DrawGUI>();
        }

        _animator = GetComponent<Animator>();

        _rb = GetComponent<Rigidbody2D>();

        _lastClickPos = null;
        _arriveRadius = MinArriveRadius;
        path = new Node[0];
        waypoints = new Waypoint[0];
        currentPathIndex = 0;


        tree = build();

    }

    void Update()
    {

        // Check if the player right-clicked (mouse button #1).
        if (human && Input.GetMouseButtonDown(1))
        {
            _lastClickPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // Set the arrival radius dynamically.
            _arriveRadius = Mathf.Clamp(ArrivePct * ((Vector2)_lastClickPos - (Vector2)transform.position).magnitude, MinArriveRadius, MaxArriveRadius);

            _flag.position = (Vector2)_lastClickPos + new Vector2(0.55f, 0.55f);
            _flagSr.enabled = true;

            Debug.Log("Path length: " + path.Length);
            path = pathfinding.RequestPath(transform.position, (Vector2)_lastClickPos);
            waypoints = pathfinding.GetWaypoints(path);
            currentPathIndex = 0;
            // if (path.Length > 0)
            // {
            //     fixedFinalNode = path[path.Length - 1].Clone();
            //     fixedFinalNode.worldPosition = (Vector2)_lastClickPos;
            //     path[path.Length - 1] = fixedFinalNode;
            //     currentPathIndex = 0;
            // }
        }
        if (human && Input.GetKeyDown(KeyCode.Space))
        {
            ShootBubble();
        }
        if (Input.GetMouseButtonDown(0))
        {
            human = !human;
        }
        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            input = 0;
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            input = 1;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            input = 2;
        }
        else // show the relevant info about fly and snake
        {
            if (closestFly != null)
                Debug.DrawLine(transform.position, closestFly.transform.position, Color.black);
            if (closestSnake != null)
                Debug.DrawLine(transform.position, closestSnake.transform.position, Color.red);
        }
    }

    void FixedUpdate()
    {
        currentSpeed = MaxSpeed * astarGrid.NodeFromWorldPoint(transform.position).GetSpeedMultiplier();
        Debug.Log(currentSpeed);
        findClosestFly();
        findClosestFireball();
        findClosestSnake();
        Vector2 desiredVel = getVelo();
        Debug.DrawLine((Vector2)transform.position, (Vector2)transform.position + desiredVel, Color.blue);
        Vector2 steering = Steering.DesiredVelToForce(desiredVel, _rb, AccelTime, MaxAccel);
        for (int i = 0; i < waypoints.Length; i++)
        {
            waypoints[i].line.Draw(0.5f);
        }
        for (int j = 1; j < path.Length; j++)
        {
            Debug.DrawLine(path[j - 1].worldPosition, path[j].worldPosition, Color.green);
        }
        _rb.AddForce(steering);

        UpdateAppearance();
    }

    private void Tracking(Vector2 currentPos)
    {
        if (path.Length > 0)
        {
            fixedFinalNode = path[path.Length - 1].Clone();
            fixedFinalNode.worldPosition = currentPos;
            path[path.Length - 1] = fixedFinalNode;
            currentPathIndex = 0;
        }

    }

    private void DeTouringCheck()
    {

        int bufferBeforeObstacle = 5; // How many nodes before the obstacle to start detouring
        for (int i = 0; i < path.Length; i++)
        {
            Node currentNode = path[i];

            // Check current node and its neighbors for dynamic obstacles
            bool nearDynamicObstacle = currentNode.dynamicObstacle;
            if (!nearDynamicObstacle)
            {
                foreach (Node neighbor in astarGrid.GetNeighbours(currentNode))
                {
                    if (neighbor.dynamicObstacle)
                    {
                        nearDynamicObstacle = true;
                        break;
                    }
                }
            }

            if (nearDynamicObstacle)
            {
                Debug.Log("Recalculate due to nearby dynamic obstacle");
                int detourIndex = Mathf.Max(0, i - bufferBeforeObstacle);
                Node detourStartNode = path[detourIndex];

                Vector2 pathDir = ((Vector2)currentNode.worldPosition - (Vector2)detourStartNode.worldPosition).normalized;
                Vector2 detourDir = Vector2.Perpendicular(pathDir).normalized;
                float detourDistance = 2f;

                Vector2 detourTarget = detourStartNode.worldPosition + detourDir * detourDistance;

                Node detourNode = astarGrid.NodeFromWorldPoint(detourTarget);
                if (!detourNode.walkable || detourNode.dynamicObstacle)
                {
                    // Try the other side
                    detourTarget = detourStartNode.worldPosition - detourDir * detourDistance;
                }

                _lastClickPos = detourTarget;
                path = pathfinding.RequestPath((Vector2)transform.position, detourTarget);

                Tracking(detourTarget);
                break;
            }
        }
        for (int i = 1; i < path.Length; i++)
        {
            Debug.DrawLine(path[i - 1].worldPosition, path[i].worldPosition, Color.black);
        }
    }

    private void UpdateAppearance()
    {
        if (_rb.linearVelocity.magnitude > Constants.MIN_SPEED_TO_ANIMATE)
        {
            _animator.SetBool("Walking", true);
            transform.up = _rb.linearVelocity;
        }
        else
        {
            _animator.SetBool("Walking", false);
        }
    }

    public void TakeDamage()
    {
        if (Health > 0)
        {
            Health--;
        }
    }

    private Vector2 getVelo()
    {
        Vector2 res = Vector2.zero;
        if (input == 0)
        {
            res = decideMovement_v1();
        }
        else if (input == 1)
        {
            res = decideMovement_v2();
        }
        else if (input == 2)
        {
            res = decideMovement_v3();
        }
        return res;
    }

    private Vector2 decideMovement_v1()
    {
        if (Health <= 0)
        {
            // Debug.Log(Health);
            MaxSpeed = 0.0f;
            _frogSr.color = new Color(1.0f, 0.2f, 0.2f);
            return (Vector2.zero);
        }
        else
        {
            //safeguard from screen
            if (isOutOfScreen(transform))
            {
                Vector2 desiredAnch = anchorWeight * Steering.GetAnchor(transform.position, AnchorDims);

                return desiredAnch.normalized * MaxSpeed;
            }
            //go to that click
            if (human)
            {
                if (_lastClickPos != null)
                {
                    return (getVelocityTowardsFlagAStar());
                }
                return Vector2.zero;
            }

            else
            {
                //continue decision tree
                if (closestSnake != null && closestSnake.attackingState())
                {
                    if (distanceToClosestSnake <= scaredRange)
                    {
                        ShootBubble();
                    }

                    return Steering.FleeDirect(gameObject.transform.position, closestSnake.transform.position, currentSpeed);
                }
                else if (closestFireball != null)
                {
                    if (distanceToClosestFireball <= scaredRange)
                    {
                        return Steering.FleeDirect(gameObject.transform.position, closestFireball.transform.position, currentSpeed);
                    }
                }
                else
                {
                    if (!isOutOfScreen(closestFly.transform) && distanceToClosestFly <= (huntRange + 8.0))
                    {
                        return Steering.SeekDirect(gameObject.transform.position, closestFly.transform.position, currentSpeed);
                    }

                }

            }
            return Steering.SeekDirect(gameObject.transform.position, Vector2.zero, currentSpeed);

        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("FireBall"))
        {
            TakeDamage();
        }
    }
    private Vector2 decideMovement_v2()
    {
        if (Health <= 0)
        {
            // Debug.Log(Health);
            MaxSpeed = 0.0f;
            _frogSr.color = new Color(1.0f, 0.2f, 0.2f);
            return (Vector2.zero);
        }
        else
        {
            if (Health <= 2 && PrevHealth != Health)
            {
                scaredRange += 5.0f;
                if (huntRange >= 6.0f)
                {
                    huntRange -= 5.0f;
                }
                PrevHealth = Health;
            }
            else if (Health > 2 && PrevHealth != Health)
            {
                if (scaredRange >= 6.0f)
                {
                    scaredRange -= 5.0f;
                }
                huntRange += 5.0f;
                PrevHealth = Health;
            }
            if (isOutOfScreen(transform))
            {
                Vector2 desiredAnch = anchorWeight * Steering.GetAnchor(transform.position, AnchorDims);

                return desiredAnch.normalized * MaxSpeed;
            }
            if (human)
            {
                if (_lastClickPos != null)
                {
                    return (getVelocityTowardsFlagAStar());
                }
                return Vector2.zero;
            }

            else
            {
                //continue decision tree
                if (closestSnake != null && closestSnake.attackingState())
                {

                    if (distanceToClosestSnake <= scaredRange)
                    {
                        Vector2 frogview = ((Vector2)transform.up).normalized;
                        Vector2 snakedir = ((Vector2)closestSnake.transform.position - (Vector2)transform.position).normalized;
                        float facing = Vector2.Dot(frogview, snakedir);
                        if (facing > 0.6f)
                        {
                            ShootBubble();
                        }
                        int SnakeSwarm = findSnakeAttackingSwarm(scaredRange);
                        if (SnakeSwarm > 2)
                        {
                            spinShoot();
                        }

                    }
                    return Steering.FleeDirect(gameObject.transform.position, closestSnake.transform.position, currentSpeed);
                }
                else if (closestFireball != null)
                {
                    if (distanceToClosestFireball <= scaredRange)
                    {
                        return Steering.FleeDirect(gameObject.transform.position, closestFireball.transform.position, currentSpeed);
                    }
                }
                else
                {
                    if (!isOutOfScreen(closestFly.transform) && distanceToClosestFly <= (huntRange))
                    {
                        return Steering.SeekDirect(gameObject.transform.position, closestFly.transform.position, currentSpeed);
                    }
                    Vector2 closestSwarm = findClosestSwarm();
                    if (closestSwarm != Vector2.zero)
                    {
                        return Steering.SeekDirect(gameObject.transform.position, closestSwarm, currentSpeed);
                    }
                }

            }
            return Steering.SeekDirect(gameObject.transform.position, Vector2.zero, currentSpeed);

        }
    }
    private BehaviourTree build()
    {
        BehaviourTree tree = new BehaviourTree("Frog");
        Sequence FrogStart = new Sequence("Start");
        FrogStart.AddChild(new Leaf("isNotHuman", new IsNotHuman(this)));
        FrogStart.AddChild(new Leaf("isNotDead", new IsNotDead(this)));
        FrogStart.AddChild(new Leaf("HealthTweak", new HealthModeChange(this)));

        Selector mainSelector = new Selector("MainSelector");
        mainSelector.AddChild(new Leaf("InCameraView", new OutOfCameraBounds(this)));

        Selector Frogfear = new Selector("FrogFear");
        Frogfear.AddChild(new Leaf("SpinShoot", new SwarmedBySnake(this)));
        Frogfear.AddChild(new Leaf("ShootBubble", new ScaredRangeWithin(this)));
        Frogfear.AddChild(new Leaf("FleeFireball", new FireBallClose(this)));
        Frogfear.AddChild(new Leaf("RunFromSnake", new SnakeIsClose(this)));

        mainSelector.AddChild(Frogfear);

        Selector FindFly = new Selector("FindFly");
        FindFly.AddChild(new Leaf("HuntFly", new ClosestFlyWithinCamera(this)));
        FindFly.AddChild(new Leaf("NearestSwarm", new NearestSwarm(this)));


        mainSelector.AddChild(FindFly);
        mainSelector.AddChild(new Leaf("MidPointDirect", new DefaultMovement(this)));
        FrogStart.AddChild(mainSelector);
        tree.AddChild(FrogStart);
        return tree;
    }

    public float GetCurrentSpeed()
    {
        return currentSpeed;
    }

    private Vector2 decideMovement_v3()
    {

        tree.Process();
        tree.Reset();
        return behaviourVel;
    }


    public Vector2 getVelocityTowardsFlagAStar()
    {

        Vector2 desiredVel = Vector2.zero;

        while (currentPathIndex < waypoints.Length - 1 && waypoints[currentPathIndex].HasCrossedLine(transform.position))
        {
            currentPathIndex++;
        }
        if (currentPathIndex == waypoints.Length - 1 && (path[currentPathIndex + 1].worldPosition - (Vector2)transform.position).magnitude < Constants.TARGET_REACHED_TOLERANCE)
        {
            _lastClickPos = null;
            if (HideFlagOnceReached)
            {
                _flagSr.enabled = false;
            }
            return desiredVel;
        }

        Node currentNode = path[currentPathIndex + 1];

        if (currentPathIndex + 1 == path.Length - 1)
        {
            desiredVel = Steering.Arrive(gameObject.transform.position, (Vector2)_lastClickPos, _arriveRadius, currentSpeed, AvoidParams);
        }
        else
        {
            desiredVel = Steering.Seek(gameObject.transform.position, currentNode.worldPosition, currentSpeed, AvoidParams);
        }

        int combinedMask = Pathfinding.grid.dynamicObstacleMask | Pathfinding.grid.unwalkableMask;
        Vector2 repulsion = Pathfinding.instance.GetRepulsion(transform.position, combinedMask);
        desiredVel += repulsion;


        if (repulsion.magnitude > MaxSpeed / 2.0)
        {
            path = pathfinding.RequestPath(transform.position, (Vector2)_lastClickPos);
            waypoints = Pathfinding.instance.GetWaypoints(path);
            currentPathIndex = 0;
        }
        return Vector2.ClampMagnitude(desiredVel, currentSpeed);
    }


    public Vector2 getVelocityTowardsFlag()
    {
        Vector2 desiredVel = Vector2.zero;
        if (_lastClickPos != null)
        {
            if (((Vector2)_lastClickPos - (Vector2)gameObject.transform.position).magnitude > Constants.TARGET_REACHED_TOLERANCE)
            {
                desiredVel = Steering.ArriveDirect(gameObject.transform.position, (Vector2)_lastClickPos, _arriveRadius, currentSpeed);
            }
            else
            {
                _lastClickPos = null;

                if (HideFlagOnceReached)
                {
                    _flagSr.enabled = false;
                }
            }

        }
        return desiredVel;
    }

    public Vector2 findClosestSwarm()
    {
        List<List<Fly>> cluster = new List<List<Fly>>();
        HashSet<Fly> visited = new HashSet<Fly>();
        foreach (Fly fly in (Fly[])GameObject.FindObjectsByType(typeof(Fly), FindObjectsSortMode.None))
        {
            if (fly.GetComponent<Fly>().State != Fly.FlyState.Dead && !visited.Contains(fly) && !isOutOfScreen(fly.transform))
            {
                Queue<Fly> que = new Queue<Fly>();
                List<Fly> Swarm = new List<Fly>();
                que.Enqueue(fly);
                visited.Add(fly);
                while (que.Count > 0)
                {
                    Fly curr = que.Dequeue();
                    Swarm.Add(curr);
                    foreach (Fly flies in (Fly[])GameObject.FindObjectsByType(typeof(Fly), FindObjectsSortMode.None))
                    {
                        float dist = (flies.transform.position - curr.transform.position).magnitude;
                        if (flies.GetComponent<Fly>().State != Fly.FlyState.Dead && !visited.Contains(flies) && !isOutOfScreen(flies.transform))
                        {
                            if (dist < 10.0f)
                            {
                                que.Enqueue(flies);
                                visited.Add(flies);
                            }
                        }

                    }

                }

                if (Swarm.Count > 0)
                {
                    cluster.Add(Swarm);
                }
            }
        }

        float bestDist = Mathf.Infinity;
        Vector2 res = Vector2.zero;
        foreach (List<Fly> swarming in cluster)
        {
            Vector2 tot = Vector2.zero;
            foreach (Fly flying in swarming)
            {
                tot += (Vector2)flying.transform.position;
            }
            tot = tot / swarming.Count;
            float dist = (tot - (Vector2)transform.position).magnitude;
            if (dist < bestDist)
            {
                bestDist = dist;
                res = tot;
            }

        }
        Debug.Log($"Closest Swarm: {res}");
        return res;

    }



    public void spinShoot()
    {
        if (Time.time < lastCooldown)
        {
            float timeRemaining = lastCooldown - Time.time;
            Debug.Log($"Time until ready: {timeRemaining:F2} seconds");
            return;
        }
        Quaternion originalRot = transform.rotation;
        float angleStep = 360f / 36;

        for (int i = 0; i < 36; i++)
        {
            transform.rotation = originalRot * Quaternion.Euler(0f, 0f, angleStep * i);
            ShootBubble();
        }
        transform.rotation = originalRot;
        lastCooldown = Time.time + cooldown;

    }
    public void findClosestFly()
    {
        distanceToClosestFly = Mathf.Infinity;

        foreach (Fly fly in (Fly[])GameObject.FindObjectsByType(typeof(Fly), FindObjectsSortMode.None))
        {
            float distanceToFly = (fly.transform.position - transform.position).magnitude;
            if (fly.GetComponent<Fly>().State != Fly.FlyState.Dead && !isOutOfScreen(fly.transform))
            {
                if (distanceToFly < distanceToClosestFly)
                {
                    closestFly = fly;
                    distanceToClosestFly = distanceToFly;

                }
            }

        }
    }



    //TODO See findClosestFly for inspiration
    public void findClosestSnake()
    {
        distanceToClosestSnake = Mathf.Infinity;
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Snake"))
        {
            ParentSnake snake = obj.GetComponent<ParentSnake>();

            if (snake != null)
            {

                float distanceToSnake = (snake.transform.position - transform.position).magnitude;
                if (distanceToSnake < distanceToClosestSnake)
                {
                    closestSnake = snake;
                    distanceToClosestSnake = distanceToSnake;
                }
            }
        }
    }

    public void findClosestFireball()
    {
        distanceToClosestFireball = Mathf.Infinity;
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("FireBall"))
        {
            Fire fire = obj.GetComponent<Fire>();

            if (fire != null)
            {

                float distanceToFireball = (fire.transform.position - transform.position).magnitude;
                if (distanceToFireball < distanceToClosestFireball)
                {
                    closestFireball = fire;
                    distanceToClosestFireball = distanceToFireball;
                }
            }
        }
    }



    public int findSnakeAttackingSwarm(float scaredRange)
    {
        int count = 0;
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("Snake"))
        {
            ParentSnake snake = obj.GetComponent<ParentSnake>();
            if (snake != null)
            {
                float distanceToSnake = (snake.transform.position - transform.position).magnitude;
                if (distanceToSnake <= scaredRange && snake.attackingState())
                {
                    count++;
                }
            }
        }
        return count;
    }


    //TODO Check wether the current transform is out of screen (true) or not (false)
    public bool isOutOfScreen(Transform transform)
    {
        Vector3 screenview = Camera.main.WorldToViewportPoint(transform.position);
        // Debug.Log(screenview);
        return screenview.z < 0 || screenview.x < 0 || screenview.x > 1 || screenview.y < 0 || screenview.y > 1;
    }

    public void ShootBubble()
    {
        Vector2 spawnBubble = transform.position + (transform.up * 1.0f);
        Vector2 frogOrient = transform.up;

        GameObject new_bubble = Instantiate(bubblePrefab, spawnBubble, transform.rotation);
        Rigidbody2D frogrb = gameObject.GetComponent<Rigidbody2D>();
        Rigidbody2D rbBubble = new_bubble.GetComponent<Rigidbody2D>();
        rbBubble.linearVelocity = frogrb.linearVelocity + (frogOrient * bubble_speed);
        Collider2D frogCollider = GetComponent<Collider2D>();
        Collider2D bubbleCollider = new_bubble.GetComponent<Collider2D>();
        if (frogCollider != null && bubbleCollider != null)
        {
            Physics2D.IgnoreCollision(frogCollider, bubbleCollider, true);
            Physics2D.IgnoreCollision(bubbleCollider, bubbleCollider, true);
        }
    }

}
