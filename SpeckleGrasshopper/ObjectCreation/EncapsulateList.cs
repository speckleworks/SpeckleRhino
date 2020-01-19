using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Types;
using SpeckleGrasshopper.Properties;

namespace SpeckleGrasshopper
{
  public class EncapsulateList : GH_Component
  {
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    public EncapsulateList( )
      : base( "Encapsulate a List", "EL",
          "Encapsulates a list to be able to set it in the properties of a speckle object. Mostly because Dimitrie is confused about list and tree management in grasshopper.",
          "Speckle", "Special" )
    {
    }


    public override Guid ComponentGuid
    {
      get { return new Guid( "{9D7837B0-F497-466B-92C0-ECF4CA39E97F}" ); }
    }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Resources.GenericIconXS;
      }
    }

    protected override void RegisterInputParams( GH_InputParamManager pManager )
    {
      pManager.AddGenericParameter( "List", "L", "List to encapsulate.", GH_ParamAccess.list );
    }

    protected override void RegisterOutputParams( GH_OutputParamManager pManager )
    {
      pManager.AddGenericParameter( "System List", "SL", "Encapsulated list", GH_ParamAccess.item );
    }

    protected override void SolveInstance( IGH_DataAccess DA )
    {
      var myList = new List<object>();
      DA.GetDataList( 0, myList);
      // DO NOT JUDGE
      try
      {
        DA.SetData( 0, new GH_ObjectWrapper( myList.Select( o => o.GetType().GetProperty( "Value" ).GetValue( o, null ) ).Cast<int>().ToList() ) );
        return;
      }
      catch { }
      try
      {
        DA.SetData( 0, new GH_ObjectWrapper( myList.Select( o => o.GetType().GetProperty( "Value" ).GetValue( o, null ) ).Cast<double>().ToList() ) );
        return;
      }
      catch { }
      try
      {
        DA.SetData( 0, new GH_ObjectWrapper( myList.Select( o => o.GetType().GetProperty( "Value" ).GetValue( o, null ) ).Cast<string>().ToList() ) );
        return;
      }
      catch { }
      try
      {
        DA.SetData( 0, new GH_ObjectWrapper( myList.Select( o => o.GetType().GetProperty( "Value" ).GetValue( o, null ) ).Cast<bool>().ToList() ) );
        return;
      }
      catch { }
      DA.SetData( 0, new GH_ObjectWrapper( myList.Select( o => o.GetType().GetProperty( "Value" ).GetValue( o, null ) ).ToList() ) );
    }
  }
}
