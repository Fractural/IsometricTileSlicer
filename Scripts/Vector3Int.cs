using Godot;
using System;

public struct Vector3Int
{
    public int x;
    public int y;
    public int z;

    public static Vector3Int Zero => new Vector3Int(0, 0, 0);
    public static Vector3Int One => new Vector3Int(1, 1, 1);

    public Vector3Int(int x, int y, int z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }

    public override bool Equals(object obj)
    {
        if (obj is Vector3Int)
        {
            var asVector3Int = (Vector3Int)obj;
            return asVector3Int == this;
        }
        return false;
    }

    public override int GetHashCode()
    {
        int hash = 7;
        hash = 71 * hash + this.x;
        hash = 71 * hash + this.y;
        hash = 71 * hash + this.z;
        return hash;
    }
    public override string ToString()
    {
        return $"({x}, {y}, {z})";
    }

    public int BlockDistanceTo(Vector3Int other)
    {
        return Mathf.Abs(other.x - x) + Mathf.Abs(other.y - y) + Mathf.Abs(other.z - z);
    }

    public float DistanceTo(Vector3Int other)
    {
        return Mathf.Sqrt(DistanceSquaredTo(other));
    }

    public int DistanceSquaredTo(Vector3Int other)
    {
        return other.x * x + other.y * y + other.z * z;
    }

    public static implicit operator Vector3(Vector3Int vInt) => new Vector3(vInt.x, vInt.y, vInt.z);
    public static explicit operator Vector3Int(Vector3 v) => new Vector3Int((int)v.x, (int)v.y, (int)v.z);
    public static Vector3Int operator -(Vector3Int one) => new Vector3Int(-one.x, -one.y, -one.z);
    public static Vector3Int operator *(Vector3Int one, float other) => new Vector3Int((int)(one.x * other), (int)(one.y * other), (int)(one.z * other));
    public static Vector3Int operator /(Vector3Int one, float other) => one * (1 / other);
    public static Vector3Int operator *(Vector3Int one, int other) => new Vector3Int(one.x * other, one.y * other, one.z * other);
    public static Vector3Int operator /(Vector3Int one, int other) => new Vector3Int(one.x / other, one.y / other, one.z / other);
    public static Vector3Int operator +(Vector3Int one, Vector3Int two) => new Vector3Int(one.x + two.x, one.y + two.y, one.z + two.z);
    public static Vector3Int operator -(Vector3Int one, Vector3Int two) => one + (-two);
    public static bool operator ==(Vector3Int one, Vector3Int two) => one.x == two.x && one.y == two.y && one.z == two.z;
    public static bool operator !=(Vector3Int one, Vector3Int two) => !(one == two);
}
