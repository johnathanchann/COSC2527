// Adapted from: https://github.com/SebLague/Pathfinding-2D

using UnityEngine;

// This is a class to track a node in the A* Grid
// It tracks a number of properties that we need to compute A*
public class Node : IHeapItem<Node>
{
    // Can the node be reached (ie, no obstacle)
    public bool walkable;
    public bool slowArea;
    public bool grassArea;
    public bool pondArea;
    public bool dynamicObstacle;

    // The actual world position of the node
    public Vector2 worldPosition;

    // The grid cell of the node
    public int gridX;
    public int gridY;

    // G Cost to get to this node
    public float gCost;
    public float speedMultiplier;

    // H Cost for this node
    public float hCost;

    // Parent for this node (for reconstructing the path)
    public Node parent;

    // For heap management
    int heapIndex;


    public Node(bool _walkable, bool _slowArea, bool _grassArea, bool _pondArea, Vector2 _worldPos, int _gridX, int _gridY, float _speedMultiplier)


    {
        walkable = _walkable;
        slowArea = _slowArea;
        grassArea = _grassArea;
        grassArea = _pondArea;
        dynamicObstacle = false;
        worldPosition = _worldPos;
        gridX = _gridX;
        gridY = _gridY;
        speedMultiplier = _speedMultiplier;
    }

    public float GetSpeedMultiplier()
    {
        return speedMultiplier;
    }

    public Node Clone()
    {

        return new Node(walkable, slowArea, grassArea, pondArea, worldPosition, gridX, gridY, speedMultiplier);
    }

    public float fCost
    {
        get
        {
            return gCost + hCost;
        }
    }

    public int HeapIndex
    {
        get
        {
            return heapIndex;
        }
        set
        {
            heapIndex = value;
        }
    }

    public int CompareTo(Node nodeToCompare)
    {
        int compare = fCost.CompareTo(nodeToCompare.fCost);
        if (compare == 0)
        {
            compare = hCost.CompareTo(nodeToCompare.hCost);
        }
        return -compare;
    }
}
