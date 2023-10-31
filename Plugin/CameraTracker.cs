using Rhino.Display;
using System;
using System.Drawing;

namespace GH_Timeline
{
    public class CameraTracker : DisplayConduit, IDisposable
    {
        private CameraState m_state;

        public event Action<CameraState> OnCameraStateChanged;

        private readonly TimelineComponent m_owner;

        public CameraTracker(TimelineComponent owner)
        {
            Enabled = true;
            m_owner = owner;
        }

        public void Dispose()
        {
            Enabled = false;
        }

        protected override void DrawOverlay(DrawEventArgs e)
        {
            base.DrawOverlay(e);

            if (m_owner.Recording && m_owner.AnimateCamera)
            {
                e.Display.Draw2dRectangle(e.Viewport.Bounds, Color.Red, 5, Color.Transparent);
            }
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            if (e.Viewport.Id == e.RhinoDoc.Views.ActiveView.ActiveViewportID)
            {
                CameraState newState = new CameraState(e.Viewport);
                if (!newState.Equals(m_state))
                {
                    OnCameraStateChanged?.Invoke(m_state);
                }
                m_state = newState;
            }
        }
    }
}
