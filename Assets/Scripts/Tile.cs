using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public Block block;
    [SerializeField] SpriteRenderer rend;
    [SerializeField] Text text;
    public void Init(BlockType type, Block _block)
    {
        rend.color = type.Color;
        text.text = type.Value.ToString();
        text.fontSize = type.FontSize;
        text.color = type.FontColor;
        block = _block;
    }
}
