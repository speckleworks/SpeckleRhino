using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nito.AsyncEx;
using SpeckleCore;

namespace SpeckleCoreApiTester
{
  class Program
  {
    static void Main( string[ ] args )
    {
      Console.WriteLine( "Init async context..." );

      AsyncContext.Run( ( ) => MainAsync( args ) );
    }

    static async void MainAsync( string[ ] args )
    {
      Console.WriteLine( "Done." );

      Console.WriteLine();
      Console.WriteLine( "Setting up client." );

      SpeckleApiClient myClient = new SpeckleApiClient();
      myClient.BaseUrl = "http://10.211.55.2:3000/api/v1";

      var authToken = "Run test accounts to set a proper auth token";

      myClient.AuthToken = authToken;

      await TestAccounts( myClient );

      //await TestProjects( myClient );

      //await TestClients( myClient );

      //await TestComments( myClient );

      await TestStreams( myClient );

      //await TestObjects( myClient );

      Console.WriteLine();
      Console.WriteLine( "Press any key to close" );
      Console.ReadLine();
    }

    static async Task TestObjects( SpeckleApiClient myClient )
    {
      var myPoint = new SpecklePoint() { Value = new List<double>() { 1, 2, 3 } };
      var mySecondPoint = new SpecklePoint() { Value = new List<double>() { 23, 33, 12 } };
      var myCircle = new SpeckleCircle() { Radius = 21, Normal = new SpeckleVector() { Value = new List<double>() { 1, 2, 2 } } };
      var myPlane = new SpecklePlane() { Origin = new SpecklePoint() { Value = new List<double>() { 12, 12, 12 } }, Normal = myCircle.Normal };
      var myArc = new SpeckleArc() { Radius = 2, AngleRadians = 2.1, EndAngle = 1, StartAngle = 0 };

      myCircle.Properties = new Dictionary<string, object>();
      myCircle.Properties.Add( "a  property", "Hello!" );
      myCircle.Properties.Add( "point", myPoint );


      List<SpeckleObject> myList = new List<SpeckleObject>();

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Creating some objects." );
        var Response = await myClient.ObjectCreateAsync( new List<SpeckleObject>() { myPoint, myCircle, myArc, myPlane } );
        Console.WriteLine( "OK: Saved " + Response.Resources.Count + " objects" );
        myList = Response.Resources;
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Updating an object" );
        var Response = await myClient.ObjectUpdateAsync( myList[ 1 ]._id, new SpeckleCircle() { Radius = 42 } );
        Console.WriteLine( "OK: Saved " + Response.Message );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Updating an object's properties" );
        var Response = await myClient.ObjectUpdatePropertiesAsync( myList[ 1 ]._id, new { hello = "World", max = 3.14 } );
        Console.WriteLine( "OK: Saved " + Response.Message );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Getting an object" );
        var Response = await myClient.ObjectGetAsync( myList[ 1 ]._id );
        Console.WriteLine( "OK: Got " + Response.Resource.ToJson() );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Getting objects in bulk" );
        var Response = await myClient.ObjectGetBulkAsync( new string[ ] { myList[ 1 ]._id, myList[ 0 ]._id, myList[ 2 ]._id }, "fields=properties,radius" );
        Console.WriteLine( "OK: Got " + Response.Resources.Count );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Deleting an object" );
        var Response = await myClient.ObjectDeleteAsync( myList[ 0 ]._id );
        Console.WriteLine( "OK: Got " + Response.Message );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }


    }

