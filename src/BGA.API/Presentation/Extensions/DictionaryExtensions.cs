using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BGA.API.Presentation.Extensions;

public static class DictionaryExtensions
{
    public static ModelStateDictionary ToModelStateDictionary(this Dictionary<string, string> sourceErrors)
    {
        var modelStateDictionary = new ModelStateDictionary();
        foreach (var item in sourceErrors)
        {
            modelStateDictionary.AddModelError(item.Key, item.Value);
        }

        return modelStateDictionary;
    }
}
