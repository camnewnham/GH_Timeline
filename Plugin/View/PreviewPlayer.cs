using Grasshopper;
using System;

namespace GH_Timeline
{
    /// <summary>
    /// Detects key presses and toggles playback of the timeline preview when the Tab key is pressed.
    /// </summary>
    internal class PreviewPlayer : IDisposable
    {
        private TimelineComponent Component;
        private bool isPlaying = false;

        /// <summary>
        /// Initialization should occur when the component is added to the document.
        /// </summary>
        public PreviewPlayer(TimelineComponent component)
        {
            Component = component;
            Instances.ActiveCanvas.KeyDown += OnKeyDown;
        }

        /// <summary>
        /// Detect keydown on the active canvas.
        /// Note this is implemented as GH_Attributes.RespondToKeyDown does not get called at the expected times.
        /// </summary>
        private void OnKeyDown(object sender, System.Windows.Forms.KeyEventArgs e)
        {
            if (
                e.KeyCode == System.Windows.Forms.Keys.Tab &&
                Component.Attributes.Selected &&
                Instances.ActiveCanvas.Document.SelectedCount == 1 &&
                Instances.ActiveCanvas?.Document == Component.OnPingDocument()
                )
            {
                if (isPlaying)
                {
                    StopPlaying();
                }
                else
                {
                    StartPlaying();
                }
                e.Handled = true;
            }
        }

        private void StopPlaying()
        {
            isPlaying = false;
        }

        private void StopPlaying(object sender, EventArgs e)
        {
            StopPlaying();
        }

        private void StartPlaying()
        {
            if (isPlaying) return;
            if (Component.CurrentValue >= 1)
            {
                Component.SetSliderValue(0);
                Component.ExpireSolution(true);
                Rhino.RhinoApp.Wait();
            }
            isPlaying = true;
            Rhino.RhinoApp.EscapeKeyPressed += StopPlaying;
            while (isPlaying)
            {
                Component.SetSliderValue(Math.Min(1, (decimal)((float)Component.CurrentValue + 1f / Component.Timeline.FrameCount)));
                Component.ExpireSolution(true);
                if (Component.CurrentValue >= 1)
                {
                    StopPlaying();
                }
                else
                {
                    Rhino.RhinoApp.Wait();
                }
            }

            isPlaying = false;
            Rhino.RhinoApp.EscapeKeyPressed -= StopPlaying;
        }


        /// <summary>
        /// Disposal should occur when the component is removed from the document.
        /// </summary>
        public void Dispose()
        {
            Instances.ActiveCanvas.KeyDown -= OnKeyDown;
        }
    }
}
