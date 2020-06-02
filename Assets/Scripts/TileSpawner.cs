using UnityEngine;
using UnityEngine.UI;

public class TileSpawner : MonoBehaviour
{
    public GameObject chunkPrefab;
    public GameObject seedInputField;
    public GameObject seedInputFieldText;
    public GameObject currentSeedText;
    public Sprite[] tileLibrary;
    public Sprite[] altTileLibrary;

    public bool useAltTileLibrary;

    public void SetUseAltTileLibrary(bool b)
    {
        useAltTileLibrary = b;

        Debug.Assert(tileLibrary != null && tileLibrary.Length > 0);
        Debug.Assert(altTileLibrary != null && altTileLibrary.Length > 0);

        defaultSprite = useAltTileLibrary ? altTileLibrary[tileLibrary.Length - 1] : tileLibrary[altTileLibrary.Length - 1];
    }

    public Sprite defaultSprite;

    public int width = 2;
    public int height = 2;

    public int tileWidth = 8;
    public int tileHeight = 8;

    public int currentGenerationIndex = 0;
    public int nextGenerationIndex = 1;

    public int maxReproductions = 2;

    public int PNGSize = 512;

    public bool shuffleTileMapping = false;
    public bool useSeed = false;
    public int seed = 0;
    public enum MutationType
    {
        None,
        Average,
        Random,
        Barricelli,
    }
    public MutationType mutationType;

    public void SetMutationType(int t)
    {
        Debug.Assert((MutationType)t >= MutationType.None && (MutationType)t <= MutationType.Barricelli);
        mutationType = (MutationType)t;
    }

    Texture2D[,] textures;
    int[,] genes;
    GameObject[,] chunks;

    int[] randomShiftIndexArray;
    int[] tileMappingArray;

    int textureDimensionX;
    int textureDimensionY;

    Random.State tileMappingState;
    bool tileMappingStateInitialized = false;


    public void SetNewWidth(string s)
    {
        width = int.Parse(s);
        Debug.Assert(width > 0);
    }

    public void SetNewHeight(string s)
    {
        height = int.Parse(s);
        Debug.Assert(height > 0);
    }

    public void SetSeed(string s)
    {
        int result;
        if(int.TryParse(s,out result))
        {
            seed = result;
        }
    }

    int GetRandomGene()
    {
        return Random.Range(-(useAltTileLibrary ? altTileLibrary : tileLibrary).Length / 2, (useAltTileLibrary ? altTileLibrary : tileLibrary).Length / 2 - (1 - (useAltTileLibrary ? altTileLibrary : tileLibrary).Length % 2));
        //return Random.Range(-tileLibrary.Length / 2, tileLibrary.Length / 2 - 1);
    }

    int MapGeneToIndex(int gene)
    {
        return gene < 0 ? gene + (useAltTileLibrary ? altTileLibrary : tileLibrary).Length / 2 : gene + (useAltTileLibrary ? altTileLibrary : tileLibrary).Length / 2 - (1 - (useAltTileLibrary ? altTileLibrary : tileLibrary).Length % 2);
        //return gene < 0 ? gene + tileLibrary.Length / 2 : gene + tileLibrary.Length / 2 - 1;
    }

    int ClampGene(int gene)
    {
        if(gene < -(useAltTileLibrary ? altTileLibrary : tileLibrary).Length / 2)
        {
            return -(useAltTileLibrary ? altTileLibrary : tileLibrary).Length / 2;
        }

        if(gene > (useAltTileLibrary ? altTileLibrary : tileLibrary).Length / 2 - (1 - (useAltTileLibrary ? altTileLibrary : tileLibrary).Length % 2))
        {
            return (useAltTileLibrary ? altTileLibrary : tileLibrary).Length / 2 - (1 - (useAltTileLibrary ? altTileLibrary : tileLibrary).Length % 2);
        }

        return gene;
    }

    public void Save()
    {
        string path = "BioLoomImage-" +
            System.DateTime.Now.Year + "-" +
            System.DateTime.Now.Month + "-" +
            System.DateTime.Now.Day + "-" +
            System.DateTime.Now.Hour + "-" +
            System.DateTime.Now.Minute + "-" +
            System.DateTime.Now.Second +
            "[" + seed + "]";

        for (int i = 0; i < textureDimensionX; ++i)
        {
            for (int j = 0; j < textureDimensionY; ++j)
            {
                byte[] bytes = textures[i, j].EncodeToPNG();
                string fullPath = path + 
                    "[" + i + "," + (textureDimensionY - j - 1) + "]" +
                    ".png";
                System.IO.File.WriteAllBytes(fullPath, bytes);
                Debug.Log(bytes.Length / 1024 + "Kb was saved as: " + fullPath);
            }
        }
    }

    public void SetShuffleTileMapping(bool shuffle)
    {
        shuffleTileMapping = shuffle;
    }

