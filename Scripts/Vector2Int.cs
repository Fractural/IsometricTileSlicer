using Godot;
using System;
using System.Text.Json.Serialization;

public struct Vector2Int
{
    public int x;
    public int y;

    public static Vector2Int Zero => new Vector2Int(0, 0);
    public static Vector2Int One => new Vector2Int(1, 1);

    public Vector2Int(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public override bool Equals(object obj)
    {
        if (obj is Vector2Int)
        {
            var asVector2Int = (Vector2Int)obj;
            return asVector2Int == this;
        }
        return false;
    }

    public override int GetHashCode()
    {
        int hash = 7;
        hash = 71 * hash + this.x;
        hash = 71 * hash + this.y;
        return hash;
    }
    public override string ToString()
    {
        return $"({x}, {y})";
    }

    public int BlockDistanceTo(Vector2Int other)
    {
        return Mathf.Abs(other.x - x) + Mathf.Abs(other.y - y);
    }

    public float DistanceTo(Vector2Int other)
    {
        return Mathf.Sqrt(DistanceSquaredTo(other));
    }

    public int DistanceSquaredTo(Vector2Int other)
    {
        return other.x * x + other.y * y;
    }

    public static implicit operator Vector2(Vector2Int vInt) => new Vector2(vInt.x, vInt.y);
    public static explicit operator Vector2Int(Vector2 v) => new Vector2Int((int)v.x, (int)v.y);
    public static Vector2Int operator -(Vector2Int one) => new Vector2Int(-one.x, -one.y);
    public static Vector2Int operator *(Vector2Int one, float other) => new Vector2Int((int)(one.x * other), (int)(one.y * other));
    public static Vector2Int operator /(Vector2Int one, float other) => one * (1 / other);
    public static Vector2Int operator *(Vector2Int one, int other) => new Vector2Int(one.x * other, one.y * other);
    public static Vector2Int operator /(Vector2Int one, int other) => new Vector2Int(one.x / other, one.y / other);
    public static Vector2Int operator +(Vector2Int one, Vector2Int two) => new Vector2Int(one.x + two.x, one.y + two.y);
    public static Vector2Int operator -(Vector2Int one, Vector2Int two) => one + (-two);
    public static bool operator ==(Vector2Int one, Vector2Int two) => one.x == two.x && one.y == two.y;
    public static bool operator !=(Vector2Int one, Vector2Int two) => !(one == two);
}
