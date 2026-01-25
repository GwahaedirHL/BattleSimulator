using Unity.Entities;

public class GameUiController
{
    GameUI gameUI;
    CheckEndBattleSystem checkEndGameSystem;
    Cooldown cooldown;

    public GameUiController(GameUI gameUI, Cooldown cooldown)
    {
        this.gameUI = gameUI;
        this.cooldown = cooldown;
    }

    public void Init()
    {
        checkEndGameSystem = World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<CheckEndBattleSystem>();
        checkEndGameSystem.Win += Win;
        gameUI.LoadMainMenu += LoadMainMenu;
        gameUI.SpawnMeteor += SpawnMeteor;
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

    void SpawnMeteor()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        var em = world.EntityManager;

        if (world == null || !world.IsCreated)
            return;

        var e = em.CreateEntity();
        em.AddComponentData(e, new MeteorSpawnRequest());

        cooldown.ResetTimer();
        cooldown.UpdateTime += gameUI.SetMeteorCooldownVisual;
        cooldown.End += gameUI.ResetMeteor;
        cooldown.StartTimer(10f);
    }
}
