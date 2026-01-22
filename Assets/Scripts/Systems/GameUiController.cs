using Unity.Entities;

public class GameUiController
{
    GameUI gameUI;
    CheckEndBattleSystem checkEndGameSystem;

    public GameUiController(GameUI gameUI)
    {

        this.gameUI = gameUI;
    }

    public void Init()
    {
        checkEndGameSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CheckEndBattleSystem>();
        checkEndGameSystem.Win += Win;
        gameUI.LoadMainMenu += LoadMainMenu;
        gameUI.Init();
    }

    void LoadMainMenu()
    {
        gameUI.LoadMainMenu -= LoadMainMenu;
        SceneLoader.LoadMainMenu();
    }

    void Win(int winner)
    {
        checkEndGameSystem.Win -= Win;
        gameUI.ShowWinner(winner);
    }
}
