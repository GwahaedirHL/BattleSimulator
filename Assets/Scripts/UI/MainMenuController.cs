using System;
using System.Collections.Generic;

public class MainMenuController
{
    UnitsConfig unitsConfig;
    MainMenu menuView;
    System.Random random = new System.Random();

    public MainMenuController(UnitsConfig unitsConfig, MainMenu menuView)
    {
        this.unitsConfig = unitsConfig;
        this.menuView = menuView;
    }

    public void Init()
    {
        menuView.LoadBattleScene += LoadBattle;
        menuView.GenerateArmy += CreateUnitsData;

        CreateUnitsData(0);
        CreateUnitsData(1);

        menuView.Init();
    }

    public void LoadBattle()
    {
        SceneLoader.LoadBattle();
    }

    void CreateUnitsData(int armyIndex)
    {
        int shapes = unitsConfig.UnitShapes.Length;
        int colors = unitsConfig.Colors.Length;
        int sizes = unitsConfig.Sizes.Length;

        int total = shapes * colors * sizes;

        var units = new List<UnitData>(total);

        for (int i = 0; i < total; i++)
        {
            int shapeIndex = i / (colors * sizes);
            int rem = i % (colors * sizes);

            int colorIndex = rem / sizes;
            int sizeIndex = rem % sizes;

            var unit = new UnitData
            {
                shape = (UnitShape)shapeIndex,
                color = unitsConfig.Colors[colorIndex],
                size = unitsConfig.Sizes[sizeIndex],
                Sprite = unitsConfig.UnitShapes[shapeIndex]
            };

            units.Add(unit);
        }

        BuildArmyCountsWithDiversity(units, unitsConfig.ArmySize, unitsConfig.maxDistinctUnits);

        menuView.CreateViews(units, armyIndex);

        ArmiesConfig.armies[armyIndex] = units;
    }

    void BuildArmyCountsWithDiversity(List<UnitData> allTypes, int armySize, int maxDistinctTypes)
    {
        for (int i = 0; i < allTypes.Count; i++)
            allTypes[i].count = 0;

        int distinct = Math.Min(maxDistinctTypes, Math.Min(armySize, allTypes.Count));

        var indices = new int[allTypes.Count];
        for (int i = 0; i < indices.Length; i++)
            indices[i] = i;

        for (int i = 0; i < distinct; i++)
        {
            int j = random.Next(i, indices.Length);
            (indices[i], indices[j]) = (indices[j], indices[i]);
            allTypes[indices[i]].count = 1;
        }

        int remaining = armySize - distinct;

        for (int i = 0; i < remaining; i++)
        {
            int idx = random.Next(0, allTypes.Count);
            allTypes[idx].count++;
        }
    }
}
