using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [SerializeField]
    Transform[] layoutTransforms;

    [SerializeField]
    UnitView unitViewTemplate;

    [SerializeField]
    Button generate1Button;

    [SerializeField]
    Button generate2Button;

    [SerializeField]
    Button startButton;

    public event UnityAction<int> GenerateArmy;
    public event UnityAction LoadBattleScene;

    public void Init()
    {
        generate1Button.onClick.AddListener(() => GenerateArmy.Invoke(0));
        generate2Button.onClick.AddListener(() => GenerateArmy.Invoke(1));
        startButton.onClick.AddListener(() => LoadBattleScene.Invoke());
    }

    public void CreateViews(List<UnitData> units, int armyIndex)
    {
        Transform parent = layoutTransforms[armyIndex];
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);

        foreach (var unit in units)
        {
            var view = Instantiate(unitViewTemplate, parent);
            view.SetSprite(unit.Sprite);
            view.SetColor(unit.color);
            view.SetSize(unit.size);
            view.SetCountText(unit.count);
        }
    }
}


