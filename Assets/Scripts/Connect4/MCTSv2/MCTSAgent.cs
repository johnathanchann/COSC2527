using UnityEngine;
using System.Collections.Generic;

namespace MCTSv2
{

    public class MCTSAgent : Agent
    {
        public int totalSims = 2500;
        public float timeLimitSeconds = 0.5f;
        public float c = Mathf.Sqrt(2.0f);
        public bool useTimeLimit = false;
        Dictionary<Connect4State, MCTSNode> transpositionTable = new Dictionary<Connect4State, MCTSNode>();

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

        public MCTSNode GetGlobalNode(MCTSNode node)
        {
            if (transpositionTable.TryGetValue(node.GetState(), out MCTSNode globalNode))
            {
                return globalNode;
            }
            else
            {
                MCTSNode newNode = new MCTSNode(node.GetState());
                transpositionTable[node.GetState()] = newNode;
                return newNode;
            }
        }
        private float Simulate(MCTSNode node, bool isPlayerTurn, int team)
        {
            if (node.GetGlobalNode() == null)
            {
                node.SetGlobalNode(GetGlobalNode(node));
            }
            MCTSNode globalNode = node.GetGlobalNode();

            if (globalNode.IsLeaf())
            {
                float score = Rollout(node.GetState(), team);
                node.PropagateScore(score, isPlayerTurn);
                globalNode.PropagateScore(score, isPlayerTurn);
                globalNode.Expand();
                return score;
            }
            else
            {
                MCTSNode nextNode = globalNode.GetNextChild(c);
                float score = Simulate(nextNode, !isPlayerTurn, team);
                node.PropagateScore(score, isPlayerTurn);
                globalNode.PropagateScore(score, isPlayerTurn);
                return score;
            }
        }

        public override int GetMove(Connect4State state)
        {
            MCTSNode root = GetGlobalNode(new MCTSNode(state.Clone()));
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
                Debug.Log("MCTSv4 Total visits: " + root.GetVisits());
                Debug.Log("MCTSv4 Visits per second: " + (int)(root.GetVisits() / timeLimitSeconds));
            }
            else
            {
                for (int i = 0; i < totalSims; i++)
                {
                    Simulate(root, false, state.GetPlayerTurn());
                }
            }

            MCTSNode bestChild = root.GetBestChild();
            return bestChild.GetMove();
        }
    }

}