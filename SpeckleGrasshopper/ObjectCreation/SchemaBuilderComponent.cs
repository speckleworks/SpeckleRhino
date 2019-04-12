using System;
using System.Collections.Generic;
using System.Reflection;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using System.Windows.Forms;
using System.Linq;
using System.Collections;

namespace SpeckleGrasshopper.UserDataUtils
{
  public class SchemaBuilderComponent : GH_Component
  {

    /// <summary>
    /// Initializes a new instance of the SchemaBuilderComponent class.
    /// </summary>


    // global variables
    public bool showAdditionalProps = false;
    public bool showApplicationId_Only = false;
    public Type selectedType = null;
    public bool checkAll = false;

    PropertyInfo[ ] props = typeof( SpeckleCore.SpeckleObject ).GetProperties( BindingFlags.Public | BindingFlags.Instance ).Skip( 1 ).ToArray();
    ToolStripMenuItem[ ] collectionItems = new ToolStripMenuItem[ typeof( SpeckleCore.SpeckleObject ).GetProperties( BindingFlags.Public | BindingFlags.Instance ).Skip( 1 ).ToArray().Length ];

    public override void AppendAdditionalMenuItems( ToolStripDropDown menu )
    {
      base.AppendAdditionalMenuItems( menu );

      ///////////////////////////////////////////////////////////////////////////////

      ToolStripMenuItem myDropDown = GH_DocumentObject.Menu_AppendItem( menu, "Overwrite Custom Properties" );
      myDropDown.ShowDropDown();

      if ( selectedType != null )
      {
        bool isEmpty = collectionItems.All( x => x == null );

        if ( !isEmpty )
        {
          //unsuscribe from event
          for ( int i = 0; i < props.Length; i++ )
            collectionItems[ i ].CheckStateChanged -= CheckStateChangeEvent;


          for ( int i = 0; i < props.Length; i++ )
          {
            myDropDown.DropDownItems.Add( collectionItems[ i ] );
            collectionItems[ i ].CheckOnClick = true;
            collectionItems[ i ].CheckStateChanged += CheckStateChangeEvent;
          }

          myDropDown.DropDown.Closing += DropDown_Closing;
          void DropDown_Closing( object sender, ToolStripDropDownClosingEventArgs e )
          {
            if ( e.CloseReason == ToolStripDropDownCloseReason.ItemClicked )
            {
              e.Cancel = true;
            }
          }

          //-------------------------------------------------------------------------------------------------

          myDropDown.DropDownItems.Add( new ToolStripSeparator() );

          //-------------------------------add global toggle to (un)check everything----------------------------------------------------------

          ToolStripButton checkitall = new ToolStripButton( "Expand/Collapse", System.Drawing.SystemIcons.Warning.ToBitmap(), CheckAllToggle );
          checkitall.AutoToolTip = false;

          myDropDown.DropDownItems.Add( checkitall );

          void CheckAllToggle( object sender, EventArgs e )
          {

            for ( int i = 0; i < props.Length; i++ )
              collectionItems[ i ].CheckStateChanged -= CheckStateChangeEvent;
            if ( checkAll == false )
            {
              for ( int i = 0; i < props.Length; i++ )
              {
                collectionItems[ i ].CheckState = CheckState.Checked;
              }
              checkAll = true;

            }
            else
            {
              for ( int i = 0; i < props.Length; i++ )
              {
                collectionItems[ i ].CheckState = CheckState.Unchecked;
              }
              checkAll = false;
            }

            myDropDown.DropDown.Refresh();
            UpdateOptionalInputs();


            for ( int i = 0; i < props.Length; i++ )
              collectionItems[ i ].CheckStateChanged += CheckStateChangeEvent;

          }
        }
      }
      else // if ToolStripMenuItems is empty, initialize them with selected prop type
      {
        myDropDown.Enabled = false;
        for ( int i = 0; i < props.Length; i++ )
        {
          //propsStatus[i] = false;
          PropertyInfo prop = props[ i ];
          ToolStripMenuItem item = new ToolStripMenuItem( prop.Name );
          item.Name = prop.Name;
          item.Text = prop.Name;
          collectionItems[ i ] = item;
          item.Checked = false;
        }
        this.ExpireSolution( true );
      }
      //-------------------------------------------------------------------------------------------------



      ///////////////////////////////////////////////////////////////////////////////

      GH_DocumentObject.Menu_AppendSeparator( menu );

      ///////////////////////////////////////////////////////////////////////////////


      var foundtypes = SpeckleCore.SpeckleInitializer.GetTypes();
      Dictionary<Assembly, ToolStripDropDownMenu> subMenus = new Dictionary<Assembly, ToolStripDropDownMenu>();

      var assemblies = SpeckleCore.SpeckleInitializer.GetAssemblies().Where( ass => foundtypes.Any( t => t.Assembly == ass ) );

      foreach ( Assembly assembly in assemblies )
      {
        menu.Items.Add( assembly.GetName().Name );
        var addedMenuItem = menu.Items[ menu.Items.Count - 1 ];

        subMenus[ assembly ] = ( ToolStripDropDownMenu ) addedMenuItem.GetType().GetProperty( "DropDown" ).GetValue( addedMenuItem );
      }

      foreach ( Type type in foundtypes )
      {

        subMenus[ type.Assembly ].Items.Add( type.Name, null, ( item, e ) =>
           {

             if ( selectedType != null && selectedType.Equals( type ) ) // Same selected type as before
          {
            //graceful exit: don't do anything because type is the same as before.
          }
             else if ( selectedType != null && !selectedType.Equals( type ) ) // Different selected type as before, reinit input!
          {
               AdaptInputs( type );
               selectedType = type;
             }
             else if ( selectedType == null ) // Type was null
          {
               InitInputsFromScratch( type );
               selectedType = type;
             }
           } );
      }

    }

