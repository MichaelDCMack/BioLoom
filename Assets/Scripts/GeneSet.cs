using UnityEngine;

public class GeneSet : MonoBehaviour
{
    #region public members
    public Sprite[] sprites;

    public Sprite emptySprite;
    public Sprite collisionSprite;

    public int height;
    public int width;

    public bool useStartGene;
    public int startGene;
    #endregion

    #region private members
    private int[] tileSpriteMapping;
    #endregion

    #region properties
    public int Length => sprites.Length;

    public Sprite Empty => emptySprite;
    public Sprite Collision => collisionSprite;

    public int MinGene
    {
        get
        {
            if (useStartGene)
            {
                return startGene;
            }

            return -Length / 2;
        }
    }
    
    public int MaxGene => MinGene + Length - 1;
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
    
    public int GetRandomGene()
    {
        return Random.Range(MinGene, MaxGene);
    }

    public Sprite GetSpriteFromGene(int gene)
    {
        return sprites[tileSpriteMapping[gene - MinGene]];
    }
}