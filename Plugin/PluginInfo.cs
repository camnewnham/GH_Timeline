using Grasshopper.Kernel;
using System;
using System.Drawing;
using System.IO;

namespace GH_Timeline
{
    public class PluginInfo : GH_AssemblyInfo
    {
        /// <summary>
        /// Working folder for caching things.
        /// </summary>
        public static string WorkingFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TimelineForGrasshopper");
        public override string Name => "Timeline";
        public override Bitmap Icon => Properties.Resources.logo_24;
        public override string Description => "Simple keyframe sequencer for Grasshopper";
        public override Guid Id => new Guid("bc2a4860-5fd1-4339-a14b-8188b81b8547");
        public override string AuthorName => "Cameron Newnham";
        public override string AuthorContact => "https://github.com/camnewnham/GH_Timeline";
    }
}