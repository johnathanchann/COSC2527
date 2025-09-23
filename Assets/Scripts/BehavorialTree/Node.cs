using System.Collections.Generic;
namespace BehavorialTree
{

    public class Selector : Node
    {
        public Selector(string name) : base(name) { }

        public override Status Process()
        {
            foreach (var child in children)
            {
                var stat = child.Process();
                if (stat == Status.Success)
                {
                    return Status.Success;
                }
            }
            return Status.Failure;
        }
    }

    public class Sequence : Node
    {
        public Sequence(string name) : base(name) { }

        public override Status Process()
        {
            foreach (var child in children)
            {
                var stat = child.Process();

                if (stat == Status.Failure)
                    return Status.Failure;

            }

            return Status.Success;
        }
    }


    public class BehaviourTree : Node
    {
        public BehaviourTree(string name) : base(name)
        {

        }

        public override Status Process()
        {
            while (currentChild < children.Count)
            {
                var status = children[currentChild].Process();
                if (status != Status.Success)
                {
                    return status;
                }
                currentChild++;
            }
            return Status.Success;
        }
    }
    public class Leaf : Node
    {
        IStrategy strategy;
        public Leaf(string name, IStrategy strategy) : base(name)
        {
            this.strategy = strategy;
        }

        public override Status Process() => strategy.Process();

        public override void Reset() => strategy.Reset();
    }
    public class Node
    {
        public enum Status { Success, Failure };

        public string name;

        public List<Node> children = new();

        protected int currentChild;


        public Node(string name = "Node")
        {
            this.name = name;
        }

        public void AddChild(Node child) => children.Add(child);

        public virtual Status Process() => children[currentChild].Process();

        public virtual void Reset()
        {
            currentChild = 0;
            foreach (var child in children)
            {
                child.Reset();
            }
        }
    }
}