    public void SetUseSeed(bool use)
    {
        useSeed = use;

        Debug.Assert(seedInputField != null);
        seedInputField.SetActive(use);

        if (use)
        {
            InputField f = seedInputField.GetComponent<InputField>();
            Debug.Assert(f != null);

            f.SetTextWithoutNotify(seed.ToString());
        }
    }

    public void Run()
    {
        Start();
    }

    // Start is called before the first frame update
    void Start()
    {
        if(!tileMappingStateInitialized)
        {
            tileMappingState = Random.state;
            tileMappingStateInitialized = true;
        }

        if (useSeed)
        {
            Random.InitState(seed);
        }
        else
        {
            seed = (int)System.DateTime.Now.Ticks;
            Random.InitState(seed);
        }

        Debug.Assert(currentSeedText != null);
        Text t = currentSeedText.GetComponent<Text>();
        Debug.Assert(t != null);

        t.text = "Seed: " + seed;

        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        Debug.Assert((useAltTileLibrary ? altTileLibrary : tileLibrary) != null);

        textureDimensionX = width * tileWidth / PNGSize;
        if ((width * tileWidth) % PNGSize > 0)
        {
            ++textureDimensionX;
        }

        textureDimensionY = height * tileHeight / PNGSize;
        if((height * tileHeight) % PNGSize > 0)
        {
            ++textureDimensionY;
        }

        textures = new Texture2D[textureDimensionX, textureDimensionY];
        chunks = new GameObject[textureDimensionX, textureDimensionY];

        for (int i = 0; i < textureDimensionX; ++i)
        {
            for (int j = 0; j < textureDimensionY; ++j)
            {
                textures[i, j] = new Texture2D(Mathf.Min(width * tileWidth, PNGSize), Mathf.Min(height * tileHeight, PNGSize));
                Debug.Assert(textures[i, j] != null);

                textures[i, j].filterMode = FilterMode.Point;

                Sprite sprite = Sprite.Create(textures[i, j], new Rect(0, 0, textures[i, j].width, textures[i, j].height), new Vector2(0.5f, 0.5f), 1);
                Debug.Assert(sprite != null);

                chunks[i, j] = GameObject.Instantiate(chunkPrefab);
                Debug.Assert(chunks[i, j] != null);

                chunks[i, j].transform.position = new Vector3(i * PNGSize, j * PNGSize);
                chunks[i, j].transform.parent = transform;

                SpriteRenderer sr = chunks[i, j].GetComponent<SpriteRenderer>();
                Debug.Assert(sr != null);

                sr.sprite = sprite;
            }
        }

        randomShiftIndexArray = new int[width];
        for (int i = 0; i < width; ++i)
        {
            randomShiftIndexArray[i] = i;
        }

        tileMappingArray = new int[(useAltTileLibrary ? altTileLibrary : tileLibrary).Length];
        for(int i= 0; i < (useAltTileLibrary ? altTileLibrary : tileLibrary).Length; ++i)
        {
            tileMappingArray[i] = i;
        }

        if (shuffleTileMapping)
        {
            Random.State tempState = Random.state;
            Random.state = tileMappingState;
            Shuffle(tileMappingArray);
            tileMappingState = Random.state;
            Random.state = tempState;
        }

        Initialize();
    }

