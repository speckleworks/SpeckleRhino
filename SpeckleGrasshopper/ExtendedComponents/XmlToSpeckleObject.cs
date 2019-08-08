//extern alias SpeckleNewtonsoft;
using SNJ = Newtonsoft.Json;
using System;
using System.Collections.Generic;
using SpeckleGrasshopper.Properties;

using Grasshopper.Kernel;
using System.Xml;

using SpeckleCore;

namespace SpeckleGrasshopper.Contrib
{
  public class XmlToSpeckleObject : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the Cmpt_GetPlane class.
    /// </summary>
    public XmlToSpeckleObject( )
      : base( "Xml To SpeckleObject", "XML2SO",
          "Convert XML structure to a SpeckleObject with properties.",
          "Speckle", "Contrib" )
    {
      SpeckleCore.SpeckleInitializer.Initialize();
      SpeckleCore.LocalContext.Init();
    }

    protected override void RegisterInputParams( GH_Component.GH_InputParamManager pManager )
    {
      pManager.AddTextParameter( "Xml", "X", "Input XML as text.", GH_ParamAccess.item );
    }

    protected override void RegisterOutputParams( GH_Component.GH_OutputParamManager pManager )
    {
      pManager.AddGenericParameter( "SpeckleObject", "SO", "SpeckleObject from XML.", GH_ParamAccess.item );
      pManager.AddGenericParameter( "Dictionary", "D", "XML as nested dictionary.", GH_ParamAccess.item);
    }

    protected override void SolveInstance( IGH_DataAccess DA )
    {
      var xml_in = "";

      DA.GetData(0, ref xml_in);
      var xml = new XmlDocument();
      xml.LoadXml(xml_in);

      var jsonText = SNJ.JsonConvert.SerializeXmlNode(xml, SNJ.Formatting.Indented, false);

      SNJ.JsonSerializerSettings jsonSS = new SNJ.JsonSerializerSettings();
      jsonSS.Formatting = SNJ.Formatting.Indented;
      jsonSS.ReferenceLoopHandling = SNJ.ReferenceLoopHandling.Ignore; 
      jsonSS.PreserveReferencesHandling = SNJ.PreserveReferencesHandling.None;
      jsonSS.StringEscapeHandling = SNJ.StringEscapeHandling.EscapeNonAscii;
      jsonSS.NullValueHandling = SNJ.NullValueHandling.Include;
      jsonSS.Converters = new SNJ.JsonConverter[] { new SpecklePropertiesConverter() };

      dynamic obj = SNJ.JsonConvert.DeserializeObject<Dictionary<string, object>>(
        jsonText, jsonSS);

      var so = new SpeckleObject() { Properties = obj };

      DA.SetData( "SpeckleObject", new GH_SpeckleObject(so) );
      DA.SetData("Dictionary", obj);
    }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Resources.GenericIconXS;
      }
    }

    public override Guid ComponentGuid
    {
      get { return new Guid("00aa6993-f6c7-474b-a7bb-3ec56220d292"); }
    }
  }
}
