using UnityEngine;
using System.Collections.Generic;


namespace MCTSv3
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
                int randomAction = actions[UnityEngine.Random.Range(0, actions.Count)];
                nextState.MakeMove(randomAction);
            }

            float result = Connect4State.ResultToFloat(nextState.GetResult());
            if (team == 0)
            {
                result = 1 - result;
            }
            return result;
        }



        public MCTSNode GetTransposition(Connect4State state)
        {
            if (transpositionTable.TryGetValue(state, out MCTSNode node))
            {
                return node;
            }
            else
            {
                MCTSNode newNode = new MCTSNode(state, transpositionTable);
                transpositionTable[state] = newNode;
                return newNode;
            }
        }

        private readonly List<MCTSEdge> trajectory = new List<MCTSEdge>();
        private void MCTS(MCTSNode root, int team)
        {
            trajectory.Clear();
            MCTSNode node = root;
            bool isPlayerTurn = true;
            while (!node.IsLeaf())
            {
                MCTSEdge edge = node.GetNextChild(c, isPlayerTurn);
                trajectory.Add(edge);
                node = edge.GetHead();
                isPlayerTurn = !isPlayerTurn;
            }
            node.Expand();
            float value = Rollout(node.GetState(), team);
            if (!node.IsTerminal())
            {
                node.UpdateV(value);
            }
            else
            {
                node.V = value;
            }
            Backpropagate(trajectory);
        }

        private void Backpropagate(List<MCTSEdge> trajectory)
        {
            for (int i = trajectory.Count - 1; i >= 0; i--)
            {
                MCTSEdge edge = trajectory[i];
                MCTSNode node = edge.GetTail();
                edge.N++;
                node.UpdateV();
            }
        }
        public override int GetMove(Connect4State state)
        {
            Connect4State currentState = state.Clone();
            MCTSNode root = GetTransposition(currentState);
            root.Expand();
            if (useTimeLimit)
            {
                float endTime = Time.realtimeSinceStartup + timeLimitSeconds;
                while (Time.realtimeSinceStartup < endTime)
                {
                    MCTS(root, state.GetPlayerTurn());
                }

                Debug.Log("MCTSv5 Total visits: " + root.N);
                Debug.Log("MCTSv5 Visits per second: " + (int)(root.N / timeLimitSeconds));
                return root.GetBestMove();
            }
            else
            {
                for (int i = 0; i < totalSims; i++)
                {
                    MCTS(root, state.GetPlayerTurn());
                }
                return root.GetBestMove();
            }
        }
    }

}