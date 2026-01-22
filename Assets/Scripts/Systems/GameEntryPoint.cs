using UnityEngine;

public class GameEntryPoint : MonoBehaviour
{
    [SerializeField]
    GameUI gameUI;

    void Start()
    {   
        var gameUiController = new GameUiController(gameUI);
        gameUiController.Init();
    }
}
