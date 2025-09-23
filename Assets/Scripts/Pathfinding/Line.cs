using UnityEngine;
public struct Line
{
    float a;

    float b;

    float c;

    bool approachSide;

    Vector2 lineCenter;

    public Line(Vector2 pointOnLine, Vector2 pointPerpendicularToLine)
    {
        a = pointPerpendicularToLine.x - pointOnLine.x;
        b = pointPerpendicularToLine.y - pointOnLine.y;
        c = -a * pointOnLine.x - b * pointOnLine.y;
        lineCenter = pointOnLine;
        approachSide = false;
        approachSide = GetSide(pointPerpendicularToLine);
    }

    public bool GetSide(Vector2 point)
    {
        return (a * point.x + b * point.y + c) > 0;
    }

    public bool HasCrossedLine(Vector2 point)
    {
        return GetSide(point) != approachSide;
    }

    public void Draw(float length)
    {
        Vector2 start = lineCenter + new Vector2(b, -a).normalized * 0.5f;
        Vector2 end = lineCenter - new Vector2(b, -a).normalized * 0.5f;
        Debug.DrawLine(start, end, Color.red);
    }
}