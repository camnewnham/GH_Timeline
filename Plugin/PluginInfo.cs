using Grasshopper;
using Grasshopper.Kernel;
using System;
using System.Drawing;

namespace Plugin
{
    public class PluginInfo : GH_AssemblyInfo
    {
        public override string Name => "Plugin";

        //Return a 24x24 pixel bitmap to represent this GHA library.
        public override Bitmap Icon => null;

        //Return a short string describing the purpose of this GHA library.
        public override string Description => "";

        public override Guid Id => new Guid("bc2a4860-5fd1-4339-a14b-8188b81b8547");

        //Return a string identifying you or your company.
        public override string AuthorName => "";

        //Return a string representing your preferred contact details.
        public override string AuthorContact => "";
    }
}