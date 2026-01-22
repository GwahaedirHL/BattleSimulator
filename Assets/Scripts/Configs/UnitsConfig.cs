using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "config", menuName = "GameConfig")]
public class UnitsConfig : ScriptableObject
{
    [SerializeField]
    public int ArmySize;

    [SerializeField]
    public int maxDistinctUnits;

    [SerializeField]
    public Sprite[] UnitShapes;    

    [SerializeField]
    public float[] Sizes;

    [SerializeField]
    public Color[] Colors;

    public const int ARMIES_COUNT = 2;
}