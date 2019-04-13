using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SpeckleCore;

namespace SpeckleGrasshopper
{
  public class ExpandSpeckleObject : GH_Component, IGH_VariableParameterComponent
  {

    Dictionary<string, List<object>> global;
    Action expireComponent, setInputsAndExpireComponent;

    /// <summary>
    /// Initializes a new instance of the MyComponent1 class.
    /// </summary>
    public ExpandSpeckleObject( )
      : base( "Expand Dictionary or SpeckleObject", "EUD",
          "Expands a SpeckleObject's properties or a dictionary into its component key value pairs.",
          "Speckle", "Special" )
    {
      expireComponent = ( ) =>
      {
        this.ExpireSolution( true );
      };

      setInputsAndExpireComponent = ( ) =>
      {
        for ( int i = Params.Output.Count - 1; i >= 0; i-- )
        {
          var myParam = Params.Output[ i ];
          if ( ( !global.Keys.Contains( myParam.Name ) ) || ( !global.Keys.Contains( myParam.NickName ) ) )
          {
            Params.UnregisterOutputParameter( myParam, true );
          }
        }

        //Params.OnParametersChanged();
        foreach ( var key in global.Keys )
        {
          var myparam = Params.Output.FirstOrDefault( q => q.Name == key );
          if ( myparam == null )
          {
            Param_GenericObject newParam = getGhParameter( key );
            Params.RegisterOutputParam( newParam );
          }
        }

        Params.OnParametersChanged();
        //end
        this.ExpireSolution( true );
      };
    }

