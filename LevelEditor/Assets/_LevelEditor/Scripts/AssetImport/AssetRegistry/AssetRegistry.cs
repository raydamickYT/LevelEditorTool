using System.Collections.Generic;
using UnityEngine;

public class AssetRegistry : MonoBehaviour
{
    public static AssetRegistry Instance { get;}
    public Dictionary<string, ImportedSpriteData> importedSprites = new Dictionary<string, ImportedSpriteData>();
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
