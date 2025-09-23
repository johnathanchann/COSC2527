using UnityEngine;
public struct Waypoint
{
    private Vector2 position;
    public Line line;
    public Waypoint(Vector2 _position, Vector2 _previous)
    {
        position = _position;
        line = new Line(_position, _previous);
    }
    public bool HasCrossedLine(Vector2 point)
    {
        return line.HasCrossedLine(point);
    }

}