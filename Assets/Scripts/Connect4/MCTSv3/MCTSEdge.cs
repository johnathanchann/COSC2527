using UnityEngine;

namespace MCTSv3
{

    public class MCTSEdge
    {
        private int move = -1;
        public float Q;
        public int N;
        private MCTSNode head = null;
        private MCTSNode tail = null;

        public MCTSEdge(MCTSNode tail, MCTSNode head, int _move)
        {
            this.tail = tail;
            this.head = head;
            move = _move;
            N = 0;
            Q = 0;
        }

        public MCTSNode GetHead()
        {
            return head;
        }
        public MCTSNode GetTail()
        {
            return tail;
        }

        public float UCT(float c, bool isPlayerTurn)
        {
            if (N == 0)
            {
                return float.MaxValue;
            }
            if (isPlayerTurn)
            {
                return head.V + c * Mathf.Sqrt(Mathf.Log(tail.N) / N);
            }
            else
            {
                return 1 - head.V + c * Mathf.Sqrt(Mathf.Log(tail.N) / N);
            }
        }

        public float GetQ()
        {
            return head.V;
        }
        public int GetN()
        {
            return N;
        }


        public int GetMove()
        {
            return move;
        }


        public void UpdateQ()
        {
            N++;
        }

    }
}