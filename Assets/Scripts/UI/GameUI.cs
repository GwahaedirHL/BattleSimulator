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
    Button spawnMeteorButton;

    [SerializeField]
    Image meteorCooldown;

    [SerializeField]
    TextMeshProUGUI winMessageText;

    public event UnityAction LoadMainMenu;
    public event UnityAction SpawnMeteor;

    public void Init()
    {
        meteorCooldown.fillAmount = 0f;
        backToMenuButtonRoot.SetActive(false);
        spawnMeteorButton.gameObject.SetActive(true);
        backToMenuButton.onClick.RemoveAllListeners();
        spawnMeteorButton.onClick.RemoveAllListeners();
        
        backToMenuButton.onClick.AddListener(() =>LoadMainMenu?.Invoke());
        spawnMeteorButton.onClick.AddListener(RequestMeteor);
    }

    public void ShowWinner(int winner)
    {
        spawnMeteorButton.gameObject.SetActive(false);
        backToMenuButtonRoot.SetActive(true);
        winMessageText.text = $"{winner} wins";
    }

    public void RequestMeteor()
    {
        spawnMeteorButton.enabled = false;
        SpawnMeteor?.Invoke();
    }

    public void ResetMeteor()
    {
        spawnMeteorButton.enabled = true;
    }

    public void SetMeteorCooldownVisual(float leftNormalized)
    {
        meteorCooldown.fillAmount = leftNormalized;
    }
}
