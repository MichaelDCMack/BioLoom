using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Windows;

public class SpriteCreator : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void CreateNewSprite()
    {
        Texture2D tex = new Texture2D(8, 8, TextureFormat.RGB24, false);

        tex.filterMode = FilterMode.Point;
        tex.SetPixel(0, 0, Color.white);
        tex.SetPixel(7, 7, Color.red);
        tex.Apply();

        SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>();
        Debug.Assert(sr != null);

        sr.sprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 1);

        //// Encode texture into PNG
        //byte[] bytes = tex.EncodeToPNG();
        //Object.Destroy(tex);

        //File.WriteAllBytes(Application.dataPath + "/Tiles/TestPNG.png", bytes);
    }
}
