using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace Plugin
{
    /// <summary>
    /// Utility for forwarding mouse events to <see cref="InputForwarder"/>.  
    /// Specify <see cref="InputHandlers"/>, then forward events from <see cref="IGH_ResponsiveObject"/> to this.
    /// </summary>
    public class InputForwarder
    {
        private InputHandler m_capturedHandler;

        public IEnumerable<InputHandler> InputHandlers;

        private IEnumerable<InputHandler> GetHandlers(PointF location)
        {
            if (m_capturedHandler != null)
            {
                yield return m_capturedHandler;
                yield break;
            }
            foreach (InputHandler handler in InputHandlers)
            {
                foreach (InputHandler sub in TraverseHandlers(handler, location))
                {
                    yield return sub;
                }
            }
        }

        private IEnumerable<InputHandler> TraverseHandlers(InputHandler handler, PointF location)
        {
            if (handler.Bounds.Contains(location))
            {
                yield return handler;

                foreach (InputHandler child in handler.InputHandlers())
                {
                    foreach (InputHandler sub in TraverseHandlers(child, location))
                    {
                        yield return sub;
                    }
                }
            }
        }

        public GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            foreach (InputHandler responder in GetHandlers(e.CanvasLocation))
            {
                GH_ObjectResponse response = responder.RespondToMouseUp(sender, e);
                switch (response)
                {
                    case GH_ObjectResponse.Ignore:
                        continue;
                    case GH_ObjectResponse.Release:
                        if (responder != m_capturedHandler)
                        {
                            throw new InvalidOperationException("A captured input response was released but it was not the current responder.");
                        }
                        m_capturedHandler = null;
                        return response;
                    case GH_ObjectResponse.Handled:
                        return response;
                    case GH_ObjectResponse.Capture:
                        m_capturedHandler = responder;
                        return GH_ObjectResponse.Capture;
                }
            }
            return GH_ObjectResponse.Ignore;
        }


        public GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            foreach (InputHandler responder in GetHandlers(e.CanvasLocation))
            {
                GH_ObjectResponse response = responder.RespondToMouseDown(sender, e);
                switch (response)
                {
                    case GH_ObjectResponse.Ignore:
                        continue;
                    case GH_ObjectResponse.Release:
                        if (responder != m_capturedHandler)
                        {
                            throw new InvalidOperationException("A captured input response was released but it was not the current responder.");
                        }
                        m_capturedHandler = null;
                        return response;
                    case GH_ObjectResponse.Handled:
                        return response;
                    case GH_ObjectResponse.Capture:
                        m_capturedHandler = responder;
                        return GH_ObjectResponse.Capture;
                }
            }
            return GH_ObjectResponse.Ignore;
        }


        public GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            foreach (InputHandler responder in GetHandlers(e.CanvasLocation))
            {
                GH_ObjectResponse response = responder.RespondToMouseMove(sender, e);
                switch (response)
                {
                    case GH_ObjectResponse.Ignore:
                        continue;
                    case GH_ObjectResponse.Release:
                        if (responder != m_capturedHandler)
                        {
                            throw new InvalidOperationException("A captured input response was released but it was not the current responder.");
                        }
                        m_capturedHandler = null;
                        return response;
                    case GH_ObjectResponse.Handled:
                        return response;
                    case GH_ObjectResponse.Capture:
                        m_capturedHandler = responder;
                        return GH_ObjectResponse.Capture;
                }
            }
            return GH_ObjectResponse.Ignore;
        }


        public GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e)
        {
            foreach (InputHandler responder in GetHandlers(e.CanvasLocation))
            {
                GH_ObjectResponse response = responder.RespondToMouseDoubleClick(sender, e);
                switch (response)
                {
                    case GH_ObjectResponse.Ignore:
                        break;
                    case GH_ObjectResponse.Release:
                        if (responder != m_capturedHandler)
                        {
                            throw new InvalidOperationException("A captured input response was released but it was not the current responder.");
                        }
                        m_capturedHandler = null;
                        return response;
                    case GH_ObjectResponse.Handled:
                        return response;
                    case GH_ObjectResponse.Capture:
                        m_capturedHandler = responder;
                        return GH_ObjectResponse.Capture;
                }
            }
            return GH_ObjectResponse.Ignore;
        }
    }

    /// <summary>
    /// Implement this interface to handle events generated by a <see cref="InputForwarder"/>
    /// </summary>
    public interface InputHandler
    {
        /// <summary>
        /// Any child input handlers to be traversed after this one.
        /// </summary>
        /// <returns>Enumerate over the children.</returns>
        IEnumerable<InputHandler> InputHandlers();
        /// <summary>
        /// The bounds to determine whether input should be processed in this component
        /// </summary>
        RectangleF Bounds { get; }
        GH_ObjectResponse RespondToMouseUp(GH_Canvas sender, GH_CanvasMouseEvent e);
        GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e);
        GH_ObjectResponse RespondToMouseMove(GH_Canvas sender, GH_CanvasMouseEvent e);
        GH_ObjectResponse RespondToMouseDoubleClick(GH_Canvas sender, GH_CanvasMouseEvent e);
    }
}
