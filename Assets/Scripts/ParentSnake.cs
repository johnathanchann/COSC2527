using UnityEngine;

public abstract class ParentSnake : MonoBehaviour
{
    public abstract Vector2 getPos();
    public abstract bool attackingState();
}