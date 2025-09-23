using Globals;
using SteeringCalcs;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class BossSnake : ParentSnake
{
    public SnakeState State;
    public AStarGrid astarGrid;
    public Pathfinding pathfinding;

    // Obstacle avoidance parameters (see the assignment spec for an explanation).
    public AvoidanceParams AvoidParams;

    public int fire_speed;


    // Steering parameters.
    public float MaxSpeed;
    public float MaxAccel;
    public float AccelTime;

    // Use this as the arrival radius for all states where the steering behaviour == arrive.
    public float ArriveRadius;

    // Parameters controlling transitions in/out of the Aggro state.
    public float AggroRange;
    public float DeAggroRange;

    // Reference to the frog (the target for the Aggro state).
    public GameObject Frog;

    // The patrol point (the target for the PatrolAway state).
    public Transform PatrolPoint;

    // The current target of the snake (see the assignment spec for an explanation).
    private Vector2 _target;

    // The snake's initial position (the target for the PatrolHome and Harmless states).
    private Vector2 _home;

    private Node previousNode;
    // Debug rendering config
    private float _debugHomeOffset = 0.3f;
    private float lastCooldown = -Mathf.Infinity;
    public float cooldown = 10.0f;
    private float lastSuperCooldown = -Mathf.Infinity;
    public float supercooldown = 10.0f;



    // References for gameobject controls
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Animator _animator;

    private List<Node> path;
    private int currentPathIndex;
    private Node fixedFinalNode;

    public LayerMask obstacleMask; // Assign this in the Inspector (e.g., "Wall" or "Obstacle" layer)
    public GameObject firePrefab;

    public bool isboss;
    private int HitCount;
    private bool hasFlee;



    // Snake FSM states (don't edit this enum).
    public enum SnakeState : int
    {
        PatrolAway = 0,
        PatrolHome = 1,
        Attack = 2,
        Benign = 3,
        Fleeing = 4
    }

    // Snake FSM events (don't edit this enum).
    public enum SnakeEvent : int
    {
        FrogInRange = 0,
        FrogOutOfRange = 1,
        BitFrog = 2,
        ReachedTarget = 3,
        HitByBubble = 4,
        NotScared = 5
    }

    // Direction IDs used by the snake animator (don't edit these).
    private enum Direction : int
    {
        Up = 0,
        Left = 1,
        Down = 2,
        Right = 3
    }

    void Start()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponent<SpriteRenderer>();
        _animator = GetComponent<Animator>();

        _home = transform.position;
        hasFlee = false;
        SetState(SnakeState.PatrolAway);


        path = new List<Node>(); // Initialized as List<Node>
        currentPathIndex = 0;
        HitCount = 0;
    }

    void FixedUpdate()
    {
        UpdateSnakeTargetPos();
        // Events triggered by each fixed update tick

        FixedUpdateEvents();

        // Update the Fly behaviour based on the current FSM state
        FSM_State();

        // Configure final appearance of the snake
        UpdateAppearance();
    }

    // Trigger Events for each fixed update tick, using a trigger first FSM implementation
    void FixedUpdateEvents()
    {
        if (State == SnakeState.PatrolHome ||
            State == SnakeState.PatrolAway)
        {
            Node currentNode = astarGrid.NodeFromWorldPoint(transform.position);

            if (previousNode != null && previousNode != currentNode)
                astarGrid.SetDynamicObstacle(previousNode.worldPosition, false); // free old cell

            astarGrid.SetDynamicObstacle(currentNode.worldPosition, true);
            previousNode = currentNode;

            if (InAggroRange())
            {
                HandleEvent(SnakeEvent.FrogInRange);
            }
            else if (AtTarget())
            {
                HandleEvent(SnakeEvent.ReachedTarget);
            }
        }
        else if (State == SnakeState.Attack)
        {
            if (isboss)
            {
                spinShoot();
            }
            ShootFire();
            if (OutOfAggroRange())
            {
                HandleEvent(SnakeEvent.FrogOutOfRange);
            }
        }
        else if (State == SnakeState.Benign)
        {
            if (AtTarget())
            {
                HandleEvent(SnakeEvent.ReachedTarget);
            }

        }
        else if (State == SnakeState.Fleeing)
        {
            if (OutOfAggroRange())
            {
                HandleEvent(SnakeEvent.NotScared);
            }
        }
    }


    // Process the current FSM state, using an event first FSM implementation
    void FSM_State()
    {

        //first calculate the path, add the dynamic pos, updating every frame for it to be movin
        Vector2 desiredVel = Vector2.zero;

        desiredVel = FollowPath();
        for (int i = 1; i < path.Count; i++)
        {
            Debug.DrawLine(path[i - 1].worldPosition, path[i].worldPosition, Color.black);
        }
        // Convert the desired velocity to a force, then apply it.
        Vector2 steering = Steering.DesiredVelToForce(desiredVel, _rb, AccelTime, MaxAccel);
        _rb.AddForce(steering);
    }


    private Vector2 FollowPath()
    {
        if (path == null || path.Count == 0)
        {
            return Vector2.zero;
        }

        if (currentPathIndex >= path.Count)
        {

            return stateHandling();
        }

        Node currentNode = path[currentPathIndex];
        float distanceToNode = Vector2.Distance(transform.position, currentNode.worldPosition);

        if (distanceToNode < Constants.TARGET_REACHED_TOLERANCE)
        {
            currentPathIndex++;
            if (currentPathIndex < path.Count)
            {
                currentNode = path[currentPathIndex];
            }
            else
            {
                return stateHandling();
                //return Steering.Arrive(transform.position, _target, MaxSpeed, ArriveRadius * 0.5f, AvoidParams);
            }
        }

        return Steering.Seek(transform.position, currentNode.worldPosition, MaxSpeed, AvoidParams);
    }


    private Vector2 stateHandling()
    {
        Node currentNode = astarGrid.NodeFromWorldPoint(transform.position);
        float effectiveSpeed = MaxSpeed;

        if (currentNode.grassArea)
        {
            effectiveSpeed *= 1.5f;
        }
        else if (currentNode.slowArea)
        {
            effectiveSpeed *= 0.4f;
        }

        Vector2 desiredAction = Vector2.zero;
        if (State == SnakeState.Attack)
        {
            return Steering.Seek(transform.position, _target, effectiveSpeed, AvoidParams);
        }
        else if (State == SnakeState.Fleeing)
        {
            return Steering.Flee((Vector2)gameObject.transform.position, (Vector2)Frog.transform.position, effectiveSpeed, AvoidParams);
        }
        else
        {
            return Steering.Arrive(transform.position, _target, effectiveSpeed, ArriveRadius * 0.5f, AvoidParams);

        }
    }


    private void CalculatePath()
    {
        Node startNode = astarGrid.NodeFromWorldPoint(transform.position);
        Node targetNode = astarGrid.NodeFromWorldPoint(_target);

        if (startNode != null && targetNode != null && startNode != targetNode)
        {
            path = pathfinding.RequestPath(transform.position, _target).ToList();

            currentPathIndex = 0;

            // Optimization: Remove the first node if it's very close to the current position
            if (path.Count > 0 && Vector2.Distance(transform.position, path[0].worldPosition) < Constants.TARGET_REACHED_TOLERANCE * 0.5f)
            {
                path.RemoveAt(0);
                if (path.Count > 0)
                {
                    currentPathIndex = 0;
                }
            }
        }
        else if (startNode == targetNode)
        {
            path.Clear();
        }
        else
        {
            path.Clear();
        }
    }
    public void spinShoot()
    {
        if (!isboss || Time.time < lastSuperCooldown)
        {
            return;
        }
        Quaternion origin = transform.rotation;
        float step = 360f / 36;
        int skip = 4;
        for (int i = 0; i < 36; i++)
        {
            if (i % (skip + 1) != 0)
                continue;

            transform.rotation = origin * Quaternion.Euler(0f, 0f, step * i);
            lastCooldown = -Mathf.Infinity;
            ShootFire();
        }
        transform.rotation = origin;
        lastSuperCooldown = Time.time + supercooldown;
    }

    // Choose the target of the snake, this depends on the FSM state
    private void UpdateSnakeTargetPos()
    {
        Vector2 previousTarget = _target;
        if (State == SnakeState.PatrolAway)
        {
            _target = PatrolPoint.position;
        }
        else if (State == SnakeState.PatrolHome)
        {
            _target = _home;
        }
        else if (State == SnakeState.Attack)
        {
            _target = Frog.transform.position;
        }
        else if (State == SnakeState.Benign)
        {
            _target = _home;
        }
        else if (State == SnakeState.Fleeing)
        {
            Vector2 curr_pos = (Vector2)gameObject.transform.position;
            if (!hasFlee)
            {
                Vector2 frog_pos = (Vector2)Frog.transform.position;
                Vector2 frog_snake_diff = (curr_pos - frog_pos).normalized;
                _target = curr_pos + frog_snake_diff * AggroRange;
                hasFlee = true;
            }

            if (Vector2.Distance(curr_pos, Frog.transform.position) >= DeAggroRange)
            {
                hasFlee = false;
                Debug.Log("Not scared.");
                HandleEvent(SnakeEvent.NotScared);
            }
        }

        else
        {
            _target = _home;
        }

        bool shouldRecalculateForTargetChange = Vector2.Distance(previousTarget, _target) > Constants.TARGET_REACHED_TOLERANCE;

        bool shouldRecalculateForEmptyPath = (path == null || path.Count == 0);

        if (shouldRecalculateForTargetChange || shouldRecalculateForEmptyPath)
        {
            CalculatePath();
        }
    }
    private void SetState(SnakeState newState)
    {
        if (newState != State)
        {
            // Can uncomment this for debugging purposes.
            //Debug.Log(name + " switching state to " + newState.ToString());

            State = newState;
        }
    }


    //TODO update so that the snake attack only alive frogs
    private void HandleEvent(SnakeEvent e)
    {

        if (State == SnakeState.PatrolAway)
        {
            if (e == SnakeEvent.FrogInRange)
            {
                SetState(SnakeState.Attack);
            }
            else if (e == SnakeEvent.ReachedTarget)
            {
                SetState(SnakeState.PatrolHome);
            }
            else if (e == SnakeEvent.HitByBubble)
            {
                SetState(SnakeState.Fleeing);
            }
        }
        else if (State == SnakeState.PatrolHome)
        {
            if (e == SnakeEvent.FrogInRange)
            {
                SetState(SnakeState.Attack);
            }
            else if (e == SnakeEvent.ReachedTarget)
            {
                SetState(SnakeState.PatrolAway);
            }
            else if (e == SnakeEvent.HitByBubble)
            {
                SetState(SnakeState.Fleeing);
            }
        }
        else if (State == SnakeState.Attack)
        {
            if (e == SnakeEvent.BitFrog)
            {
                SetState(SnakeState.Benign);
            }
            else if (e == SnakeEvent.FrogOutOfRange)
            {
                SetState(SnakeState.PatrolHome);
            }
            else if (e == SnakeEvent.HitByBubble)
            {
                SetState(SnakeState.Fleeing);
            }
        }
        else if (State == SnakeState.Benign)
        {
            if (e == SnakeEvent.ReachedTarget)
            {
                SetState(SnakeState.PatrolHome);
            }
            else if (e == SnakeEvent.HitByBubble)
            {
                SetState(SnakeState.Fleeing);
            }
        }
        else if (State == SnakeState.Fleeing)
        {
            if (e == SnakeEvent.NotScared)
            {
                SetState(SnakeState.PatrolHome);
            }
        }

    }
    private void UpdateAppearance()
    {
        // Update the snake's colour to provide a visual indication of its state.
        if (State == SnakeState.PatrolAway)
        {
            _sr.color = new Color(0.3f, 0.3f, 0.3f);
        }
        if (State == SnakeState.PatrolHome)
        {
            _sr.color = new Color(1.0f, 1.0f, 1.0f);
        }
        else if (State == SnakeState.Attack)
        {
            _sr.color = new Color(1.0f, 0.2f, 0.2f);
        }
        else if (State == SnakeState.Benign)
        {
            _sr.color = new Color(0.20f, 0.94f, 0.23f);
        }
        else if (State == SnakeState.Fleeing)
        {
            _sr.enabled = true;
            _sr.color = new Color(0.1f, 0.7f, 0.3f);

        }

        // Update the Snake visual based on the direction it's moving
        if (_rb.linearVelocity.magnitude > Constants.MIN_SPEED_TO_ANIMATE)
        {
            // Determine the bearing of the snake in degrees (between -180 and 180)
            float angle = Mathf.Atan2(_rb.linearVelocity.y, _rb.linearVelocity.x) * Mathf.Rad2Deg;

            if (angle > -135.0f && angle <= -45.0f) // Down
            {
                transform.up = new Vector2(0.0f, -1.0f);
                _animator.SetInteger("Direction", (int)Direction.Down);
            }
            else if (angle > -45.0f && angle <= 45.0f) // Right
            {
                transform.up = new Vector2(1.0f, 0.0f);
                _animator.SetInteger("Direction", (int)Direction.Right);
            }
            else if (angle > 45.0f && angle <= 135.0f) // Up
            {
                transform.up = new Vector2(0.0f, 1.0f);
                _animator.SetInteger("Direction", (int)Direction.Up);
            }
            else // Left
            {
                transform.up = new Vector2(-1.0f, 0.0f);
                _animator.SetInteger("Direction", (int)Direction.Left);
            }
        }

        Debug.DrawLine(_home + new Vector2(-_debugHomeOffset, -_debugHomeOffset), _home + new Vector2(_debugHomeOffset, _debugHomeOffset), Color.magenta);
        Debug.DrawLine(_home + new Vector2(-_debugHomeOffset, _debugHomeOffset), _home + new Vector2(_debugHomeOffset, -_debugHomeOffset), Color.magenta);
    }

    // Events for 2D collisions
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (State == SnakeState.Attack && collision.gameObject.CompareTag("Frog"))
        {
            Frog frogComponent = (this.Frog).GetComponent<Frog>();
            frogComponent.TakeDamage();
            HandleEvent(SnakeEvent.BitFrog);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Bubble"))
        {
            HitCount++;
            if (HitCount >= 2)
            {
                HandleEvent(SnakeEvent.HitByBubble);
                HitCount = 0;
            }
        }
    }


    // Helper to check if we're at the target position
    private bool AtTarget()
    {
        return ((Vector2)transform.position - _target).magnitude < Constants.TARGET_REACHED_TOLERANCE;
    }

    private bool InAggroRange()
    {
        return (transform.position - Frog.transform.position).magnitude < AggroRange;
    }

    private bool OutOfAggroRange()
    {
        return (transform.position - Frog.transform.position).magnitude > DeAggroRange;
    }

    public override Vector2 getPos()
    {
        return transform.position;
    }

    public override bool attackingState()
    {
        return State == SnakeState.Attack;
    }

    public void ShootFire()
    {

        if (Time.time < lastCooldown)
        {
            float timeRemaining = lastCooldown - Time.time;
            Debug.Log($"Time until ready: {timeRemaining:F2} seconds");
            return;
        }
        Vector2 spawnBubble = transform.position + (transform.up * 1.0f);
        Vector2 snakeOrient = transform.up;

        GameObject new_fire = Instantiate(firePrefab, spawnBubble, transform.rotation);
        Rigidbody2D snakerb = gameObject.GetComponent<Rigidbody2D>();
        Rigidbody2D rbBubble = new_fire.GetComponent<Rigidbody2D>();
        rbBubble.linearVelocity = snakerb.linearVelocity + (snakeOrient * fire_speed);
        Collider2D frogCollider = GetComponent<Collider2D>();
        Collider2D bubbleCollider = new_fire.GetComponent<Collider2D>();
        if (frogCollider != null && bubbleCollider != null)
        {
            Physics2D.IgnoreCollision(frogCollider, bubbleCollider, true);
            Physics2D.IgnoreCollision(bubbleCollider, bubbleCollider, true);
        }
        lastCooldown = Time.time + cooldown;
    }
}

