using UnityEngine;

[CreateAssetMenu]
public class Gem : ScriptableObject
{
    public enum Type { Beige, Blue, Green, Pink, Yellow }
    public Type Color;
    public Sprite s;
    public Gem(Type color)
    {
        this.Color = color;
    }
}
