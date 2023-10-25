using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.IO;

namespace Plugin
{
    public class PluginInfo : GH_AssemblyInfo
    {
        /// <summary>
        /// Working folder for caching things.
        /// </summary>
        public static string WorkingFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TimelineForGrasshopper");
        public override string Name => "Plugin";
        public override Bitmap Icon => null;
        public override string Description => "";
        public override Guid Id => new Guid("bc2a4860-5fd1-4339-a14b-8188b81b8547");
        public override string AuthorName => "";
        public override string AuthorContact => "";
    }
}