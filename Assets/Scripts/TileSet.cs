using UnityEngine;

public class TileSet : MonoBehaviour
{

    #region public members
    public Sprite[] sprites;

    public Sprite emptySprite;
    public Sprite collisionSprite;

    public int height;
    public int width;

    #endregion

    #region private members
    private int[] tileSpriteMapping;
    #endregion

    #region properties
    public int Length => sprites.Length;

    public Sprite this[int index] => sprites[tileSpriteMapping[index]];

    public Sprite Empty => emptySprite;
    public Sprite Collision => collisionSprite;
    #endregion

    public void Start()
    {
        ResetMapping();
    }

    public void ResetMapping()
    {
        tileSpriteMapping = new int[sprites.Length];
        for(int i = 0; i < sprites.Length; ++i)
        {
            tileSpriteMapping[i] = i;
        }
    }

    public void ShuffleMapping()
    {
        Extensions.Shuffle(tileSpriteMapping);
    }
}