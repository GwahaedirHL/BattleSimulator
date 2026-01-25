using UnityEngine.SceneManagement;

public static class SceneLoader
{
    const string GameSceneID = "GameScene";
    const string MainMenuID = "MainMenuScene";

    public static void LoadBattle()
    {
        SceneManager.LoadScene(GameSceneID);
    }

    public static void LoadMainMenu()
    {
        SceneManager.LoadScene(MainMenuID);
    }
}