    // MENU UI ENDS HERE
    ///////////////////////////////////////////////////////////////////////////////



    void CheckStateChangeEvent( object sender, EventArgs e )
    {
      UpdateOptionalInputs();
    }

    void UpdateOptionalInputs( )
    {
      Rhino.RhinoApp.WriteLine( "e" );

      props = selectedType.BaseType.GetProperties( BindingFlags.Public | BindingFlags.Instance ).Skip( 1 ).ToArray();

      string[ ] InputNames = new string[ Params.Input.Count ];
      for ( int i = 0; i < InputNames.Length; i++ )
        InputNames[ i ] = Params.Input[ i ].NickName;

      for ( int i = 0; i < collectionItems.Length; i++ )
      {
        if ( collectionItems[ i ].CheckState is CheckState.Checked )
        {
          // check if exists in params. if exists, do nothing. if does not exist, add
          string colName = collectionItems[ i ].Name;

          if ( !InputNames.Contains( colName ) )
            RegisterPropertyAsInputParameter( props[ i ] );
        }
        else if ( collectionItems[ i ].CheckState is CheckState.Unchecked )
        {
          string colName = collectionItems[ i ].Name;
          // check if exists in params. if exists, remove. if does not exist, do nothing.

          if ( InputNames.Contains( colName ) )
          {
            for ( int j = 0; j < Params.Input.Count; j++ )
            {
              if ( Params.Input[ j ].Name == colName )
                Params.UnregisterInputParameter( Params.Input[ j ] );
            }
          }
        }
      }

      this.Params.OnParametersChanged();
      this.ExpireSolution( true );
    }

    void InitInputsFromScratch( Type myType )
    {
      DeleteInputs();

      Console.WriteLine( myType.ToString() );
      this.Message = myType.Name;

      PropertyInfo[ ] props = new PropertyInfo[ ] { };
      props = myType.GetProperties( BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public ).Skip( 1 ).ToArray();

      List<Param_GenericObject> inputParams;
      RegisterInputParamerters( props, out inputParams );

      InitOutput( myType );
      this.Params.OnParametersChanged();
      this.ExpireSolution( true );
    }

    // Delete inputs when object type is updated by the user
    void DeleteInputs( )
    {
      for ( int i = this.Params.Input.Count - 1; i >= 0; i-- )
        Params.UnregisterInputParameter( this.Params.Input[ i ] );
    }



    void RegisterPropertyAsInputParameter( PropertyInfo prop )
    {
      // get property name and value
      Type propType = prop.PropertyType;
      Type baseType = propType.BaseType;

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
      bool isCollection = typeof( System.Collections.IEnumerable ).IsAssignableFrom( propType ) && propType != typeof( string );
      if ( isCollection == true )
      {
        newInputParam.Access = GH_ParamAccess.list;
      }
      else
      {
        newInputParam.Access = GH_ParamAccess.item;
      }
      Params.RegisterInputParam( newInputParam );

    }

