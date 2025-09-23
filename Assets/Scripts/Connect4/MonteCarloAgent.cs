using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Purchasing;

public class MonteCarloAgent : Agent
{
    //TODO values can be changed
    public int totalSims = 2500;
    public float timeLimitSeconds = 0.5f;
    public bool useTimeLimit = false;


    float Rollout(Connect4State state, int team)
    {
        Connect4State nextState = state.Clone();
        while (nextState.GetResult() == Connect4State.Result.Undecided)
        {
            List<int> actions = nextState.GetPossibleMoves();
            int randomAction = actions[Random.Range(0, actions.Count)];
            nextState.MakeMove(randomAction);
        }
        float result = Connect4State.ResultToFloat(nextState.GetResult());
        if (team == 0)
        {
            result = 1 - result;
        }
        return result;
    }

    public override int GetMove(Connect4State state)
    {
        MCSNode root = new MCSNode(state);
        List<MCSNode> children = root.GetChildren();


        if (useTimeLimit)
        {
            float endTime = Time.realtimeSinceStartup + timeLimitSeconds;
            int i = 0;
            while (Time.realtimeSinceStartup < endTime)
            {
                MCSNode currentChild = children[i % children.Count];
                float score = Rollout(currentChild.GetState(), state.GetPlayerTurn());
                currentChild.UpdateScore(score);
                i++;
            }
        }
        else
        {
            for (int i = 0; i < totalSims; i++)
            {
                // round robin distribution of the simulations
                MCSNode currentChild = children[i % children.Count];
                float score = Rollout(currentChild.GetState(), state.GetPlayerTurn());
                currentChild.UpdateScore(score);
            }
        }
        float bestScore = float.MinValue;
        int bestMove = -1;
        foreach (MCSNode child in children)
        {
            float averageScore = child.GetAverageScore();
            if (averageScore > bestScore)
            {
                bestScore = averageScore;
                bestMove = child.GetMove();
            }
        }
        return bestMove;
    }
}
