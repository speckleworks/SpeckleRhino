using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleGrasshopper.Utilities
{
  public static class CollectionExtentions
  {
    public static HashSet<T> ToHastSet<T>(this IEnumerable<T> data)
    {
      var hashSet = new HashSet<T>();

      foreach (var d in data)
      {
        hashSet.Add(d);
      }

      return hashSet;
    }
  }
}
