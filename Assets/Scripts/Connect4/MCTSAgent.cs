using UnityEngine;
using System.Collections.Generic;

public class MCTSAgent : Agent
{
    public int totalSims = 2500;
    public float timeLimitSeconds = 0.5f;
    public float c = Mathf.Sqrt(2.0f);
    public bool useTimeLimit = false;


    private float Rollout(Connect4State state, int team)
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

    private float Simulate(MCTSNode node, bool isPlayerTurn, int team)
    {
        if (node.IsLeaf())
        {
            float score = Rollout(node.GetState(), team);
            node.PropagateScore(score, isPlayerTurn);
            node.Expand();
            return score;
        }
        else
        {
            MCTSNode nextNode = node.GetNextChild(c);
            float score = Simulate(nextNode, !isPlayerTurn, team);
            node.PropagateScore(score, isPlayerTurn);
            return score;
        }
    }

    public override int GetMove(Connect4State state)
    {
        MCTSNode root = new MCTSNode(state.Clone());
        root.Expand();

        int sims = 0;
        if (useTimeLimit)
        {
            float endTime = Time.realtimeSinceStartup + timeLimitSeconds;
            while (Time.realtimeSinceStartup < endTime)
            {
                Simulate(root, false, state.GetPlayerTurn());
                sims++;
            }
            Debug.Log("MCTS Total visits: " + root.GetVisits());
            Debug.Log("MCTS Visits per second: " + (int)(root.GetVisits() / timeLimitSeconds));
        }
        else
        {
            while (sims < totalSims)
            {
                Simulate(root, false, state.GetPlayerTurn());
                sims++;
            }
        }

        MCTSNode bestChild = root.GetBestChild();
        return bestChild.GetMove();
    }
}