    void RegisterInputParamerters( PropertyInfo[ ] props, out List<Param_GenericObject> inputParams )
    {
      List<Param_GenericObject> _inputParams = new List<Param_GenericObject>();


      for ( int i = 0; i < props.Length; i++ )
      {
        // get property name and value
        Type propType = props[ i ].PropertyType;
        Type baseType = propType.BaseType;

        string propName = props[ i ].Name;
        object propValue = props[ i ];

        // Create new param based on property name
        Param_GenericObject newInputParam = new Param_GenericObject();
        newInputParam.Name = propName;
        newInputParam.NickName = propName;
        newInputParam.MutableNickName = false;
        newInputParam.Description = propName + " as " + propType.Name;
        newInputParam.Optional = true;

        // check if input needs to be a list or item access
        bool isCollection = typeof( System.Collections.IEnumerable ).IsAssignableFrom( propType ) && propType != typeof( string );
        if ( isCollection == true )
        {
          newInputParam.Access = GH_ParamAccess.list;
        }
        else
        {
          newInputParam.Access = GH_ParamAccess.item;
        }

        Params.RegisterInputParam( newInputParam );
        _inputParams.Add( newInputParam );
      }

      inputParams = _inputParams;
    }

    void AdaptInputs( Type myType )
    {
      this.Message = myType.Name;


      PropertyInfo[ ] nextProps = myType.GetProperties( BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public ).Skip( 1 ).ToArray();


      PropertyInfo[ ] currentProps = selectedType.GetProperties( BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public ).Skip( 1 ).ToArray();
      string[ ] currentPropsNames = new string[ currentProps.Length ];
      for ( int i = 0; i < currentProps.Length; i++ )
        currentPropsNames[ i ] = Params.Input[ i ].Name;

      for ( int i = this.Params.Input.Count - 1; i >= 0; i-- )
      {
        if ( currentPropsNames.Contains( Params.Input[ i ].Name ) )

          Params.UnregisterInputParameter( Params.Input[ i ] );

      }


      for ( int i = nextProps.Length - 1; i >= 0; i-- )
      {
        // get property name and value
        Type propType = nextProps[ i ].PropertyType;
        Type baseType = propType.BaseType;

        string propName = nextProps[ i ].Name;
        object propValue = nextProps[ i ];

        // Create new param based on property name
        Param_GenericObject newInputParam = new Param_GenericObject();
        newInputParam.Name = propName;
        newInputParam.NickName = propName;
        newInputParam.MutableNickName = false;
        newInputParam.Description = propName + " as " + propType.Name;
        newInputParam.Optional = true;

        // check if input needs to be a list or item access
        bool isCollection = typeof( System.Collections.IEnumerable ).IsAssignableFrom( propType ) && propType != typeof( string );
        if ( isCollection == true )
        {
          newInputParam.Access = GH_ParamAccess.list;
        }
        else
        {
          newInputParam.Access = GH_ParamAccess.item;
        }

        Params.RegisterInputParam( newInputParam, 0 );

      }

      /*
        DeleteInputs();
      List<Param_GenericObject> inputParams = new List<Param_GenericObject>();
      RegisterInputParamerters(props, out inputParams);
      */
      InitOutput( myType );
      this.Params.OnParametersChanged();
      this.ExpireSolution( true );
    }

    void InitOutput( Type myType )
    {
      this.Params.Output[ 0 ].NickName = myType.Name;
      this.Message = myType.Name;
    }

    public SchemaBuilderComponent( )
      : base( "Schema Builder Component", "SBC",
              "Builds Speckle Types through reflecting upon SpeckleCore and SpeckleKits.",
              "Speckle", "User Data Utils" )
    {
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
      if ( selectedType is null )
      {
        return;
      }

      // instantiate object !!
      var outputObject = Activator.CreateInstance( selectedType );
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

          try
          {
            outputObject.GetType().GetProperty( Params.Input[ i ].Name ).SetValue( outputObject, innerValue );
          }
          catch
          {
            outputObject.GetType().GetProperty( Params.Input[ i ].Name ).SetValue( outputObject, SpeckleCore.Converter.Serialise( innerValue ) );
          }
        }
      }

      // toggle hash generation
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

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        return SpeckleGrasshopper.Properties.Resources.SchemaBuilder;
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
