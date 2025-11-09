using System.Collections.Generic;
using System.Linq;
using EnumLookup.Generated; 
using UnityEngine;

public class AssetLoader
{
    public void LinqWarningSample()
    {
        var list = new List<int> {1, 2, 3, 4};
        
        list.Where(x => x % 2 == 0)
            .ToList()
            .ForEach(x => Debug.Log(x));
    }
    
    public void EnumLookupSample(Dictionary<string, string> catalog)  
    {
        foreach (var pair in catalog)
        {
            var assetCategory = pair.Key.ToAssetHelper(); 
            var assetHelper = pair.Key.ToAssetHelper();  
        }
    }
}