    static async Task TestStreams( SpeckleApiClient myClient )
    {
      string streamId = "lol";
      string secondStreamId = "hai";

      var myPoint = new SpecklePoint() { Value = new List<double>() { 1, 2, 3 } };
      var mySecondPoint = new SpecklePoint() { Value = new List<double>() { 23, 33, 12 } };
      var myCircle = new SpeckleCircle() { Radius = 21, Normal = new SpeckleVector() { Value = new List<double>() { 1, 2, 2 } } };


      myPoint.Properties = new Dictionary<string, object>();
      myPoint.Properties.Add( "Really", mySecondPoint );

      myCircle.Properties = new Dictionary<string, object>();
      myCircle.Properties.Add( "a property", "Hello!" );
      myCircle.Properties.Add( "point", myPoint );

      SpeckleStream myStream = new SpeckleStream()
      {
        Name = "Hello World My Little Stream",
        Objects = new List<SpeckleObject>() { myCircle, myPoint }
      };

      SpeckleStream secondStream = new SpeckleStream()
      {
        Name = "Second Little Stream",
        Objects = new List<SpeckleObject>() { myCircle, mySecondPoint }
      };

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Creating a stream." );
        var Response = await myClient.StreamCreateAsync( myStream );
        Console.WriteLine( "OK: " + Response.Resource.ToJson() );

        streamId = Response.Resource.StreamId;
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Creating a second stream." );
        var Response = await myClient.StreamCreateAsync( secondStream );
        Console.WriteLine( "OK: " + Response.Resource.ToJson() );

        secondStreamId = Response.Resource.StreamId;
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }


      Console.WriteLine();
      try
      {
        Console.WriteLine( "Diffing two streams!" );
        var Response = await myClient.StreamDiffAsync( streamId, secondStreamId );
        Console.WriteLine( "OK: " + Response.Message );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Getting a stream." );
        var Response = await myClient.StreamGetAsync( streamId, null );
        Console.WriteLine( "OK: " + Response.Resource.ToJson() );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Getting a stream's objects." );
        var Response = await myClient.StreamGetObjectsAsync( streamId, null );
        Console.WriteLine( "OK: " + Response.Resources.Count );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Updating a stream." );
        var Response = await myClient.StreamUpdateAsync( streamId, new SpeckleStream() { Name = "I hate api testing", ViewerLayers = new List<object>() { new { test = "test" } } } );
        Console.WriteLine( "OK: " + Response.Message );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Getting a stream field." );
        var Response = await myClient.StreamGetAsync( streamId, "fields=viewerLayers,name,owner" );
        Console.WriteLine( "OK: " + Response.Resource.ToJson() );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Getting all users's streams." );
        var Response = await myClient.StreamsGetAllAsync();
        Console.WriteLine( "OK: " + Response.Resources.Count );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }


      Console.WriteLine();
      try
      {
        Console.WriteLine( "Cloning a stream." );
        var Response = await myClient.StreamCloneAsync( streamId );
        Console.WriteLine( "OK: " + Response.Message );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Deleting a stream: " + streamId );
        var Response = await myClient.StreamDeleteAsync( streamId );
        Console.WriteLine( "OK: " + Response.Message );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

    }

