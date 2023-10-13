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

        protected override void DrawOverlay(DrawEventArgs e)
        {
            CameraState newState = CameraState.CreateFromViewport(e.Viewport);
            if (!newState.Equals(m_state))
            {
                m_state = newState;
                OnCameraStateChanged?.Invoke(m_state);
            }
        }
    }
}
