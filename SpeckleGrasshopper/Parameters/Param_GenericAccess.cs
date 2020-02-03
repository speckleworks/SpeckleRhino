using System;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;

public class Param_GenericAccess : Param_GenericObject, IDisposable
{
  private const string ParamAccessKey = "ParamAccess";
  public override GH_Exposure Exposure => GH_Exposure.hidden;

  public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
  {
    base.AppendAdditionalMenuItems(menu);
    if (Kind != GH_ParamKind.output)
    {
      var item0 = Menu_AppendItem(menu, "Item", Menu_Clicked, true, Access == GH_ParamAccess.item);
      item0.ToolTipText = "Make this parameter an Item";
      var item1 = Menu_AppendItem(menu, "Array", Menu_Clicked, true, Access == GH_ParamAccess.list);
      item1.ToolTipText = "Make this parameters an Array";
      //var item2 = Menu_AppendItem(menu, "Tree", Menu_Clicked, true, Access == GH_ParamAccess.tree);
      //item2.ToolTipText = "Make this parameter a Tree";
    }
  }

  public void Dispose()
  {
    ClearData();
  }

  private void Menu_Clicked(object sender, EventArgs e)
  {
    var menu = sender as ToolStripMenuItem;
    var name = menu.AccessibilityObject.Name;
    RecordUndoEvent("Changing State");

    if (name.Equals("Tree"))
    {
      //RecordUndoEvent("tree");
      Access = GH_ParamAccess.tree;
    }
    else if (name.Equals("Array"))
    {
      //RecordUndoEvent("array");
      Access = GH_ParamAccess.list;
    }
    else if (name.Equals("Item"))
    {
      //RecordUndoEvent("item");
      Access = GH_ParamAccess.item;
    }

    OnObjectChanged(GH_ObjectEventType.DataMapping);
    ExpireSolution(true);

  }

  public override bool Read(GH_IReader reader)
  {
    var result = base.Read(reader);
    if (reader.ItemExists(ParamAccessKey))
    {
      try
      {
        //In case casting produces invalid Access enum
        Access = (GH_ParamAccess)reader.GetInt32(ParamAccessKey);
      }
      catch (Exception)
      {

      }
    }
    return result;
  }

  public override Guid ComponentGuid => new Guid("{2E711E3A-573E-42AD-86FF-0B6BA23E9990}");


  public override bool Write(GH_IWriter writer)
  {
    var result = base.Write(writer);
    writer.SetInt32(ParamAccessKey, (int)Access);
    return result;
  }

}


