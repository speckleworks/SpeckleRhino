using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Newtonsoft.Json;
using SpeckleCore;

namespace SpecklePopup
{
  public partial class MainWindow : Window
  {
    List<string> existingServers = new List<string>();
    List<string> existingServers_fullDetails = new List<string>();
    List<Account> accounts = new List<Account>();

    bool validationCheckPass = false;

    Uri ServerAddress;
   
    public string restApi;
    public string apitoken;

    public MainWindow( )
    {
      InitializeComponent();
      this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

      this.DragRectangle.MouseDown += ( sender, e ) =>
      {
        this.DragMove();
      };

      accounts = LocalContext.GetAllAccounts();

      //string strPath = System.Environment.GetFolderPath( System.Environment.SpecialFolder.LocalApplicationData ) + @"\SpeckleSettings";

      //if ( Directory.Exists( strPath ) && Directory.EnumerateFiles( strPath, "*.txt" ).Count() > 0 )
      //  foreach ( string file in Directory.EnumerateFiles( strPath, "*.txt" ) )
      //  {
      //    string content = File.ReadAllText( file );
      //    string[ ] pieces = content.TrimEnd( '\r', '\n' ).Split( ',' );

      //    accounts.Add( new SpeckleAccount() { email = pieces[ 0 ], apiToken = pieces[ 1 ], serverName = pieces[ 2 ], restApi = pieces[ 3 ], rootUrl = pieces[ 4 ] } );
      //  }

      //var gridView = new GridView();

      AccountListBox.ItemsSource = accounts;
    }

    private void Rectangle_MouseDown( object sender, MouseButtonEventArgs e )
    {
      this.DragMove();
    }

    private string ValidateRegister( )
    {
      Debug.WriteLine( "validating..." );
      string validationErrors = "";

      Uri uriResult;
      bool IsUrl = Uri.TryCreate( RegisterServerUrl.Text, UriKind.Absolute, out uriResult ) &&
          ( uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps );

      if ( !IsUrl )
        validationErrors += "Invalid server url. \n";

      ServerAddress = uriResult;

      MailAddress addr = null;
      try
      {
        addr = new System.Net.Mail.MailAddress( this.RegisterEmail.Text );
      }
      catch
      {
        validationErrors += "Invalid email address. \n";
      }

      string password = this.RegisterPassword.Password;

      if ( password.Length <= 8 )
        validationErrors += "Password too short (<8). \n";

      if ( password != this.RegisterPasswordConfirm.Password )
        validationErrors += "Passwords do not match. \n";

      return validationErrors;
    }

    private string ValidateLogin( )
    {
      Debug.WriteLine( "validating..." );
      string validationErrors = "";

      Uri uriResult;
      bool IsUrl = Uri.TryCreate( LoginServerUrl.Text, UriKind.Absolute, out uriResult ) &&
          ( uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps );

      if ( !IsUrl )
        validationErrors += "Invalid server url. \n";

      ServerAddress = uriResult;

      MailAddress addr = null;
      try
      {
        addr = new System.Net.Mail.MailAddress( this.LoginEmail.Text );
      }
      catch
      {
        validationErrors += "Invalid email address. \n";
      }

      return validationErrors;
    }

    private void saveAccountToDisk( string _email, string _apitoken, string _serverName, string _restApi, string _rootUrl )
    {

      string strPath = System.Environment.GetFolderPath( System.Environment.SpecialFolder.LocalApplicationData );

      System.IO.Directory.CreateDirectory( strPath + @"\SpeckleSettings" );

      strPath = strPath + @"\SpeckleSettings\";

      string fileName = _email + "." + _apitoken.Substring( 0, 4 ) + ".txt";

      string content = _email + "," + _apitoken + "," + _serverName + "," + _restApi + "," + _rootUrl;

      Debug.WriteLine( content );

      System.IO.StreamWriter file = new System.IO.StreamWriter( strPath + fileName );
      file.WriteLine( content );
      file.Close();
    }

    private void CancelButton_Click( object sender, RoutedEventArgs e )
    {
      this.Close();
    }

    private void AccountListBox_MouseDoubleClick( object sender, MouseButtonEventArgs e )
    {
      this.restApi = this.accounts[ this.AccountListBox.SelectedIndex ].RestApi;
      this.apitoken = this.accounts[ this.AccountListBox.SelectedIndex ].Token;
      this.Close();
    }

    private void ButonUseSelected_Click( object sender, RoutedEventArgs e )
    {
      if ( !( this.AccountListBox.SelectedIndex != -1 ) )
      {
        MessageBox.Show( "Please select an account first." );
        return;
      }
      this.restApi = this.accounts[ this.AccountListBox.SelectedIndex ].RestApi;
      this.apitoken = this.accounts[ this.AccountListBox.SelectedIndex ].Token;
      this.Close();
    }

