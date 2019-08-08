//extern alias SpeckleNewtonsoft;
using SNJ = Newtonsoft.Json;

using System;
using System.Collections.Generic;
using System.Reflection;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using System.Windows.Forms;
using System.Linq;
using System.Collections;
using GH_IO.Serialization;

namespace SpeckleGrasshopper.UserDataUtils
{
  public class SchemaBuilderComponent : GH_Component, IGH_VariableParameterComponent
  {

    public Type SelectedType;
    public List<PropertyInfo> SelectedTypeProps;
    public List<PropertyInfo> OptionalProps;
    public ToolStripMenuItem OptionalPropDropdown;

    public Dictionary<string, bool> OptionalPropsMask;
    public List<ToolStripItem> OptionalPropsItems;
    public bool CheckItAll = false;

    public SchemaBuilderComponent( )
      : base( "Schema Builder Component", "SBC",
              "Builds Speckle Types through reflecting upon SpeckleCore and SpeckleKits.",
              "Speckle", "SpeckleKits" )
    {
      GenerateOptionalPropsMenu();
    }

    public void GenerateOptionalPropsMenu( Dictionary<string, bool> mask = null )
    {
      // holds all the speckle object props (the optional ones!)
      OptionalProps = typeof( SpeckleCore.SpeckleObject ).GetProperties( BindingFlags.Public | BindingFlags.Instance ).Where( pinfo => pinfo.Name != "Type" ).ToList();

      // Set up optional properties mask and generate the toolstrip menu that we will add in the dropdown
      OptionalPropsMask = new Dictionary<string, bool>();
      OptionalPropsItems = new List<ToolStripItem>();

      foreach ( var prop in OptionalProps )
      {
        var defaultStaus = false;
        if ( mask != null )
        {
          if ( mask.ContainsKey( prop.Name ) ) defaultStaus = mask[ prop.Name ];
        }

        OptionalPropsMask.Add( prop.Name, defaultStaus );
        var tsi = new ToolStripMenuItem( prop.Name ) { Name = prop.Name, Checked = defaultStaus, CheckOnClick = true };

        tsi.CheckStateChanged += ( sender, e ) =>
        {
          var key = ( ( ToolStripMenuItem ) sender ).Name;
          OptionalPropsMask[ key ] = !OptionalPropsMask[ key ];

          if ( OptionalPropsMask[ key ] )
            RegisterPropertyAsInputParameter( prop, Params.Input.Count );
          else
            UnregisterPropertyInput( prop );

          Params.OnParametersChanged();
          ExpireSolution( true );
        };
        OptionalPropsItems.Add( tsi );
      }

      OptionalPropsItems.Add( new ToolStripSeparator() );

      OptionalPropsItems.Add( new ToolStripButton( "Expand/Collapse All", System.Drawing.SystemIcons.Warning.ToBitmap(), ( sender, e ) =>
      {
        CheckItAll = !CheckItAll;
        int k = 0;
        foreach ( var key in OptionalPropsMask.Keys.ToList() )
        {
          ( ( ToolStripMenuItem ) OptionalPropDropdown.DropDownItems[ k++ ] ).CheckState = CheckItAll ? CheckState.Checked : CheckState.Unchecked;
        }

      } ) );
    }

    public override void AppendAdditionalMenuItems( ToolStripDropDown menu )
    {
      base.AppendAdditionalMenuItems( menu );

      OptionalPropDropdown = GH_DocumentObject.Menu_AppendItem( menu, "Overwrite Custom Properties" );
      OptionalPropDropdown.DropDownItems.AddRange( OptionalPropsItems.ToArray() );
      OptionalPropDropdown.DropDown.Closing += ( sender, e ) =>
      {
        if ( e.CloseReason == ToolStripDropDownCloseReason.ItemClicked ) e.Cancel = true;
      };

      Menu_AppendSeparator( menu );

      var subMenus = new Dictionary<Assembly, ToolStripDropDownMenu>();
      var types = SpeckleCore.SpeckleInitializer.GetTypes();
      var assemblies = SpeckleCore.SpeckleInitializer.GetAssemblies().Where( ass => types.Any( t => t.Assembly == ass ) );

      try
      {
        foreach ( Assembly assembly in assemblies )
        {
          menu.Items.Add( assembly.GetName().Name );
          var addedMenuItem = menu.Items[ menu.Items.Count - 1 ];
          subMenus[ assembly ] = ( ToolStripDropDownMenu ) addedMenuItem.GetType().GetProperty( "DropDown" ).GetValue( addedMenuItem );
        }

        foreach ( Type type in types )
          subMenus[ type.Assembly ].Items.Add( type.Name, null, ( sender, e ) => SwitchToType( type ) );
      }
      catch ( Exception e )
      {
        System.Diagnostics.Debug.WriteLine( e.Message );
      }
    }

