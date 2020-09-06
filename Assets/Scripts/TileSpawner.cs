using UnityEngine;
using UnityEngine.UI;

public class TileSpawner : MonoBehaviour
{
    public GameObject chunkPrefab;
    public GameObject seedInputField;
    public GameObject seedInputFieldText;
    public GameObject currentSeedText;
    public GameObject heightField;
    public GameObject widthField;
    public Sprite[] tileLibrary;
    public Sprite[] altTileLibrary;

    public bool useAltTileLibrary;

    public Sprite[] TileLibrary => useAltTileLibrary ? altTileLibrary : tileLibrary;

    public void SetUseAltTileLibrary(bool b)
    {
        useAltTileLibrary = b;

        Debug.Assert(tileLibrary != null && tileLibrary.Length > 0);
        Debug.Assert(altTileLibrary != null && altTileLibrary.Length > 0);
    }

    public Sprite DefaultSprite => TileLibrary[TileLibrary.Length -1];

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
        AverageWithRandom,
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
    Cell[,] cells;
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
        return Random.Range(GetMinGene(), GetMaxGene());
    }

    int GetMinGene()
    {
        int length = TileLibrary.Length;
        return -length / 2 + (1 - length % 2);
    }

    int GetMaxGene()
    {
        int length = TileLibrary.Length;
        return length / 2;
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
        Initialize();
    }

    // Start is called before the first frame update
    void Start()
    {
        var field = heightField.GetComponent<InputField>();
        field.SetTextWithoutNotify(height.ToString());

        field = widthField.GetComponent<InputField>();
        field.SetTextWithoutNotify(width.ToString());

        Initialize();
    }

    void Initialize()
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

        {
            Debug.Assert(currentSeedText != null);
            Text t = currentSeedText.GetComponent<Text>();
            Debug.Assert(t != null);

            t.text = "Seed: " + seed;
        }

        foreach (Transform child in transform)
        {
            GameObject.Destroy(child.gameObject);
        }

        Debug.Assert(TileLibrary != null);

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

        tileMappingArray = new int[TileLibrary.Length];
        for(int i= 0; i < TileLibrary.Length; ++i)
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

        cells = new Cell[width, height];
        Cell.Min = GetMinGene();
        Cell.Max = GetMaxGene();

        currentGenerationIndex = 0;
        nextGenerationIndex = currentGenerationIndex + 1;

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                if (y == currentGenerationIndex)
                {
                    int randomGene = GetRandomGene();

                    AssignGeneToTile(x, y, randomGene, Cell.CellStatus.Normal);
                }
                else
                {
                    AssignGeneToTile(x, y, 0, Cell.CellStatus.Empty);
                }
            }
        }

        foreach (Texture2D t in textures)
        {
            t.Apply();
        }
    }

    void AssignGeneToTile(int x, int y, int gene, Cell.CellStatus status)
    {
        Debug.Assert(textures != null);
        Debug.Assert(cells != null);

        cells[x, y].Gene = gene;
        cells[x, y].Status = status;

        int textureX = (x * tileWidth) / PNGSize;
        int textureY = (y * tileHeight) / PNGSize;

        int chunkX = (x * tileWidth) % PNGSize;
        int chunkY = (y * tileHeight) % PNGSize;

        Texture2D t;
        Rect r;

        if (status == Cell.CellStatus.Normal)
        {
            int index = cells[x, y].Index;
            t = TileLibrary[tileMappingArray[index]].texture;
            r = TileLibrary[tileMappingArray[index]].textureRect;
        }
        else
        {
            t = DefaultSprite.texture;
            r = DefaultSprite.textureRect;
        }

        textures[textureX, textureY].SetPixels(chunkX, chunkY, tileWidth, tileHeight, t.GetPixels((int)r.x, (int)r.y, (int)r.width, (int)r.height));
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
        Cell start = cells[i, currentGenerationIndex];
        Cell current = start;

        if(start.Status != Cell.CellStatus.Normal)
        {
            return;
        }

        //reproduce: if j = 0 then goto next i;
        int reproductionCounter = 0;
        do
        {
            //k:= modulo 512 of(i) plus: (j);
            int k = i + current.Gene;
            while (k < 0 || k >= width)
            {
                k = (k + width) % width;
            }

            //if next generation[k] != 0 then we have a collision
            Cell destination = cells[k, nextGenerationIndex];

            if (destination.Status != Cell.CellStatus.Empty)
            {
                switch (mutationType)
                {
                    case MutationType.None:
                        if(destination.Gene != start.Gene)
                        {
                            AssignGeneToTile(k, nextGenerationIndex, 0, Cell.CellStatus.Collision);
                        }
                        break;
                    case MutationType.Average:
                        if (cells[k, currentGenerationIndex].Status != Cell.CellStatus.Empty)
                        {
                            int m = (start.Gene + destination.Gene) / 2;

                            AssignGeneToTile(k, nextGenerationIndex, m, Cell.CellStatus.Normal);
                        }
                        break;
                    case MutationType.AverageWithRandom:
                        if (cells[k, currentGenerationIndex].Status != Cell.CellStatus.Empty)
                        {
                            int a = (start.Gene + destination.Gene) / 2;
                            int r = GetRandomGene();
                            int m = a * 4 + r;
                            m /= 5;

                            AssignGeneToTile(k, nextGenerationIndex, m, Cell.CellStatus.Normal);
                        }
                        break;
                    case MutationType.Random:
                        if (cells[k, currentGenerationIndex].Status != Cell.CellStatus.Empty)
                        {
                            int m = GetRandomGene();

                            AssignGeneToTile(k, nextGenerationIndex, m, Cell.CellStatus.Normal);
                        }
                        break;
                    case MutationType.Barricelli:
                        if (cells[k, currentGenerationIndex].Status == Cell.CellStatus.Normal)
                        {
                            AssignGeneToTile(k, nextGenerationIndex, 0, Cell.CellStatus.Collision);
                        }
                        else
                        {
                            int d = FindDistance(k);

                            AssignGeneToTile(k, nextGenerationIndex, d, Cell.CellStatus.Normal);
                        }
                        break;
                }
                break;
            }
            else
            {
                // no collision

                //next generation[k] := n;
                AssignGeneToTile(k, nextGenerationIndex, start.Gene, Cell.CellStatus.Normal);
            }

            //j:= this generation[k];
            current = cells[k, currentGenerationIndex];

            ++reproductionCounter;
        } while (current.Status == Cell.CellStatus.Normal && current.Gene != start.Gene && reproductionCounter < maxReproductions);
    }

    int FindDistance(int k)
    {
        int rCount = 0;
        int r = k;

        while(cells[r, currentGenerationIndex].Status != Cell.CellStatus.Normal)
        {
            rCount++;
            r++;
            r = r % width;
        }

        int lCount = 0;
        int l = k;

        while (cells[l, currentGenerationIndex].Status == Cell.CellStatus.Normal)
        {
            lCount++;
            l--;
            l = (l + width) % width;
        }

        if( (cells[r, currentGenerationIndex].Gene > 0 && cells[l, currentGenerationIndex].Gene > 0) ||
            (cells[r, currentGenerationIndex].Gene < 0 && cells[l, currentGenerationIndex].Gene < 0) )
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