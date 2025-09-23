// Adapted from: https://github.com/SebLague/Pathfinding-2D

using UnityEngine;
using System.Collections.Generic;
using System;

public class Pathfinding : MonoBehaviour
{
    public static AStarGrid grid;
    public static Pathfinding instance;

    public float repulsionStrength = 10f;
    public float repulsionDistance = 3f;


    // This is used instead of Start() so that the
    // A* grid is only greated once when the game is launched
    void Awake()
    {
        grid = GetComponent<AStarGrid>();
        instance = this;
    }
    public Waypoint[] GetWaypoints(Node[] path)
    {
        if (path.Length < 1)
        {
            return new Waypoint[0];
        }
        float length = 3f;
        Waypoint[] waypoints = new Waypoint[path.Length - 1];
        for (int j = 1; j < path.Length; j++)
        {
            float dist = (path[j].worldPosition - path[j - 1].worldPosition).magnitude;
            float curLength = Math.Min(dist, length);

            Vector2 dir = (path[j].worldPosition - path[j - 1].worldPosition).normalized;
            waypoints[j - 1] = new Waypoint(path[j].worldPosition - curLength * dir, path[j].worldPosition - (curLength + 1) * dir);
        }
        return waypoints;
    }

    // Public callable method
    public Node[] RequestPath(Vector2 from, Vector2 to)
    {
        return instance.SimplifyPath(FindPath(from, to));
    }


    // Internal private implementation
    public Vector2 GetRepulsion(Vector2 currentPos, int combinedMask)
    {
        Collider2D[] nearbyObstacles = Physics2D.OverlapCircleAll(
            currentPos,
            repulsionDistance,
            combinedMask
        );

        Vector2 repulsion = Vector2.zero;
        foreach (var col in nearbyObstacles)
        {
            Vector2 obsPos = col.ClosestPoint(currentPos);
            Vector2 away = currentPos - obsPos;
            float dist = away.magnitude;

            if (dist < 0.0001f) continue;


            repulsion += away.normalized * (repulsionStrength / (dist * dist));
        }
        return repulsion;
    }
    Node[] FindPath(Vector2 from, Vector2 to)
    {
        // A* Waypoints to return
        Node[] waypoints = new Node[0];

        // Set to true if a path is found
        bool pathSuccess = false;

        // Starting node point - selected from the A* Grid
        Node startNode = grid.NodeFromWorldPoint(from);

        // Goal node point - selected from the A* Grid
        Node targetNode = grid.NodeFromWorldPoint(to);

        // Ensure the starting node's parent is not null
        // Also let's us detect the start node if needed
        startNode.parent = startNode;

        // Niceity check to ensure the start and target nodes are walkable
        // by the frog (such as if you clock on a object)
        // If not, we find the closest walkable point in the grid
        if (!startNode.walkable)
        {
            startNode = grid.ClosestWalkableNode(startNode);
        }
        if (!targetNode.walkable)
        {
            targetNode = grid.ClosestWalkableNode(targetNode);
        }

        if ((startNode.walkable && targetNode.walkable) || (startNode.slowArea && targetNode.slowArea))
        {
            // A* Starts here!!!
            // TODO: Your job is to fill in the missing code below the marked comments

            // Track the open set of nodes to explore, as a heap sorted by the A* Cost
            Heap<Node> openSet = new Heap<Node>(grid.MaxSize);


            // Track closed set of all visited nodes
            HashSet<Node> closedSet = new HashSet<Node>();

            // TODO: Commence A* by adding the start node to the open set

            openSet.Add(startNode);

            // Stop if we have a path or run out of nodes to explore (means no path can be found!)
            while (!pathSuccess && openSet.Count > 0)
            {
                Node currentNode = openSet.RemoveFirst();

                closedSet.Add(currentNode);

                if (currentNode == targetNode)
                {
                    pathSuccess = true;
                    break;
                }
                else
                {
                    Node[] neighbours = new Node[0];

                    foreach (Node node in grid.GetNeighbours(currentNode))
                    {

                        // TODO:If we can reach the neighbour and it is not in the closed set (repalce false)
                        if ((node.walkable || node.slowArea) && !closedSet.Contains(node))
                        {
                            // TODO: Calculate the G Cost of the neighbour node
                            float newGCost = currentNode.gCost + GetDistance(currentNode, node) / currentNode.GetSpeedMultiplier();

                            // if moving within the area and the target is also in the mud area
                            //if ((currentNode.slowArea && node.slowArea) || (currentNode.slowArea) || (node.slowArea))
                            // TOSO: If the neighbour is not in the open set OR
                            //    the neighour was previously checked and the new G Cost is less than the previous G Cost 
                            //    (repalce false)

                            if (!openSet.Contains(node) || newGCost < node.gCost)
                            {
                                // TODO: Set neightbour G Cost
                                node.gCost = newGCost;

                                // TODO: Compute and set the H Cost for the neighbour
                                float hCost = GetHeuristic(node, targetNode, selectedHeuristic);
                                node.hCost = hCost;

                                // TODO: Set the parent of the neighbour to the current node
                                node.parent = currentNode;

                                // TODO: Add neighbour to the open set, but need to check if the neighbour is already in the open set
                                // If not in the open set, then add to the heap
                                // If in the open set, then UDPATE the neighbour in the heap
                                if (!openSet.Contains(node))
                                {
                                    openSet.Add(node);
                                }
                                else
                                {
                                    openSet.UpdateItem(node);
                                }
                            }
                        }
                    }
                }
            }
        }

        if (pathSuccess)
        {
            waypoints = RetracePath(startNode, targetNode);
        }

        return waypoints;
    }

