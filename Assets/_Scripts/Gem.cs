using UnityEngine;

[CreateAssetMenu]
public class Gem : ScriptableObject
{
    public enum Color { Beige, Blue, Green, Pink, Yellow }
    public Color color;
    public Sprite sprite;
    public Gem(Color color)
    {
        this.color = color;
    }
}
