//extern alias SpeckleNewtonsoft;
using System.Collections.Generic;

using SpeckleCore;
using System.Linq;

namespace SpeckleGrasshopper.Utilities
{
  public static class SpecklStreamExtentions
  {
    public static Dictionary<string, object> ToDictionary(this SpeckleStream speckleStream)
    {
      var dictionary = new Dictionary<string, object>();
      dictionary.Add("StreamId", speckleStream.StreamId);
      dictionary.Add("Name", speckleStream.Name);
      dictionary.Add("Description", speckleStream.Description);
      dictionary.Add("Layers", speckleStream.Layers?.Select(x => x.Name).ToList());
      //dictionary.Add("TotalObjects", speckleStream.Objects?.Count);
      dictionary.Add("Tags", speckleStream.Tags == null ? new List<string> { "null" } : speckleStream.Tags);
      dictionary.Add("Parent", speckleStream.Parent == null ? "null" : speckleStream.Parent);
      dictionary.Add("Children", speckleStream.Children == null ? new List<string> { "null" } : speckleStream.Children);
      dictionary.Add("Ancestors", speckleStream.Ancestors == null ? new List<string> { "null" } : speckleStream.Ancestors);
      return dictionary;
    }
  }
}
