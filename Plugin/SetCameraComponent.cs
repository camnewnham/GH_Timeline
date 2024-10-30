using Grasshopper.Kernel;
using Rhino.Geometry;
using System;

namespace GH_Timeline
{
    public class SetCameraComponent : GH_Component
    {
        public override Guid ComponentGuid => new Guid("EEE94A1F-A762-425C-B564-5F5282580951");
        protected override System.Drawing.Bitmap Icon => Properties.Resources.icon_setcamera_24;

        public SetCameraComponent()
          : base("Set Camera", "Set Camera", "Sets the camera location and target", "Display", "Timeline")
        {
        }

        protected override void RegisterInputParams(GH_InputParamManager pManager)
        {
            pManager.AddPointParameter("Camera Location", "L", "The location of the camera", GH_ParamAccess.item);
            pManager.AddPointParameter("Camera Target", "T", "The target of the camera", GH_ParamAccess.item);
            pManager[pManager.AddTextParameter("Viewport", "V", "The name of the viewport. If none, the active viewport will be used", GH_ParamAccess.item)].Optional = true;
        }

        protected override void RegisterOutputParams(GH_OutputParamManager pManager)
        {
        }

        protected override void SolveInstance(IGH_DataAccess DA)
        {
            Rhino.Display.RhinoViewport viewport = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView.ActiveViewport;
            string viewportName = viewport.Name;


            if (DA.GetData(2, ref viewportName))
            {
                Rhino.Display.RhinoView rhView = Rhino.RhinoDoc.ActiveDoc.Views.Find(viewportName, false);
                if (rhView == null)
                {
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Error, $"Unable to find viewport {viewportName}");
                    return;
                }

                viewport = rhView.ActiveViewport;
            }

            Point3d camPos = default;
            Point3d camTar = default;

            DA.GetData(0, ref camPos);
            DA.GetData(1, ref camTar);

            viewport.SetCameraLocations(camTar, camPos);
        }
    }
}