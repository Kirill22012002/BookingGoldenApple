using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace BGA.Users.API.Extensions;

public static class DictionaryExtensions
{
    public static ModelStateDictionary ToModelStateDictionary(this IDictionary<string, string[]> sourceErrors)
    {
        var modelStateDictionary = new ModelStateDictionary();
        foreach (var item in sourceErrors)
        {
            foreach (var value in item.Value)
            {
                modelStateDictionary.AddModelError(item.Key, value);
            }
        }

        return modelStateDictionary;
    }
}
