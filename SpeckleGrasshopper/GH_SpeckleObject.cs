using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SpeckleCore;
using GH_IO.Serialization;
using Grasshopper.Kernel.Types;
using Grasshopper.Kernel;

namespace SpeckleGrasshopper
{
  public class GH_SpeckleObject : GH_Goo<SpeckleObject>
  {
    #region Constructors
    public GH_SpeckleObject( ) : this( null ) { }

    public GH_SpeckleObject( SpeckleObject native ) { this.Value = native; }

    public override IGH_Goo Duplicate( )
    {
      if ( Value == null )
        return new GH_SpeckleObject( null );
      else
      {
        return new GH_SpeckleObject( SpeckleObject.FromJson( Value.ToJson() ) );
      }
    }
    #endregion

    public override string ToString( )
    {
      if ( Value == null ) return "Null SpeckleObject";
      return Value.ToString();
    }

    public override string TypeName => "SpeckleObjectGoo";
    public override string TypeDescription => "SpeckleObjectGoo";
    public override object ScriptVariable( ) => Value;

    public override bool IsValid
    {
      get
      {
        if ( Value == null ) return false;
        return true;
      }
    }
    public override string IsValidWhyNot
    {
      get
      {
        if ( Value == null ) return "No data.";
        return string.Empty;
      }
    }

    #region Casting
    public override bool CastFrom( object source )
    {
      if ( source == null ) return false;
      if ( source is SpeckleObject speckleObject )
      {
        Value = speckleObject;
        return true;
      }
      if ( source is GH_SpeckleObject ghSpeckleObject )
      {
        Value = ghSpeckleObject.Value;
        return true;
      }
      try
      {
        Value = Converter.Serialise( source ) as SpeckleObject;
        return true;
      }
      catch { return false; }
    }

    public override bool CastTo<Q>( ref Q target )
    {
      if ( Value == null ) return false;

      // Could implement something here to cast to relevant types if it is
      // contained in the SpeckleObject
      /*
                  if (typeof(Q).IsAssignableFrom(typeof(GH_Mesh)))
                  {
                      object mesh = new GH_Mesh(Value.GetBoundingMesh());

                      target = (Q)mesh;
                      return true;
                  }
                  if (typeof(Q).IsAssignableFrom(typeof(GH_Brep)))
                  {
                      object blank = new GH_Brep(Value.GetBoundingBrep());

                      target = (Q)blank;
                      return true;
                  }
                  if (typeof(Q).IsAssignableFrom(typeof(GH_Curve)))
                  {
                      object cl = new GH_Curve(Value.Centreline);
                      target = (Q)cl;
                      return true;
                  }
      */
      return false;
    }

    #endregion

    #region Serialization

    public override bool Write( GH_IWriter writer )
    {
      if ( Value == null ) return false;

      var json = Value.ToJson();
      writer.SetString( "SpeckleJson", json );

      return true;
    }

    public override bool Read( GH_IReader reader )
    {
      if ( !reader.ItemExists( "SpeckleJson" ) )
      {
        Value = null;
        return true;
        //throw new Exception("No SpeckleJson found.");
      }

      string json = reader.GetString( "SpeckleJson" );
      Value = SpeckleObject.FromJson( json );

      if ( Value == null )
        throw new Exception( "Failed to wrangle SpeckleObject." );

      return true;
    }
    #endregion

  }

  public class SpeckleObjectParameter : GH_PersistentParam<GH_SpeckleObject>
  {
    public SpeckleObjectParameter() : this("SpeckleObject", "SpeckleObject", "This is a SpeckleObject.", "Speckle", "Parameters") { }
    public SpeckleObjectParameter(string name, string nickname, string description, string category, string subcategory)
        : base(name, nickname, description, category, subcategory) { }
    public SpeckleObjectParameter(GH_InstanceDescription tag) : base(tag) { }

    //public override GH_Exposure Exposure => GH_Exposure.secondary;
    protected override System.Drawing.Bitmap Icon => Properties.Resources.GenericIconXS;

    // TODO: Create new GUID
    public override System.Guid ComponentGuid => new Guid("{9be1632a-a114-4780-9dbe-d56c32938c95}");
    protected override GH_GetterResult Prompt_Singular(ref GH_SpeckleObject value)
    {
      return GH_GetterResult.cancel;
    }
    protected override GH_GetterResult Prompt_Plural(ref List<GH_SpeckleObject> values)
    {
      return GH_GetterResult.cancel;
    }
  }
}