    void Initialize()
    {
        genes = new int[width, height];

        currentGenerationIndex = 0;
        nextGenerationIndex = currentGenerationIndex + 1;

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                AssignGeneToTile(x, y, -100); //hack!

                if (y == currentGenerationIndex)
                {
                    int randomGene = GetRandomGene();

                    AssignGeneToTile(x, y, randomGene);
                }
            }
        }

        foreach (Texture2D t in textures)
        {
            t.Apply();
        }
    }

    void AssignGeneToTile(int x, int y, int gene)
    {
        Debug.Assert(textures != null);
        Debug.Assert(genes != null);

        if(gene == 0)
        {
            return;
        }

        int index = MapGeneToIndex(gene);
        if (index >= 0 && index < (useAltTileLibrary ? altTileLibrary : tileLibrary).Length)
        {
            int textureX = (x * tileWidth) / PNGSize;
            int textureY = (y * tileHeight) / PNGSize;

            int chunkX = (x * tileWidth) % PNGSize;
            int chunkY = (y * tileHeight) % PNGSize;

            Texture2D t = (useAltTileLibrary ? altTileLibrary : tileLibrary)[tileMappingArray[index]].texture;
            Rect r = (useAltTileLibrary ? altTileLibrary : tileLibrary)[tileMappingArray[index]].textureRect;
            textures[textureX, textureY].SetPixels(chunkX, chunkY, tileWidth, tileHeight, t.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height));

            genes[x, y] = gene;
        }
        else
        {
            //TODO: display an X or something
            int textureX = (x * tileWidth) / PNGSize;
            int textureY = (y * tileHeight) / PNGSize;

            int chunkX = (x * tileWidth) % PNGSize;
            int chunkY = (y * tileHeight) % PNGSize;

            Texture2D t = defaultSprite.texture;
            Rect r = defaultSprite.textureRect;
            textures[textureX, textureY].SetPixels(chunkX, chunkY, tileWidth, tileHeight, t.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height));

            //Color[] colors = new Color[tileHeight * tileWidth];
            //for(int i = 0; i < tileHeight * tileWidth; ++i)
            //{
            //    colors[i] = Color.white;
            //}

            //textures[textureX, textureY].SetPixels(chunkX, chunkY, tileWidth, tileHeight, colors);


            genes[x, y] = 0;
        }
    }

    // Update is called once per frame
    void Update()
    {
        
        if (nextGenerationIndex > 0)
        {
            for(int i = 0; i < 512; ++i)
            {
                RunBarricelli();
            }

            foreach (Texture2D t in textures)
            {
                t.Apply();
            }
        }
    }

    public void RunBarricelli()
    {
        if(nextGenerationIndex >= height)
        {
            return;
        }

        SimulateRow();

        ++currentGenerationIndex;
        ++nextGenerationIndex;
    }

    void SimulateRow()
    {
        Shuffle(randomShiftIndexArray);
        for(int i = 0; i < width; ++i)
        {
            Shift(randomShiftIndexArray[i]);
        }
    }

    private void Shift(int i)
    {
        //n := j := this generation[i];
        int j = genes[i, currentGenerationIndex];

        int n = j;

        if(n == 0 || n == (useAltTileLibrary ? altTileLibrary : tileLibrary).Length)
        {
            return;
        }

        int reproductionCounter = 0;

        //reproduce: if j = 0 then goto next i;
        do
        {
            //k:= modulo 512 of(i) plus: (j);
            int k = i + j;
            while (k < 0 || k >= width)
            {
                k = (k + width) % width;
            }

            //if next generation[k] > 0 then
            int bi1 = genes[k, nextGenerationIndex];

            if (bi1 != 0)
            {
                switch (mutationType)
                {
                    case MutationType.None:
                        AssignGeneToTile(k, nextGenerationIndex, bi1 == n ? n : (useAltTileLibrary ? altTileLibrary : tileLibrary).Length);
                        break;
                    case MutationType.Average:
                        if (genes[k, currentGenerationIndex] != 0)
                        {
                            int m = (n + bi1) / 2;

                            AssignGeneToTile(k, nextGenerationIndex, m);
                        }
                        break;
                    case MutationType.Random:
                        if (genes[k, currentGenerationIndex] != 0)
                        {
                            int m = GetRandomGene();

                            AssignGeneToTile(k, nextGenerationIndex, m);
                        }
                        break;
                    case MutationType.Barricelli:
                        if (genes[k, currentGenerationIndex] == 0 || genes[k, currentGenerationIndex] == (useAltTileLibrary ? altTileLibrary : tileLibrary).Length)
                        {
                            int d = FindDistance(k);
                            ClampGene(d);

                            AssignGeneToTile(k, nextGenerationIndex, d);
                        }
                        else
                        {
                            AssignGeneToTile(k, nextGenerationIndex, (useAltTileLibrary ? altTileLibrary : tileLibrary).Length);
                        }
                        break;
                }
                break;
            }
            else
            {
                //next generation[k] := n;
                AssignGeneToTile(k, nextGenerationIndex, n);
            }

            //j:= this generation[k];
            j = genes[k, currentGenerationIndex];

            ++reproductionCounter;
        } while (j != 0 && j != (useAltTileLibrary ? altTileLibrary : tileLibrary).Length && j != n && reproductionCounter < maxReproductions);
    }

    int FindDistance(int k)
    {
        int rCount = 0;
        int r = k;

        while(genes[r, currentGenerationIndex] == 0 || genes[r, currentGenerationIndex] == (useAltTileLibrary ? altTileLibrary : tileLibrary).Length)
        {
            rCount++;
            r++;
            r = r % width;
        }

        int lCount = 0;
        int l = k;

        while (genes[l, currentGenerationIndex] == 0 || genes[l, currentGenerationIndex] == (useAltTileLibrary ? altTileLibrary : tileLibrary).Length)
        {
            lCount++;
            l--;
            l = (l + width) % width;
        }

        if( (genes[r, currentGenerationIndex] > 0 && genes[l, currentGenerationIndex] > 0) ||
            (genes[r, currentGenerationIndex] < 0 && genes[l, currentGenerationIndex] < 0) )
        {
            return rCount + lCount;
        }
        else
        {
            return -(rCount + lCount);
        }
    }

    public static void Shuffle<T>(T[] array)
    {
        int n = array.Length;
        while (n > 1)
        {
            int k = Random.Range(0, n--);
            T temp = array[n];
            array[n] = array[k];
            array[k] = temp;
        }
    }
}
