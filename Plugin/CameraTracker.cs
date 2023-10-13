using Rhino.Display;
using System;

namespace Plugin
{
    public class CameraTracker : DisplayConduit, IDisposable
    {
        private CameraState m_state;

        public event Action<CameraState> OnCameraStateChanged;

        public CameraTracker()
        {
            Enabled = true;
        }

        public void Dispose()
        {
            Enabled = false;
        }

        protected override void PostDrawObjects(DrawEventArgs e)
        {
            if (e.Viewport.Id == e.RhinoDoc.Views.ActiveView.ActiveViewportID)
            {
                CameraState newState = new CameraState(e.Viewport);
                if (!newState.Equals(m_state))
                {
                    m_state = newState;
                    OnCameraStateChanged?.Invoke(m_state);
                }
            }
        }
    }
}
