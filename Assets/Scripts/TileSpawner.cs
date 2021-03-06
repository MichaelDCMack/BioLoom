﻿using UnityEngine;
using UnityEngine.UI;

public class TileSpawner : MonoBehaviour
{
    public GameObject chunkPrefab;
    public GameObject seedInputField;
    public GameObject currentSeedText;
    public GameObject heightField;
    public GameObject widthField;
    public GameObject geneSetDropdown;
    public GameObject brushPreview;

    public GeneSet[] geneSets;

    public int width = 2;
    public int height = 2;

    public int currentGenerationIndex = 0;
    public int nextGenerationIndex = 1;

    public int maxReproductions = 2;

    public int PNGSize = 512;

    public bool shuffleTileMapping = false;
    public bool useSeed = false;
    public int seed = 0;
    public bool populate = true;

    public int geneSetIndex = 0;

    public bool shuffleIndexArray;
    
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

    public void SetGeneSetIndex(int i)
    {
        Debug.Assert(i >= 0 && i < geneSets.Length);
        geneSetIndex = i;
    }

    int geneBrushIndex;

    public void IncrementGeneBrushIndex(int value)
    {
        geneBrushIndex += value;
        geneBrushIndex = Extensions.Mod(geneBrushIndex, GeneSet.Length);
        UpdateGeneBrushPreview();
    }

    Texture2D[,] textures;
    Cell[,] cells;
    GameObject[,] chunks;

    int textureDimensionX;
    int textureDimensionY;

    private Random.State[] rowStates;

    private Camera _camera;

    FPSWatcher FPSWatcher { get; set; }

    private GeneSet GeneSet => geneSets[GeneSetIndex];

    private int GeneSetIndex { get; set; }

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
        _camera = Camera.main;
        
        var field = heightField.GetComponent<InputField>();
        field.SetTextWithoutNotify(height.ToString());

        field = widthField.GetComponent<InputField>();
        field.SetTextWithoutNotify(width.ToString());

        FPSWatcher = GetComponent<FPSWatcher>();

        var dropdown = geneSetDropdown.GetComponent<Dropdown>();
        foreach (var geneSet in geneSets)
        {
            dropdown.options.Add(new Dropdown.OptionData(geneSet.SetName));
        }

        dropdown.value = 0;

