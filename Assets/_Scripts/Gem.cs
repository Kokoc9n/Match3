using UnityEngine;

[CreateAssetMenu]
public class Gem : ScriptableObject
{
    public enum Color { Beige, Blue, Green, Pink, Yellow }
    public Color c;
    public Sprite s;
    public Gem(Color color)
    {
        this.c = color;
    }
}
