using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

using GH_IO.Serialization;
using System.Diagnostics;
using Grasshopper.Kernel.Parameters;

using Grasshopper;
using Grasshopper.Kernel.Data;

using Newtonsoft.Json;
using System.Dynamic;

using SpeckleCore;
using SpeckleRhinoConverter;
using SpeckleGrasshopper.Properties;

namespace SpeckleGrasshopper
{

  public class EncodeToSpeckle : GH_Component
  {
    public EncodeToSpeckle( )
      : base( "Serialiser", "SRL",
          "Serialises a Rhino object to a Speckle object.",
          "Speckle", "Converters" )
    {
    }

    public override Guid ComponentGuid
    {
      get { return new Guid( "{c4442de1-c440-40ba-8da7-33c89eb1a529}" ); }
    }

    protected override void RegisterInputParams( GH_InputParamManager pManager )
    {
      pManager.AddGenericParameter( "Object", "O", "Objects to convert.", GH_ParamAccess.item );
    }

    protected override void RegisterOutputParams( GH_OutputParamManager pManager )
    {
      pManager.AddGenericParameter( "Conversion Result String", "S", "Conversion result string.", GH_ParamAccess.item );
      pManager.AddGenericParameter( "Conversion Result", "R", "Conversion result object.", GH_ParamAccess.item );
    }

    protected override void SolveInstance( IGH_DataAccess DA )
    {
      object myObj = null;
      DA.GetData( 0, ref myObj );

      var result = myObj.GetType().GetProperty( "Value" ).GetValue(myObj);

      //object result = null;
      object conv;
      if ( result != null )
        conv = SpeckleCore.Converter.Serialise( result );
      else
        conv = SpeckleCore.Converter.Serialise( myObj );

      DA.SetData( 0, JsonConvert.SerializeObject( conv, Formatting.Indented ) );
      DA.SetData( 1, conv );
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Resources.GenericIconXS;
      }
    }
  }

  public class DecodeFromSpeckle : GH_Component
  {
    public DecodeFromSpeckle( )
      : base( "Deserialiser", "DSR",
          "Deserialises Speckle (geometry) objects to Rhino objects.",
          "Speckle", "Converters" )
    {
    }

    protected override void RegisterInputParams( GH_InputParamManager pManager )
    {
      pManager.AddGenericParameter( "Object", "O", "Objects to cast.", GH_ParamAccess.item );
    }

    protected override void RegisterOutputParams( GH_OutputParamManager pManager )
    {
      pManager.AddGenericParameter( "Conversion Result", "R", "Conversion result object.", GH_ParamAccess.item );
    }

    protected override void SolveInstance( IGH_DataAccess DA )
    {
      object myObj = null;
      DA.GetData( 0, ref myObj );

      if ( myObj == null )
        return;

      var cast = myObj as Grasshopper.Kernel.Types.GH_ObjectWrapper;
      var result = Converter.Deserialize( ( SpeckleObject ) cast.Value );
      //var result = SpeckleCore.Converter.FromAbstract( (SpeckleAbstract) cast.Value );
      DA.SetData( 0, new Grasshopper.Kernel.Types.GH_ObjectWrapper( result ) );
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Resources.GenericIconXS;
      }
    }

    public override Guid ComponentGuid
    {
      get { return new Guid( "{43b4f541-d914-471e-9f37-72291db2f2d4}" ); }
    }
  }

}
