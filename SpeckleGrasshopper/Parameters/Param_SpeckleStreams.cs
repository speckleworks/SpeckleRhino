using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SpeckleCore;

namespace SpeckleGrasshopper.Parameters
{
  public class Param_SpeckleStreams : GH_PersistentParam<GH_SpeckleStream>
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public Param_SpeckleStreams() : base(new GH_InstanceDescription("SpeckleStream", "SS", "This is a speckle Stream", "Speckle", "Management"))
    {
    }
    
    public override Guid ComponentGuid => new Guid("{CE914C71-72F6-4D6E-9FA2-7F38FA465228}");

    protected override GH_GetterResult Prompt_Plural(ref List<GH_SpeckleStream> values)
    {
      throw new NotImplementedException();
    }

    protected override GH_GetterResult Prompt_Singular(ref GH_SpeckleStream value)
    {
      throw new NotImplementedException();
    }
  }

  public class GH_SpeckleStream : GH_Goo<SpeckleStream>
  {

    public GH_SpeckleStream(SpeckleStream speckleStream)
    {
      Value = speckleStream;
    }

    public override bool IsValid => Value != null;

    public override string TypeName => "Speckle stream";

    public override string TypeDescription => "Object containing a speckle stream";

    public override IGH_Goo Duplicate()
    {
      var ss = new SpeckleStream()
      {
        Ancestors = Value.Ancestors,

      };
      return new GH_SpeckleStream(ss);

    }

    public override string ToString()
    {
      return $"SpeckleStream, Name: {Value.Name}, ID: {Value.StreamId}";
    }
  }

}

