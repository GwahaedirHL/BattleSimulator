using UnityEngine;

public class UnitData
{
    public float size;
    public UnitShape shape;
    public Color color;
    public int count;
    public Sprite Sprite;
}

public enum UnitShape 
{
    Sphere = 0,
    Cube = 1
}