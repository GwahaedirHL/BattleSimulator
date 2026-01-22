using System.Collections.Generic;

using UnityEngine;

public static class ArmiesConfig
{
    public static List<UnitData>[] armies = new List<UnitData>[UnitsConfig.ARMIES_COUNT];

    static ArmiesConfig() {
        for (int i = 0; i < armies.Length; i++)
            armies[i] = new List<UnitData>();
    }
}