    private void RegisterButton_Click( object sender, RoutedEventArgs e )
    {
      RegisterButton.IsEnabled = false;
      RegisterButton.Content = "Contacting server...";
      var errs = ValidateRegister();
      if ( errs != "" )
      {
        MessageBox.Show( errs );
        RegisterButton.IsEnabled = true;
        RegisterButton.Content = "Register";
        return;
      }

      var names = this.RegisterName.Text.Split( ' ' );
      var myUser = new User()
      {
        Email = this.RegisterEmail.Text,
        Password = this.RegisterPassword.Password,
        Name = names?[ 0 ],
        Surname = names.Length >= 2 ? names?[ 1 ] : null,
        Company = this.RegisterCompany.Text,
      };

      string rawPingReply = "";
      dynamic parsedReply = null;
      using ( var client = new WebClient() )
      {
        try
        {
          rawPingReply = client.DownloadString( ServerAddress.ToString() );
          parsedReply = JsonConvert.DeserializeObject( rawPingReply );
        }
        catch { MessageBox.Show( "Failed to contact " + ServerAddress.ToString() ); RegisterButton.IsEnabled = true; RegisterButton.Content = "Register"; return; }
      }

      var spkClient = new SpeckleApiClient() { BaseUrl = ServerAddress.ToString() };
      try
      {
        var response = spkClient.UserRegisterAsync( myUser ).Result;
        if ( response.Success == false )
        {
          MessageBox.Show( "Failed to register user. " + response.Message ); RegisterButton.IsEnabled = true; RegisterButton.Content = "Register"; return;
        }

        var serverName = parsedReply.serverName;

        saveAccountToDisk( this.RegisterEmail.Text, response.Resource.Apitoken, (string) serverName, this.RegisterServerUrl.Text, this.RegisterServerUrl.Text );

        MessageBox.Show( "Account creation ok: You're good to go." );
        this.restApi = this.RegisterServerUrl.Text;
        this.apitoken = response.Resource.Apitoken;
        RegisterButton.IsEnabled = true;
        RegisterButton.Content = "Register";
        this.Close();
      }
      catch ( Exception err )
      {
        MessageBox.Show( "Failed to register user. " + err.InnerException.ToString() ); RegisterButton.IsEnabled = true; RegisterButton.Content = "Register";  return;
      }
    }

    private void LoginButton_Click( object sender, RoutedEventArgs e )
    {
      var errs = ValidateLogin();
      if ( errs != "" )
      {
        MessageBox.Show( errs );
        return;
      }

      var myUser = new User()
      {
        Email = this.LoginEmail.Text,
        Password = this.LoginPassword.Password,
      };

      var spkClient = new SpeckleApiClient() { BaseUrl = ServerAddress.ToString() };

      string rawPingReply = "";
      dynamic parsedReply = null;
      using ( var client = new WebClient() )
      {
        try
        {
          rawPingReply = client.DownloadString( ServerAddress.ToString() );
          parsedReply = JsonConvert.DeserializeObject( rawPingReply );
        }
        catch { MessageBox.Show( "Failed to contact " + ServerAddress.ToString() ); RegisterButton.IsEnabled = true; RegisterButton.Content = "Register"; return; }
      }

      var existing = accounts.FirstOrDefault( account => account.Email == myUser.Email && account.RestApi == ServerAddress.ToString() );
      if(existing != null)
      {
        MessageBox.Show( "You already have an account on " + ServerAddress.ToString() + " with " + myUser.Email + "." );
        return;
      }


      try
      {
        var response = spkClient.UserLoginAsync( myUser ).Result;
        if ( response.Success == false )
        {
          MessageBox.Show( "Failed to login. " + response.Message ); return;
        }

        var serverName = parsedReply.serverName;

        saveAccountToDisk( myUser.Email, response.Resource.Apitoken, ( string ) serverName, this.ServerAddress.ToString(), this.ServerAddress.ToString() );

        MessageBox.Show( "Account login ok: You're good to go." );
        this.restApi = this.RegisterServerUrl.Text;
        this.apitoken = response.Resource.Apitoken;
        
        this.Close();
      }
      catch ( Exception err )
      {
        MessageBox.Show( "Failed to login user. " + err.InnerException.ToString() ); return;
      }

    }
  }

  public class SpeckleAccount
  {
    public string email { get; set; }
    public string apiToken { get; set; }
    public string serverName { get; set; }
    public string restApi { get; set; }
    public string rootUrl { get; set; }
  }
}
