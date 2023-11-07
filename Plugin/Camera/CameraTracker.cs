using Rhino.Display;
using System;
using System.Drawing;

namespace GH_Timeline
{
    /// <summary>
    /// Display conduit used to detect camera changes
    /// </summary>
    public class CameraTracker : DisplayConduit, IDisposable
    {
        /// <summary>
        /// The last known state
        /// </summary>
        private CameraState m_state;

        /// <summary>
        /// Raised when the camera state changes
        /// </summary>
        public event Action<CameraState> OnCameraStateChanged;

        /// <summary>
        /// The component that this tracker corresponds to.
        /// </summary>
        private readonly TimelineComponent m_owner;

        /// <summary>
        /// Create a conduit and start tracking for a component
        /// </summary>
        /// <param name="owner"></param>
        public CameraTracker(TimelineComponent owner)
        {
            Enabled = true;
            m_owner = owner;
        }

        /// <summary>
        /// Detach the display conduit
        /// </summary>
        public void Dispose()
        {
            Enabled = false;
        }

        /// <summary>
        /// Render an overlay denoting whether we are recording state for a timeline.
        /// </summary>
        protected override void DrawOverlay(DrawEventArgs e)
        {
            base.DrawOverlay(e);

            if (m_owner.Recording && m_owner.AnimateCamera)
            {
                e.Display.Draw2dRectangle(e.Viewport.Bounds, Color.Red, 5, Color.Transparent);
            }
        }

        /// <summary>
        /// Detect whether the camera has changed and raise the <see cref="OnCameraStateChanged"/> event.
        /// </summary>
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
