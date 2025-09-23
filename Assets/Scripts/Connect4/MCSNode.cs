using UnityEngine;
using System.Collections.Generic;

public class MCSNode
{
    private Connect4State state;
    private int visits;
    private float scores;
    private int move = -1;
    public MCSNode(Connect4State _state, int _move = -1)
    {
        state = _state;
        move = _move;
        visits = 0;
        scores = 0;
    }

    public bool IsTerminal()
    {
        // Check if the node is a leaf node (i.e., it has no children).
        return state.GetResult() != Connect4State.Result.Undecided;
    }
    public float GetResult()
    {
        return Connect4State.ResultToFloat(state.GetResult());
    }

    public void UpdateScore(float score)
    {
        scores += score;
        visits++;
    }
    public Connect4State GetState()
    {
        return state;
    }

    public float GetAverageScore()
    {
        if (visits == 0)
        {
            return 0;
        }
        return scores / visits;
    }

    public List<MCSNode> GetChildren()
    {
        List<MCSNode> children = new List<MCSNode>();
        List<int> moves = state.GetPossibleMoves();
        foreach (int move in moves)
        {
            Connect4State childState = state.Clone();
            childState.MakeMove(move);
            MCSNode childNode = new MCSNode(childState, move);
            children.Add(childNode);
        }
        return children;
    }
    public int GetMove()
    {
        return move;
    }

}
