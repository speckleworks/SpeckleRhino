using System;
using SpeckleGrasshopper.Properties;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace SpeckleGrasshopper.Contrib
{
  public class SerializeTransform : GH_Component
  {

    public SerializeTransform()
      : base("SerializeTransform", "SerialX",
          "Serialize a Transform for Speckle consumption.",
          "Speckle", "Contrib")
    {
    }

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddTransformParameter("Transform", "X", "Transform to serialize.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("String", "S", "Serialized string of matrix elements.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var m_xform = Transform.Identity;

      if (!DA.GetData(0, ref m_xform))
        return;

      DA.SetData(0, String.Join(" ", m_xform.ToFloatArray(true)));
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
      get { return new Guid("a27b6fa7-aa9c-4460-a047-0cf305397c12"); }
    }
  }
}
