using UnityEngine;
using SteeringCalcs;

namespace BehavorialTree
{
    public interface IStrategy
    {
        Node.Status Process();
        void Reset();

    }
    public class IsNotDead : IStrategy
    {
        private Frog frog;
        public IsNotDead(Frog frog)
        {
            this.frog = frog;
        }

        public Node.Status Process()
        {
            if (Frog.Health > 0)
            {
                return Node.Status.Success;
            }
            frog.MaxSpeed = 0;
            frog._frogSr.color = new Color(1.0f, 0.2f, 0.2f);
            frog.behaviourVel = Vector2.zero;
            return Node.Status.Failure;

        }

        public void Reset() { }


    }
    public class IsNotHuman : IStrategy
    {
        private Frog frog;
        public IsNotHuman(Frog frog)
        {
            this.frog = frog;
        }

        public Node.Status Process()
        {
            if (Frog.human)
            {
                if (frog._lastClickPos != null)
                {
                    frog.behaviourVel = frog.getVelocityTowardsFlagAStar();
                }
                return Node.Status.Failure;
            }
            return Node.Status.Success;

        }

        public void Reset() { }
    }


    public class OutOfCameraBounds : IStrategy
    {
        private Frog frog;
        public OutOfCameraBounds(Frog frog)
        {
            this.frog = frog;
        }
        public Node.Status Process()
        {
            if (frog.isOutOfScreen(frog.transform))
            {
                Vector2 desiredAnch = frog.anchorWeight * Steering.GetAnchor(frog.transform.position, frog.AnchorDims);
                frog.behaviourVel = desiredAnch.normalized * frog.GetCurrentSpeed();
                return Node.Status.Success;

            }
            return Node.Status.Failure;
        }

        public void Reset() { }
    }

    public class SnakeIsClose : IStrategy
    {
        private Frog frog;
        public SnakeIsClose(Frog frog)
        {
            this.frog = frog;
        }

        public Node.Status Process()
        {
            // if (frog.closestFireball != null && frog.distanceToClosestFireball <= frog.scaredRange)
            // {
            //     frog.behaviourVel = Steering.FleeDirect(frog.gameObject.transform.position, frog.closestFireball.transform.position, frog.MaxSpeed);
            //     return Node.Status.Success;
            // }
            if (frog.closestSnake != null && frog.closestSnake.attackingState())
            {
                frog.behaviourVel = Steering.FleeDirect(frog.gameObject.transform.position, frog.closestSnake.transform.position, frog.GetCurrentSpeed());
                return Node.Status.Success;
            }
            return Node.Status.Failure;
        }

        public void Reset() { }

    }
    public class FireBallClose : IStrategy
    {
        private Frog frog;
        public FireBallClose(Frog frog)
        {
            this.frog = frog;
        }

        public Node.Status Process()
        {
            if (frog.closestFireball != null && frog.distanceToClosestFireball <= frog.scaredRange)
            {
                frog.behaviourVel = Steering.FleeDirect(frog.gameObject.transform.position, frog.closestFireball.transform.position, frog.GetCurrentSpeed());
                return Node.Status.Success;
            }

            return Node.Status.Failure;
        }

        public void Reset() { }

    }

    public class ScaredRangeWithin : IStrategy
    {
        private Frog frog;

        public ScaredRangeWithin(Frog frog)
        {
            this.frog = frog;
        }

        public Node.Status Process()
        {
            if (frog.distanceToClosestSnake <= frog.scaredRange)
            {
                Vector2 frogview = ((Vector2)frog.transform.up).normalized;
                Vector2 snakedir = ((Vector2)frog.closestSnake.transform.position - (Vector2)frog.transform.position).normalized;
                float facing = Vector2.Dot(frogview, snakedir);
                if (facing > 0.6f)
                {
                    frog.ShootBubble();
                    return Node.Status.Success;
                }


            }
            return Node.Status.Failure;
        }
        public void Reset() { }

    }

    public class SwarmedBySnake : IStrategy
    {
        private Frog frog;
        public SwarmedBySnake(Frog frog)
        {
            this.frog = frog;
        }

        public Node.Status Process()
        {
            int SnakeSwarm = frog.findSnakeAttackingSwarm(frog.scaredRange);
            if (SnakeSwarm > 2)
            {
                Debug.Log("Shooting");
                frog.spinShoot();
                return Node.Status.Success;
            }
            return Node.Status.Failure;
        }
        public void Reset() { }

    }

    public class ClosestFlyWithinCamera : IStrategy
    {
        private Frog frog;
        public ClosestFlyWithinCamera(Frog frog)
        {
            this.frog = frog;
        }

        public Node.Status Process()
        {
            if (!frog.isOutOfScreen(frog.closestFly.transform) && frog.distanceToClosestFly <= frog.huntRange)
            {
                frog.behaviourVel = Steering.SeekDirect(frog.gameObject.transform.position, frog.closestFly.transform.position, frog.GetCurrentSpeed());
                return Node.Status.Success;
            }
            return Node.Status.Failure;
        }
        public void Reset() { }

    }

    public class NearestSwarm : IStrategy
    {
        private Frog frog;

        public NearestSwarm(Frog frog)
        {
            this.frog = frog;
        }

        public Node.Status Process()
        {
            Vector2 closestSwarm = frog.findClosestSwarm();
            if (closestSwarm != Vector2.zero)
            {
                frog.behaviourVel = Steering.SeekDirect(frog.gameObject.transform.position, closestSwarm, frog.GetCurrentSpeed());
                return Node.Status.Success;
            }
            return Node.Status.Failure;
        }
        public void Reset() { }

    }

    public class DefaultMovement : IStrategy
    {
        private Frog frog;

        public DefaultMovement(Frog frog)
        {
            this.frog = frog;
        }

        public Node.Status Process()
        {
            Vector2 rndDir = UnityEngine.Random.insideUnitCircle.normalized;
            frog.behaviourVel = rndDir * frog.GetCurrentSpeed();
            return Node.Status.Success;
        }

        public void Reset() { }
    }

    public class HealthModeChange : IStrategy
    {
        private Frog frog;
        public HealthModeChange(Frog frog)
        {
            this.frog = frog;
            frog.PrevHealth = Frog.Health;
        }

        public Node.Status Process()
        {
            if (frog.PrevHealth != Frog.Health)
            {
                if (Frog.Health <= 2)
                {
                    frog.scaredRange += 5.0f;
                    if (frog.huntRange >= 6.0f)
                    {
                        frog.huntRange -= 5.0f;
                    }
                }
                else if (Frog.Health > 2)
                {
                    if (frog.scaredRange >= 6.0f)
                    {
                        frog.scaredRange -= 5.0f;
                    }
                    frog.huntRange += 5.0f;
                }
            }
            // frog.PrevHealth=frog.Health;
            return Node.Status.Success;
        }
        public void Reset() { }
    }
}
