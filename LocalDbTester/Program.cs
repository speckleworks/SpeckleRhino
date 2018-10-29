using System;
using SpeckleCore;

namespace LocalDbTester
{
  class Program
  {
    static void Main( string[ ] args )
    {
      Console.WriteLine( "Hello World!" );
      SpeckleLocalContext.Init();

      var x = SpeckleLocalContext.GetAccountsByRestApi( "asdfa" );

      Console.WriteLine( x.Count );

      Console.WriteLine( "Press enter to close..." );
      Console.ReadLine();
    }
  }
}
