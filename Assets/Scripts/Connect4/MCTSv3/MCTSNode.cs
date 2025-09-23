using UnityEngine;
using System.Collections.Generic;

namespace MCTSv3
{


    public class MCTSNode
    {
        private List<MCTSEdge> children = null;
        private bool isTerminal = false;
        Connect4State state = null;

        public int N = 0;
        public float U = 0;

        public float V = 0;
        Dictionary<Connect4State, MCTSNode> transpositionTable = null;

        public MCTSNode(Connect4State _state, Dictionary<Connect4State, MCTSNode> _transpositionTable)
        {
            transpositionTable = _transpositionTable;
            state = _state;
            isTerminal = _state.GetResult() != Connect4State.Result.Undecided;
            N = 0;
            V = 0;
        }

        public void UpdateV()
        {
            N++;
            float sum = U;
            for (int i = 0; i < children.Count; i++)
            {
                sum += children[i].GetQ() * children[i].N;
            }
            V = sum / N;

        }
        public void UpdateV(float value)
        {
            N++;
            U += value;
            V += U;
        }


        private MCTSNode GetNode(Connect4State state)
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

        public void Expand()
        {
            if (children == null && !IsTerminal())
            {
                children = new List<MCTSEdge>();
                List<int> moves = state.GetPossibleMoves();
                foreach (int move in moves)
                {
                    Connect4State childState = state.Clone();
                    childState.MakeMove(move);
                    MCTSNode childNode = GetNode(childState);
                    MCTSEdge childEdge = new MCTSEdge(this, childNode, move);
                    children.Add(childEdge);
                }
            }
        }

        public List<int> GetPossibleMoves()
        {
            List<MCTSEdge> children = GetChildren();
            List<int> moves = new List<int>();
            for (int i = 0; i < children.Count; i++)
            {
                moves.Add(children[i].GetMove());
            }
            return moves;
        }

        public List<MCTSEdge> GetChildren()
        {
            return children;
        }


        public MCTSEdge GetNextChild(float c, bool isPlayerTurn)
        {
            children = GetChildren();
            MCTSEdge nextChild = children[0];
            float bestScore = nextChild.UCT(c, isPlayerTurn);
            foreach (MCTSEdge child in children)
            {
                float score = child.UCT(c, isPlayerTurn);
                if (score > bestScore)
                {
                    nextChild = child;
                    bestScore = score;
                }
            }
            return nextChild;
        }

        public int GetBestMove()
        {
            List<MCTSEdge> children = GetChildren();
            int bestMove = children[0].GetMove();
            int mostVisits = children[0].GetN();
            for (int i = 1; i < children.Count; i++)
            {
                int visits = children[i].GetN();
                if (visits > mostVisits)
                {
                    bestMove = children[i].GetMove();
                    mostVisits = visits;
                }
            }
            return bestMove;
        }


    }
}