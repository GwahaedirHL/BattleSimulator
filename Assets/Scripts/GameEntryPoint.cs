using UnityEngine;

public class GameEntryPoint : MonoBehaviour
{
    [SerializeField]
    GameUI gameUI;

    [SerializeField]
    Cooldown cooldown;

    void Start()
    {   
        var gameUiController = new GameUiController(gameUI, cooldown);
        gameUiController.Init();
    }
}