        Initialize();
    }

    void Initialize()
    {
        GeneSetIndex = geneSetIndex;
        rowStates = new Random.State[height];

        geneBrushIndex = 0;
        UpdateGeneBrushPreview();

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

        Debug.Assert(GeneSet != null);

        textureDimensionX = width * GeneSet.width / PNGSize;
        if ((width * GeneSet.width) % PNGSize > 0)
        {
            ++textureDimensionX;
        }

        textureDimensionY = height * GeneSet.height / PNGSize;
        if((height * GeneSet.height) % PNGSize > 0)
        {
            ++textureDimensionY;
        }

        textures = new Texture2D[textureDimensionX, textureDimensionY];
        chunks = new GameObject[textureDimensionX, textureDimensionY];

        for (int i = 0; i < textureDimensionX; ++i)
        {
            for (int j = 0; j < textureDimensionY; ++j)
            {
                textures[i, j] = new Texture2D(
                    Mathf.Min(width * GeneSet.width, PNGSize), 
                    Mathf.Min(height * GeneSet.height, PNGSize));
                Debug.Assert(textures[i, j] != null);

                textures[i, j].filterMode = FilterMode.Point;

                Sprite sprite = Sprite.Create(
                    textures[i, j], 
                    new Rect(0, 0, textures[i, j].width, textures[i, j].height), 
                    new Vector2(0f, 0f), 
                    1);
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

        GeneSet.ResetMapping();
        if (shuffleTileMapping)
        {
            Random.State tempState = Random.state;
            GeneSet.ShuffleMapping();
            Random.state = tempState;
        }

        cells = new Cell[width, height];

        currentGenerationIndex = 0;
        nextGenerationIndex = currentGenerationIndex + 1;

        for (int y = 0; y < height; ++y)
        {
            for (int x = 0; x < width; ++x)
            {
                if (populate && y == currentGenerationIndex)
                {
                    int randomGene = GeneSet.GetRandomGene();

                    AssignGeneToTile(x, y, randomGene, randomGene == 0 ? Cell.CellStatus.Empty : Cell.CellStatus.Normal);
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

    private void UpdateGeneBrushPreview()
    {
        var preview = brushPreview.GetComponent<Image>();
        preview.sprite = GeneSet.sprites[geneBrushIndex];
        preview.SetNativeSize();
    }

    void AssignGeneToTile(int x, int y, int gene, Cell.CellStatus status)
    {
        Debug.Assert(textures != null);
        Debug.Assert(cells != null);

        cells[x, y].Gene = Extensions.Clamp(gene, GeneSet.MinGene, GeneSet.MaxGene);
        cells[x, y].Status = status;

        int textureX = (x * GeneSet.width) / PNGSize;
        int textureY = (y * GeneSet.height) / PNGSize;

        int chunkX = (x * GeneSet.width) % PNGSize;
        int chunkY = (y * GeneSet.height) % PNGSize;

        Texture2D t;
        Rect r;

        switch (status)
        {
            case Cell.CellStatus.Normal:
            {
                t = GeneSet.GetSpriteFromGene(cells[x, y].Gene).texture;
                r = GeneSet.GetSpriteFromGene(cells[x, y].Gene).textureRect;
                break;
            }
            case Cell.CellStatus.Collision:
            {
                t = GeneSet.Collision.texture;
                r = GeneSet.Collision.textureRect;
                break;
            }
            default:
            {
                t = GeneSet.Empty.texture;
                r = GeneSet.Empty.textureRect;
                break;
            }
        }

        textures[textureX, textureY].SetPixels(chunkX, chunkY,
            GeneSet.width, GeneSet.height,
            t.GetPixels((int) r.x, (int) r.y, (int) r.width, (int) r.height));
    }

    // Update is called once per frame
    void Update()
    {
        bool applyTextures = false;

        if (Input.GetKey(KeyCode.Space))
        {
            Vector3 currentPosition = Input.mousePosition;
            currentPosition.z = -_camera.transform.position.z;
            Vector3 worldPosition = _camera.ScreenToWorldPoint(currentPosition);

            int x = (int) worldPosition.x / GeneSet.width;
            int y = (int) worldPosition.y / GeneSet.height;

            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                AssignGeneToTile(x, y, geneBrushIndex + GeneSet.MinGene, Cell.CellStatus.Normal);

                for (int i = 0; i < width; ++i)
                {
                    for (int j = y + 1; j < height; ++j)
                    {
                        AssignGeneToTile(i, j, 0, Cell.CellStatus.Empty);
                    }
                }

                applyTextures = true;

                if (currentGenerationIndex > y)
                {
                    currentGenerationIndex = y;
                    nextGenerationIndex = y + 1;

                    Random.state = rowStates[currentGenerationIndex];
                }
            }
        }

        int allowedOperations = FPSWatcher.AllowedOperations;
        for (int operations = 0; operations < allowedOperations; ++operations)
        {
            if (RunBarricelli() == 0)
            {
                break;
            }

            applyTextures = true;
        }

        if (applyTextures)
        {
            foreach (Texture2D t in textures)
            {
                t.Apply();
            }
        }
    }

    int RunBarricelli()
    {
        if(nextGenerationIndex >= height)
        {
            return 0;
        }

        int operations = SimulateRow();

        ++currentGenerationIndex;
        ++nextGenerationIndex;

        return operations;
    }

    int SimulateRow()
    {
        rowStates[currentGenerationIndex] = Random.state;
        
        int[] indexArray = new int[width];
        for (int i = 0; i < width; ++i)
        {
            indexArray[i] = i;
        }

        if (shuffleIndexArray)
        {
            Extensions.Shuffle(indexArray);
        }

        for(int i = 0; i < width; ++i)
        {
            Shift(indexArray[i]);
        }

        return 1;
    }

    private void Shift(int i)
    {
        Cell start = cells[i, currentGenerationIndex];
        Cell current = start;

        if(start.Status != Cell.CellStatus.Normal)
        {
            return;
        }

        int reproductionCounter = 0;
        do
        {
            int k = i + current.Gene;
            k = Extensions.Mod(k, width);
 
            Cell destination = cells[k, nextGenerationIndex];

            switch (destination.Status)
            {
                case Cell.CellStatus.Empty:
                {
                    AssignGeneToTile(k, nextGenerationIndex, start.Gene, Cell.CellStatus.Normal);
                    break;
                }
                case Cell.CellStatus.Normal:
                {
                    //if we're mutating, then return
                    Mutate(start, destination, k);
                    return;
                }
                case Cell.CellStatus.Collision:
                {
                    //if this is a collision from the past, then return
                    return;
                }
            }

            current = cells[k, currentGenerationIndex];
            
            ++reproductionCounter;
            
        } while (current.Status == Cell.CellStatus.Normal && current.Gene != start.Gene && reproductionCounter < maxReproductions);
    }

    void Mutate(Cell start, Cell destination, int k)
    {
        switch (mutationType)
        {
            case MutationType.None:
            {
                if (destination.Gene != start.Gene)
                {
                    AssignGeneToTile(k, nextGenerationIndex, 0, Cell.CellStatus.Collision);
                }

                break;
            }
            case MutationType.Average:
            {
                int m = (start.Gene + destination.Gene) / 2;

                AssignGeneToTile(k, nextGenerationIndex, m, m == 0 ? Cell.CellStatus.Empty : Cell.CellStatus.Normal);
                
                break;
            }
            case MutationType.AverageWithRandom:
            {
                int a = (start.Gene + destination.Gene) / 2;
                int r = GeneSet.GetRandomGene();
                int m = a * 4 + r;
                m /= 5;

                AssignGeneToTile(k, nextGenerationIndex, m, m == 0 ? Cell.CellStatus.Empty : Cell.CellStatus.Normal);
                
                break;
            }
            case MutationType.Random:
            {
                int m = GeneSet.GetRandomGene();

                AssignGeneToTile(k, nextGenerationIndex, m, m == 0 ? Cell.CellStatus.Empty : Cell.CellStatus.Normal);

                break;
            }
            case MutationType.Barricelli:
            {
                if (cells[k, currentGenerationIndex].Status == Cell.CellStatus.Normal)
                {
                    AssignGeneToTile(k, nextGenerationIndex, 0, Cell.CellStatus.Collision);
                }
                else
                {
                    AssignGeneToTile(k, nextGenerationIndex, FindDistance(k), Cell.CellStatus.Normal);
                }

                break;
            }
        }
    }

    int FindDistance(int k)
    {
        int rCount = 0;
        int r = k;

        while(cells[r, currentGenerationIndex].Status != Cell.CellStatus.Normal)
        {
            ++rCount;
            ++r;
            r = Extensions.Mod(r, width);
        }

        int lCount = 0;
        int l = k;

        while (cells[l, currentGenerationIndex].Status != Cell.CellStatus.Normal)
        {
            ++lCount;
            --l;
            l = Extensions.Mod(l, width);
        }

        bool bothPositive = cells[r, currentGenerationIndex].Gene > 0 && cells[l, currentGenerationIndex].Gene > 0;
        bool bothNegative = cells[r, currentGenerationIndex].Gene < 0 && cells[l, currentGenerationIndex].Gene < 0;
        if( bothPositive || bothNegative )  // this causes a positive number bias, maybe change this if creating my own
        {
            return rCount + lCount;
        }

        return -(rCount + lCount);
    }
}