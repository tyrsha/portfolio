using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AssetLoader
{
    public void LinqSample()
    {
        var list = new List<int> {1, 2, 3, 4};
        
        list.Where(x => x % 2 == 0)
            .ToList()
            .ForEach(x => Debug.Log(x));
    }
    
    public void LoadAssets(Dictionary<string, string> catalog)
    {
        foreach (var pair in catalog)
        {
            var assetCategory = pair.Key.ToAssetCategory();
            
            
        }
    }
}
