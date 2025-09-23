using UnityEngine;
using System.Collections.Generic;

namespace MCTSv2
{


    public class MCTSNode
    {
        private Connect4State state;
        private int visits = 0;
        private int move = -1;
        private float scores = 0;
        private List<MCTSNode> children = null;
        private bool isTerminal = false;
        MCTSNode parent = null;
        MCTSNode globalNode = null;

        public MCTSNode(Connect4State _state, int _move = -1, MCTSNode _parent = null)
        {
            state = _state;
            move = _move;
            parent = _parent;
            visits = 0;
            scores = 0;
            isTerminal = _state.GetResult() != Connect4State.Result.Undecided;
        }

        public void SetGlobalNode(MCTSNode _globalNode)
        {
            globalNode = _globalNode;
        }
        public Connect4State GetState()
        {
            return state;
        }
        public bool IsLeaf()
        {
            return children == null;
        }
        public bool IsTerminal()
        {
            return isTerminal;
        }

        public float GetResult()
        {
            return Connect4State.ResultToFloat(state.GetResult());
        }

        // Backpropagation of the score, flipping the score if it's not the player's turn
        public void PropagateScore(float score, bool isPlayerTurn)
        {
            if (!isPlayerTurn)
            {
                score = 1 - score;
            }
            scores += score;
            visits++;
        }
        public MCTSNode GetGlobalNode()
        {
            return globalNode;
        }
        public float GetUCBScore(float c)
        {
            if (visits == 0)
            {
                return float.MaxValue;
            }
            if (parent == null)
            {
                return 0;
            }
            float avgScore = GetAverageScore();
            return avgScore + c * Mathf.Sqrt(Mathf.Log(parent.visits) / visits);
        }
        public float GetAverageScore()
        {
            return globalNode.GetScores() / globalNode.GetVisits();
        }

        public float GetScores()
        {
            return scores;
        }

        public void Expand()
        {
            if (children == null && !IsTerminal())
            {
                children = new List<MCTSNode>();
                List<int> moves = state.GetPossibleMoves();
                foreach (int move in moves)
                {
                    Connect4State childState = state.Clone();
                    childState.MakeMove(move);
                    MCTSNode childNode = new MCTSNode(childState, move, this);
                    children.Add(childNode);
                }
            }
        }

        public List<MCTSNode> GetChildren()
        {
            // Lazy initialization of children
            if (children != null)
            {
                return children;
            }
            else
            {
                Expand();
                return children;
            }
        }

        public int GetVisits()
        {
            return visits;
        }

        // Get best child based on the number of visits
        public MCTSNode GetBestChild()
        {

            children = GetChildren();
            if (children == null || children.Count == 0)
            {
                return null;
            }
            MCTSNode bestChild = children[0];
            int mostVisits = bestChild.GetVisits();
            foreach (MCTSNode child in children)
            {
                int visits = child.GetVisits();
                if (visits > mostVisits)
                {
                    bestChild = child;
                    mostVisits = visits;
                }
            }
            return bestChild;
        }

        // Get next child based on UCB score
        public MCTSNode GetNextChild(float c)
        {
            children = GetChildren();
            MCTSNode nextChild = children[0];
            float bestScore = nextChild.GetUCBScore(c);
            foreach (MCTSNode child in children)
            {
                float score = child.GetUCBScore(c);
                if (score > bestScore)
                {
                    nextChild = child;
                    bestScore = score;
                }
            }
            return nextChild;
        }

        public int GetMove()
        {
            return move;
        }

    }
}