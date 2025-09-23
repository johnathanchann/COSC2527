using Globals;
using SteeringCalcs;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
public class Snake : ParentSnake
{
    public SnakeState State;
    public AStarGrid astarGrid;
    public Pathfinding pathfinding;
    // Obstacle avoidance parameters (see the assignment spec for an explanation).
    public AvoidanceParams AvoidParams;


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

    // References for gameobject controls
    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Animator _animator;

    private Node[] path;
    private Waypoint[] waypoints;
    private int currentPathIndex;
    private Node fixedFinalNode;
    private float currentSpeed;

    public LayerMask obstacleMask; // Assign this in the Inspector (e.g., "Wall" or "Obstacle" layer)

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

        path = new Node[0]; // Initialized as List<Node>
        waypoints = new Waypoint[0]; // Initialized as List<Waypoint>
        currentPathIndex = 0;
    }

    void FixedUpdate()
    {
        currentSpeed = MaxSpeed * astarGrid.NodeFromWorldPoint(transform.position).GetSpeedMultiplier();
        Debug.Log(currentSpeed);

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
        for (int i = 1; i < path.Length; i++)
        {
            Debug.DrawLine(path[i].worldPosition, path[i - 1].worldPosition, Color.black);
        }
        int combinedMask = astarGrid.dynamicObstacleMask | astarGrid.unwalkableMask;
        Vector2 repulsion = pathfinding.GetRepulsion(transform.position, combinedMask);
        desiredVel += repulsion;
        desiredVel = Vector2.ClampMagnitude(desiredVel, currentSpeed);
        if (repulsion.magnitude > MaxSpeed / 2)
        {
            CalculatePath();
        }
        Vector2 steering = Steering.DesiredVelToForce(desiredVel, _rb, AccelTime, MaxAccel);
        _rb.AddForce(steering);
    }


    private Vector2 FollowPath()
    {
        Vector2 desiredVel = Vector2.zero;

        while (currentPathIndex < waypoints.Length - 1 && waypoints[currentPathIndex].HasCrossedLine(transform.position))
        {
            currentPathIndex++;
        }
        if (currentPathIndex + 1 >= path.Length)
        {
            return desiredVel;
        }

        Node currentNode = path[currentPathIndex + 1];
        if (currentPathIndex + 1 == path.Length - 1)
        {
            desiredVel = stateHandling();
        }
        else
        {
            desiredVel = Steering.Seek(transform.position, currentNode.worldPosition, currentSpeed, AvoidParams);
        }
        return desiredVel;

    }


    private Vector2 stateHandling()
    {
        Vector2 desiredAction = Vector2.zero;
        if (State == SnakeState.Attack)
        {
            return Steering.Seek(transform.position, _target, currentSpeed, AvoidParams);

        }
        else if (State == SnakeState.Fleeing)
        {
            return Steering.Flee((Vector2)gameObject.transform.position, (Vector2)Frog.transform.position, currentSpeed, AvoidParams);

        }
        else
        {
            return Steering.ArriveDirect(transform.position, _target, ArriveRadius, currentSpeed);
        }
    }


    private void CalculatePath()
    {
        Node startNode = astarGrid.NodeFromWorldPoint(transform.position);
        Node targetNode = astarGrid.NodeFromWorldPoint(_target);

        if (startNode != null && targetNode != null && startNode != targetNode)
        {
            path = pathfinding.RequestPath(transform.position, _target);
            currentPathIndex = 0;
            waypoints = pathfinding.GetWaypoints(path);
        }
        else if (startNode == targetNode)
        {
            path = new Node[0];
        }
        else
        {
            path = new Node[0];
        }
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
                HandleEvent(SnakeEvent.NotScared);
            }
        }

        else
        {
            _target = _home;
        }

        bool shouldRecalculateForTargetChange = previousTarget != _target;

        bool shouldRecalculateForEmptyPath = (path == null || path.Length == 0);

        if (shouldRecalculateForTargetChange || shouldRecalculateForEmptyPath)
        {
            CalculatePath();
            Debug.Log("Recalculating path");
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
        if (State == SnakeState.Attack && collision.gameObject == Frog)
        {
            Frog frogComponent = (this.Frog).GetComponent<Frog>();
            frogComponent.TakeDamage();
            HandleEvent(SnakeEvent.BitFrog);
        }
    }

    public override Vector2 getPos()
    {
        return transform.position;
    }

    public override bool attackingState()
    {
        return State == SnakeState.Attack;
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Bubble"))
        {
            HandleEvent(SnakeEvent.HitByBubble);
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
}
