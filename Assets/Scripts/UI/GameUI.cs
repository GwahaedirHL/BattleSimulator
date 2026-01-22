using TMPro;

using Unity.Entities;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class GameUI : MonoBehaviour
{
    [SerializeField] 
    GameObject backToMenuButtonRoot;

    [SerializeField] 
    Button backToMenuButton;

    [SerializeField]
    TextMeshProUGUI winMessageText;

    public event UnityAction LoadMainMenu;

    public void Init()
    {
        backToMenuButtonRoot.SetActive(false);
        backToMenuButton.onClick.RemoveAllListeners();
        backToMenuButton.onClick.AddListener(LoadMainMenu.Invoke);
    }

    public void ShowWinner(int winner)
    {
        backToMenuButtonRoot.SetActive(true);
        winMessageText.text = $"{winner} wins";
    }
}