    /// <summary>
    /// Handles the change to the selected type.
    /// </summary>
    /// <param name="myType"></param>
    public void SwitchToType( Type myType )
    {
      if ( SelectedType == myType ) return;

      // unregister old
      if ( SelectedType != null )
        foreach ( var p in SelectedType.GetProperties( BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public ).Where( pinfo => pinfo.Name != "Type" ) )
          UnregisterPropertyInput( p );

      // register new
      int k = 0;
      foreach ( var p in myType.GetProperties( BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public ).Where( pinfo => pinfo.Name != "Type" ) )
        RegisterPropertyAsInputParameter( p, k++ );

      Message = myType.Name;
      SelectedType = myType;
      Params.Output[ 0 ].NickName = myType.Name;
      Params.OnParametersChanged();
      ExpireSolution( true );
    }

    /// <summary>
    /// Adds a property to the component's inputs.
    /// </summary>
    /// <param name="prop"></param>
    void RegisterPropertyAsInputParameter( PropertyInfo prop, int index )
    {
      // get property name and value
      Type propType = prop.PropertyType;

      string propName = prop.Name;
      object propValue = prop;

      // Create new param based on property name
      Param_GenericObject newInputParam = new Param_GenericObject();
      newInputParam.Name = propName;
      newInputParam.NickName = propName;
      newInputParam.MutableNickName = false;
      newInputParam.Description = propName + " as " + propType.Name;
      newInputParam.Optional = true;

      // check if input needs to be a list or item access
      bool isCollection = typeof( System.Collections.IEnumerable ).IsAssignableFrom( propType ) && propType != typeof( string ) && !propType.Name.ToLower().Contains( "dictionary" );
      if ( isCollection == true )
      {
        newInputParam.Access = GH_ParamAccess.list;
      }
      else
      {
        newInputParam.Access = GH_ParamAccess.item;
      }
      Params.RegisterInputParam( newInputParam, index );
    }

    public void UnregisterPropertyInput( PropertyInfo myProp )
    {
      for ( int i = Params.Input.Count - 1; i >= 0; i-- )
      {
        if ( Params.Input[ i ].Name == myProp.Name )
        {
          Params.UnregisterInputParameter( Params.Input[ i ] );
          return;
        }
      }
    }

    /// <summary>
    /// Makes sure we deserialise correctly, and reinstate everything there is to reinstate:
    /// - selected type properties
    /// - optional properties
    /// </summary>
    /// <param name="reader"></param>
    /// <returns></returns>
    public override bool Read( GH_IReader reader )
    {
      bool isInit = reader.GetBoolean( "init" );
      if ( isInit )
      {
        var selectedTypeName = reader.GetString( "type" );
        var selectedTypeAssembly = reader.GetString( "assembly" );
        var myOptionalProps = SpeckleCore.Converter.getObjFromBytes( reader.GetByteArray( "optionalmask" ) ) as Dictionary<string, bool>;

        var selectedType = SpeckleCore.SpeckleInitializer.GetTypes().FirstOrDefault( t => t.Name == selectedTypeName ); //&& t.AssemblyQualifiedName == selectedTypeAssembly );
        SelectedType = selectedType;
        Message = SelectedType.Name;

        GenerateOptionalPropsMenu( myOptionalProps );

        if ( selectedType == null )
        {
          AddRuntimeMessage( GH_RuntimeMessageLevel.Error, string.Format( "Type {0} from the {1} kit was not found. Are you sure you have it installed?", selectedTypeName, selectedTypeAssembly ) );
        }
      }

      return base.Read( reader );
    }

