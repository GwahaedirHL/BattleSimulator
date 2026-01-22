using UnityEngine;

public class EntryPoint : MonoBehaviour
{
    [SerializeField]
    UnitsConfig unitsConfig;

    [SerializeField]
    MainMenu menuView;


    void Start() 
    {
        MainMenuController menuController = new MainMenuController(unitsConfig, menuView);
        menuController.Init();
    }
}