    Node[] RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();

        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);

            currentNode = currentNode.parent;

        }
        path.Add(startNode);
        Node[] waypoints = path.ToArray();
        Array.Reverse(waypoints);
        return waypoints;
    }



    public enum HeuristicType { Euclidean, Manhattan, Diagonal, TerrainAware, TurningAware, RepulsionAware }

    public HeuristicType selectedHeuristic = HeuristicType.TerrainAware;

    private float GetHeuristic(Node a, Node b, HeuristicType type)
    {
        switch (type)
        {
            case HeuristicType.Manhattan: return ManhattanHeuristic(a, b);
            case HeuristicType.Diagonal: return DiagonalHeuristic(a, b);
            case HeuristicType.TerrainAware: return TerrainAwareHeuristic(a, b);
            case HeuristicType.TurningAware: return TurningPenaltyHeuristic(a, b);
            case HeuristicType.RepulsionAware: return RepulsionHeuristic(a, b);
            case HeuristicType.Euclidean:
            default: return Vector2.Distance(a.worldPosition, b.worldPosition); // Euclidean
        }
    }

    private float DiagonalHeuristic(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);
        return Mathf.Max(dx, dy);
    }


    private float TerrainAwareHeuristic(Node a, Node b)
    {
        float minTerrainModifier = 0.4f; // Assuming 0.4 is the slowest terrain speed
        return GetDistance(a, b) * minTerrainModifier;
    }


    private float TurningPenaltyHeuristic(Node a, Node b)
    {
        float baseDist = Vector2.Distance(a.worldPosition, b.worldPosition);
        float turningPenalty = TurningCost(a, b);
        return baseDist + turningPenalty * 0.1f; // Scale the penalty modestly
    }

    private float RepulsionHeuristic(Node node, Node target)
    {
        float baseHeuristic = Vector2.Distance(node.worldPosition, target.worldPosition);
        Vector2 repulsion = GetRepulsion(node.worldPosition, LayerMask.GetMask("Obstacle")); // Adjust mask
        return baseHeuristic + repulsion.magnitude;
    }
    private float ManhattanHeuristic(Node a, Node b)
    {
        int dx = Mathf.Abs(a.gridX - b.gridX);
        int dy = Mathf.Abs(a.gridY - b.gridY);
        return dx + dy;
    }

    private float EuclideanHeuristic(Node a, Node b)
    {
        return Vector2.Distance(a.worldPosition, b.worldPosition);
    }



    private Node[] SimplifyPath(Node[] path)
    {
        var newPath = new List<Node>();
        if (path.Length <= 1)
            return newPath.ToArray();
        Vector2 lastDir = (path[1].worldPosition - path[0].worldPosition).normalized;
        newPath.Add(path[0]);
        for (int i = 1; i < path.Length - 1; i++)
        {
            Vector2 dir = (path[i].worldPosition - path[i - 1].worldPosition).normalized;
            if (dir != lastDir)
            {
                newPath.Add(path[i]);
                lastDir = dir;
            }

        }
        newPath.Add(path[path.Length - 1]);
        return newPath.ToArray();
    }


    int TurningCost(Node from, Node to)
    {
        if (from.parent == null)
            return 0;
        Vector2 dirOld = new Vector2(from.gridX - from.parent.gridX, from.gridY - from.parent.gridY);
        Vector2 dirNew = new Vector2(to.gridX - from.gridX, to.gridY - from.gridY);
        if (dirNew == dirOld)
            return 0;
        else if (dirOld.x != 0 && dirOld.y != 0 && dirNew.x != 0 && dirNew.y != 0)
        {
            return 5;
        }
        else
        {
            return 10;
        }
    }

    float GetDistance(Node nodeA, Node nodeB)
    {
        int dx = Mathf.Abs(nodeA.gridX - nodeB.gridX);
        int dy = Mathf.Abs(nodeA.gridY - nodeB.gridY);
        const float D = 1f;
        const float D2 = 1.41421356f;
        if (dx > dy)
            return D2 * dy + D * (dx - dy);
        else
            return D2 * dx + D * (dy - dx);
    }

}