    static async Task TestComments( SpeckleApiClient myClient )
    {

      string commentId = "lol", secondCommentId = "lol";

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Creating a comment on a stream." );
        var Response = await myClient.CommentCreateAsync( ResourceType.Stream, "SJY4LnGqz", new Comment() { Text = "Hello World!", Labels = new List<string>() { "hai", "urgent" } } );
        Console.WriteLine( "OK: " + Response.Resource.ToJson() );

        commentId = Response.Resource._id;
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Creating a comment on a comment." );
        var Response = await myClient.CommentCreateAsync( ResourceType.Comment, commentId, new Comment() { Text = "Nested Hello World!", Labels = new List<string>() { "urgent" } } );
        Console.WriteLine( "OK: " + Response.Resource.ToJson() );

        secondCommentId = Response.Resource._id;
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Getting a comment." );
        var Response = await myClient.CommentGetAsync( secondCommentId );
        Console.WriteLine( "OK: " + Response.Resource.ToJson() );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Deleting a comment." );
        var Response = await myClient.CommentDeleteAsync( secondCommentId );
        Console.WriteLine( "OK: " + Response.Message );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }
    }

    static async Task TestClients( SpeckleApiClient myClient )
    {
      string clientId = "lol";
      Console.WriteLine();
      try
      {
        Console.WriteLine( "Creating a client." );
        var Response = await myClient.ClientCreateAsync( new AppClient() { DocumentGuid = "fakester", Role = "Sender", Online = false } );
        Console.WriteLine( "OK: " + Response.Resource.ToJson() );

        clientId = Response.Resource._id;
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Updating a client." );
        var Response = await myClient.ClientUpdateAsync( clientId, new AppClient() { Role = "Receiver", DocumentLocation = "C£aapppdata/x/xdfsdf.gh" } );
        Console.WriteLine( "OK: " + Response.Message );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Getting a client." );
        var Response = await myClient.ClientGetAsync( clientId );
        Console.WriteLine( "OK: " + Response.Resource.ToJson() );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Getting all users clients." );
        var Response = await myClient.ClientGetAllAsync();
        Console.WriteLine( "OK: " + Response.Resources.Count );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Deleteing  a client." );
        var Response = await myClient.ClientDeleteAsync( clientId );
        Console.WriteLine( "OK: " + Response.Message );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

    }

    static async Task TestProjects( SpeckleApiClient myClient )
    {
      string projectId = "lol";
      Console.WriteLine();
      try
      {
        Console.WriteLine( "Creating a project." );
        var Response = await myClient.ProjectCreateAsync( new Project() { Name = "A simple project" } );
        Console.WriteLine( "OK: " + Response.Resource.ToJson() );

        projectId = Response.Resource._id;
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Updating a project." );
        var Response = await myClient.ProjectUpdateAsync( projectId, new Project() { Name = "A more complicated project", Private = false } );
        Console.WriteLine( "OK: " + Response.Resource.ToJson() );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Getting a project." );
        var Response = await myClient.ProjectGetAsync( projectId );
        Console.WriteLine( "OK: " + Response.Resource.ToJson() );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Getting all users projects." );
        var Response = await myClient.ProjectGetAllAsync();
        Console.WriteLine( "OK: " + Response.Resources.Count );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      Console.WriteLine();
      try
      {
        Console.WriteLine( "Deleteing  a project." );
        var Response = await myClient.ProjectDeleteAsync( projectId );
        Console.WriteLine( "OK: " + Response.Message );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }


    }

    static async Task TestAccounts( SpeckleApiClient myClient )
    {
      Console.WriteLine();
      try
      {
        Console.WriteLine( "Logging in a user." );
        var Response = await myClient.UserLoginAsync( new User() { Email = "didi@improved.ro", Password = "redialtwice" } );
        Console.WriteLine( "OK Got user: " + Response.Resource.ToJson() );

        myClient.AuthToken = Response.Resource.Apitoken;
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      //Console.WriteLine();
      //Console.WriteLine( "Registering user." );
      //try
      //{
      //  var Response = await myClient.UserRegisterAsync( new User() { Name = "Dim", Email = DateTime.Now.ToString() + "email@testing.com", Password = "redialtwice" } );
      //  Console.WriteLine( "OK Results:: " + Response.Resource.ToJson() );
      //}
      //catch ( Exception e )
      //{
      //  Console.WriteLine( e.Message );
      //}

      Console.WriteLine();
      Console.WriteLine( "Searching for some users." );
      try
      {
        var Response = await myClient.UserSearchAsync( new User() { Email = "testing.com" } );
        Console.WriteLine( "OK Results:: " + Response.Resources.Count );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }


      Console.WriteLine();
      Console.WriteLine( "Getting profile." );
      try
      {
        var Response = await myClient.UserGetAsync();
        Console.WriteLine( "OK Results:: " + Response.Resource.Email );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }


      Console.WriteLine();
      Console.WriteLine( "Updating profile." );
      try
      {
        var Response = await myClient.UserUpdateProfileAsync( new User() { Company = "BARARARA" } );
        Console.WriteLine( "OK Results:: " + Response.Message );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }

      string userId = "lol";
      Console.WriteLine();
      Console.WriteLine( "Getting profile." );
      try
      {
        var Response = await myClient.UserGetAsync(); userId = Response.Resource._id;
        Console.WriteLine( "OK Results:: " + Response.Resource.Company );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }


      Console.WriteLine();
      Console.WriteLine( "Getting profile by id." );
      try
      {
        var Response = await myClient.UserGetProfileByIdAsync( userId );
        Console.WriteLine( "OK Results:: " + Response.Resource.Company );
      }
      catch ( Exception e )
      {
        Console.WriteLine( e.Message );
      }
    }

  }
}
