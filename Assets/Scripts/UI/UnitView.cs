using TMPro;

using UnityEngine;
using UnityEngine.UI;

public class UnitView: MonoBehaviour
{
    [SerializeField]
    Image visual;

    [SerializeField]
    TextMeshProUGUI countText;

    public void SetColor(Color color)
    {
        visual.color = color;
    }

    public void SetSize(float scale)
    {
        visual.rectTransform.localScale = new Vector3(scale , scale, scale);
    }

    public void SetSprite(Sprite sprite)
    {
        visual.sprite = sprite;
    }

    public void SetCountText(int count)
    {
        countText.text = count.ToString();
    }
}
