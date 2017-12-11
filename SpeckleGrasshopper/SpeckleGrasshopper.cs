using System;
using System.Drawing;
using Grasshopper.Kernel;

namespace SpeckleAbstract
{
    public class MyProject7Info : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "SocketTest";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "";
            }
        }
        public override Guid Id
        {
            get
            {
                return new Guid("97b51837-30c6-4f23-942d-2b8eb73c6f6f");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "";
            }
        }
    }
}
