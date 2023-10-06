using UnityEngine;

[CreateAssetMenu]
public class Gem : ScriptableObject
{
    public enum Type { Beige, Blue, Green, Pink, Yellow }
    public Type Color;
    public Sprite GemSprite;
    public Gem(Type color)
    {
        Color = color;
    }
}