    public override void AddedToDocument( GH_Document document )
    {
      base.AddedToDocument( document );
      Debug.WriteLine( this.Params.Output.Count );
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams( GH_Component.GH_InputParamManager pManager )
    {
      pManager.AddGenericParameter( "Dictionaries", "D", "Dictionaries or Speckle Objects to expand.", GH_ParamAccess.list );
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams( GH_Component.GH_OutputParamManager pManager )
    {
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance( IGH_DataAccess DA )
    {
      List<object> objs = new List<object>();
      objs = Params.Input[ 0 ].VolatileData.AllData( true ).ToList<object>();

      if ( objs.Count == 0 )
      {
        this.AddRuntimeMessage( GH_RuntimeMessageLevel.Warning, "No dictionaries found." );
        return;
      }

      global = new Dictionary<string, List<object>>();
      var first = true;

      foreach ( var obj in objs )
      {
        var goo = obj as GH_ObjectWrapper;
        if ( goo == null )
        {
          this.AddRuntimeMessage( GH_RuntimeMessageLevel.Warning, "We don't like nulls." );
          return;
        }
        //ArchivableDictionary dict = goo.Value as ArchivableDictionary;

        var dict = new Dictionary<string, object>();
        if ( goo.Value is SpeckleObject )
          dict = ( ( SpeckleObject ) goo.Value ).Properties;
        else if ( goo.Value is Dictionary<string, object> )
          dict = goo.Value as Dictionary<string, object>;

        if ( dict != null )
        {
          foreach ( var key in dict.Keys )
          {
            if ( ( first ) )
            {
              global.Add( key, new List<object>() );
              global[ key ].Add( dict[ key ] );
            }

            else if ( !global.Keys.Contains( key ) )
            {
              this.AddRuntimeMessage( GH_RuntimeMessageLevel.Error, "Object dictionaries do not match." );
              return;
            }
            else
            {
              global[ key ].Add( dict[ key ] );
            }
          }
        }
        first = false;
      }

      if ( global.Keys.Count == 0 )
      {
        this.AddRuntimeMessage( GH_RuntimeMessageLevel.Warning, "Empty dictionary." );
        return;
      }

      var changed = false;

      if ( Params.Output.Count != global.Keys.Count )
      {
        changed = true;
      }

      Debug.WriteLine( "changed:" + changed );

      if ( changed )
      {
        Rhino.RhinoApp.MainApplicationWindow.Invoke( setInputsAndExpireComponent );
      }
      else
      {
        int k = 0;
        foreach ( var key in global.Keys )
        {
          Params.Output[ k ].Name = Params.Output[ k ].NickName = key;
          var results = new List<object>();
          var isNestedList = false;
          foreach ( var x in global[ key ] )
          {
            var t = x.GetType();
            if ( x is IEnumerable<SpeckleObject> )
            {
              results.Add( Converter.Deserialise( x as IEnumerable<SpeckleObject> ) );
              isNestedList = true;
              continue;
            }
            else if ( x is IEnumerable<int> || x is IEnumerable<double> || x is IEnumerable<string> || x is IEnumerable<bool> )
            {
              switch ( x )
              {
                case IEnumerable<int> l:
                  results.Add( l );
                  break;
                case IEnumerable<double> l:
                  results.Add( l );
                  break;
                case IEnumerable<bool> l:
                  results.Add( l );
                  break;
                case IEnumerable<string> l:
                  results.Add( l );
                  break;
              }
              isNestedList = true;
              continue;
            }
            else if ( x is IEnumerable<object> && !( x is IEnumerable<SpeckleObject> ) )
            {
              results.Add( ( ( IEnumerable<object> ) x ).Select( xx => { var res =  Converter.Deserialise( xx as SpeckleObject ); return res == null ? xx : res ; } ).ToList() );
              isNestedList = true;
              continue;
            }
            else if ( x is IDictionary )
            {
              results.Add( new GH_ObjectWrapper( x ) );
              continue;
            }
            else
            {
              if ( x is bool || x is string || x is double || x is int )
                results.Add( x );
              else
                results.Add( new GH_ObjectWrapper( Converter.Deserialise( x as SpeckleObject ) ) );
              continue;
            }
          }

          if ( !isNestedList )
            DA.SetDataList( k++, results );
          else
          {
            var tree = new DataTree<object>();
            ToDataTree( results, ref tree, new List<int> { 0 } );
            DA.SetDataTree( k++, tree );
          }
          //DA.SetDataList( k++, global[ key ].Select( x =>
          //{
          //  if ( x is IEnumerable<SpeckleObject> )
          //    return Converter.Deserialise( x as IEnumerable<SpeckleObject> );
          //  return new GH_ObjectWrapper( Converter.Deserialise( x as SpeckleObject ) );
          //} ) );
        }
      }
    }

    void ToDataTree( IEnumerable list, ref DataTree<object> Tree, List<int> path )
    {
      int k = 0;
      int b = 0;
      bool addedRecurse = false;
      foreach ( var item in list )
      {
        if ( ( item is IEnumerable ) && !( item is string ) )
        {
          if ( !addedRecurse )
          {
            path.Add( b );
            addedRecurse = true;
          }
          else
            path[ path.Count - 1 ]++;

          ToDataTree( item as IEnumerable, ref Tree, path );
        }
        else
        {
          GH_Path Path = new GH_Path( path.ToArray() );
          Tree.Insert( item, Path, k++ );
        }
      }
    }

    private Param_GenericObject getGhParameter( string key )
    {
      Param_GenericObject newParam = new Param_GenericObject();
      newParam.Name = ( string ) key;
      newParam.NickName = ( string ) key;
      newParam.MutableNickName = false;
      newParam.Access = GH_ParamAccess.list;
      return newParam;
    }

    bool IGH_VariableParameterComponent.CanInsertParameter( GH_ParameterSide side, Int32 index )
    {
      return false;
    }
    bool IGH_VariableParameterComponent.CanRemoveParameter( GH_ParameterSide side, Int32 index )
    {
      return false;
    }
    bool IGH_VariableParameterComponent.DestroyParameter( GH_ParameterSide side, Int32 index )
    {
      return false;
    }
    IGH_Param IGH_VariableParameterComponent.CreateParameter( GH_ParameterSide side, Int32 index )
    {
      return null;
    }

    public void VariableParameterMaintenance( )
    {
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Properties.Resources.ExpandUserData;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid( "{D69F00DD-8F8C-4CE2-8B51-DAE8362844F7}" ); }
    }
  }
}