    /// <summary>
    /// Serialises the current state of the component, making sure we save:
    /// - the optional property dictionary
    /// - the current type.
    /// </summary>
    /// <param name="writer"></param>
    /// <returns></returns>
    public override bool Write( GH_IWriter writer )
    {
      if ( SelectedType != null )
      {
        writer.SetBoolean( "init", true );
        writer.SetString( "type", SelectedType.Name );
        writer.SetString( "assembly", SelectedType.AssemblyQualifiedName );
        writer.SetByteArray( "optionalmask", SpeckleCore.Converter.getBytes( OptionalPropsMask ) );
      }
      else
        writer.SetBoolean( "init", false );

      return base.Write( writer );
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams( GH_Component.GH_InputParamManager pManager )
    {
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams( GH_Component.GH_OutputParamManager pManager )
    {
      pManager.Register_GenericParam( "Object", "Object", "The created object." );
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance( IGH_DataAccess DA )
    {
      if ( SelectedType is null )
      {
        AddRuntimeMessage( GH_RuntimeMessageLevel.Warning, "Please select an object to initialise." );
        return;
      }

      // instantiate object !!
      var outputObject = Activator.CreateInstance( SelectedType );
      DA.SetData( 0, outputObject );

      for ( int i = 0; i < Params.Input.Count; i++ )
      {
        if ( Params.Input[ i ].Access == GH_ParamAccess.list )
        {
          var ObjectsList = new List<object>();
          DA.GetDataList( i, ObjectsList );

          if ( ObjectsList.Count == 0 ) continue;

          var listForSetting = ( IList ) Activator.CreateInstance( outputObject.GetType().GetProperty( Params.Input[ i ].Name ).PropertyType );
          foreach ( var item in ObjectsList )
          {
            object innerVal = null;
            try
            {
              innerVal = item.GetType().GetProperty( "Value" ).GetValue( item );
            }
            catch
            {
              innerVal = item;
            }

            listForSetting.Add( innerVal );
          }

          outputObject.GetType().GetProperty( Params.Input[ i ].Name ).SetValue( outputObject, listForSetting, null );
        }
        else if ( Params.Input[ i ].Access == GH_ParamAccess.item )
        {
          object ghInput = null; // INPUT OBJECT ( PROPERTY )
          DA.GetData( i, ref ghInput );

          if ( ghInput == null ) continue;

          object innerValue = null;
          try
          {
            innerValue = ghInput.GetType().GetProperty( "Value" ).GetValue( ghInput );
          }
          catch
          {
            innerValue = ghInput;
          }

          if ( innerValue == null ) continue;

          PropertyInfo prop = outputObject.GetType().GetProperty( Params.Input[ i ].Name );
          if ( prop.PropertyType.IsEnum )
          {
            try
            {
              prop.SetValue( outputObject, Enum.Parse( prop.PropertyType, ( string ) innerValue ) );
              continue;
            }
            catch { }

            try
            {
              prop.SetValue( outputObject, ( int ) innerValue );
              continue;
            }
            catch { }

            this.AddRuntimeMessage( GH_RuntimeMessageLevel.Error, "Unable to set " + Params.Input[ i ].Name + "." );
          }

          else if ( innerValue.GetType() != prop.PropertyType )
          {
            try
            {
              var conv = SpeckleCore.Converter.Serialise( innerValue );
              prop.SetValue( outputObject, conv );
              continue;
            }
            catch { }

            try
            {
              prop.SetValue( outputObject, innerValue );
              continue;
            }
            catch { }

            try
            {
              var conv = SNJ.JsonConvert.DeserializeObject( ( string ) innerValue, prop.PropertyType );
              prop.SetValue( outputObject, conv );
              continue;
            }
            catch { }

            this.AddRuntimeMessage( GH_RuntimeMessageLevel.Error, "Unable to set " + Params.Input[ i ].Name + "." );
          }

          else
          {
            prop.SetValue( outputObject, innerValue );
          }
        }
      }

      // toggle hash generation onyl if it's not overriden
      if ( OptionalPropsMask[ "Hash" ] == false )
        outputObject.GetType().GetMethod( "GenerateHash" ).Invoke( outputObject, null );

      // applicationId generation/setting
      var appId = outputObject.GetType().GetProperty( "ApplicationId" ).GetValue( outputObject );
      if ( appId == null )
      {
        var myGeneratedAppId = "gh/" + outputObject.GetType().GetProperty( "Hash" ).GetValue( outputObject );
        outputObject.GetType().GetProperty( "ApplicationId" ).SetValue( outputObject, myGeneratedAppId );
      }
      DA.SetData( 0, outputObject );
    }

    public bool CanInsertParameter( GH_ParameterSide side, int index )
    {
      return false;
    }

    public bool CanRemoveParameter( GH_ParameterSide side, int index )
    {
      return false;
    }

    public IGH_Param CreateParameter( GH_ParameterSide side, int index )
    {
      return null;
    }

    public bool DestroyParameter( GH_ParameterSide side, int index )
    {
      return false;
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
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        return SpeckleGrasshopper.Properties.Resources.NewObject;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid( "970d1754-b192-405f-a78b-98afb74ee6ca" ); }
    }
  }
}
