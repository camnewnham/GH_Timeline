using Grasshopper.Kernel;
using Newtonsoft.Json;
using Rhino.Display;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GH_Timeline
{
    /// <summary>
    /// A sequence of keyframes that stores component keyframes
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class ComponentSequence : Sequence
    {
        [JsonProperty("id")]
        private Guid m_instanceGuid;

        /// <inheritdoc/>
        public override string IsValidWhyNot => TryGetDocumentObject(out _) ? null : $"Component not found with InstanceGuid {m_instanceGuid}";

        /// <inheritdoc/>
        public override float Sort => (m_ghObject?.Attributes.Pivot.Y ?? 0) + 10000;

        private GH_Document m_document;

        /// <summary>
        /// Gets the current document. When set, attempt to find the component matching <see cref="m_instanceGuid"/> to assign <see cref="DocumentObject"/>
        /// </summary>
        public GH_Document Document
        {
            get => m_document;
            internal set
            {
                if (value != m_document)
                {
                    m_document = value;
                    m_ghObject = value?.FindObject(InstanceGuid, true);
                }
            }
        }

        /// <summary>
        /// Gets the document object corresponding to this sequence. Throws an exception if not found.
        /// </summary>
        public IGH_DocumentObject DocumentObject => TryGetDocumentObject(out IGH_DocumentObject found)
                    ? found : throw new KeyNotFoundException($"Unable to find document object ({InstanceGuid})");

        /// <summary>
        /// Attempts to retrieve the document object corresponding to this sequence
        /// </summary>
        /// <param name="obj">The object</param>
        /// <returns>True if the object was found</returns>
        public bool TryGetDocumentObject(out IGH_DocumentObject obj)
        {
            if (m_ghObject != null)
            {
                obj = m_ghObject;
                return true;
            }
            m_ghObject = obj = Document?.FindObject(InstanceGuid, true);
            return obj != null;
        }

        /// <inheritdoc/>
        public override string Name => m_ghObject?.GetName() ?? "Missing";

        /// <summary>
        /// Hashset of the last state. The component corresponding to this sequence will
        /// only be expired if the hashcode changes.
        /// </summary>
        private int m_lastStateHashCode = -1;

        private IGH_DocumentObject m_ghObject;

        /// <summary>
        /// Gets or sets the instance GUID of the component corresponding to this sequence
        /// </summary>
        public Guid InstanceGuid
        {
            get => m_instanceGuid;
            set
            {
                if (value != m_instanceGuid)
                {
                    m_instanceGuid = value;
                    m_ghObject = null;
                }
            }
        }

        public ComponentSequence(Guid instanceGuid, GH_Document doc)
        {
            InstanceGuid = instanceGuid;
            Document = doc;
        }

        /// <summary>
        /// Handles interpolation of keyframes in this sequence, expiring components as required
        /// </summary>
        /// <param name="time">The time to set</param>
        /// <param name="doc">The document to apply the time change to</param>
        /// <returns>True if setting the time resulted in a change and expired the component (that should prompt component and document expiry)</returns>
        public override bool SetTime(double time, GH_Document doc, RhinoViewport viewport)
        {
            Document = doc;

            if (IsValidWhyNot != null)
            {
                return false;
            }

            List<Keyframe> kfs = Keyframes.ToList();

            int oldHash = m_lastStateHashCode;
            for (int i = 0; i < kfs.Count; i++)
            {
                ComponentKeyframe current = kfs[i] as ComponentKeyframe;
                ComponentKeyframe next = i < kfs.Count - 1 ? kfs[i + 1] as ComponentKeyframe : null;

                if (next == null // Past last frame or only one keyframe
                    || (i == 0 && current.Time >= time))   // On or before first frame
                {
                    m_lastStateHashCode = current.LoadState(DocumentObject);
                    break;
                }
                else if (time >= current.Time && time < next.Time) // Between this frame and next 
                {
                    double fraction = MathUtils.Remap(time, current.Time, next.Time, 0, 1);
                    m_lastStateHashCode = current.InterpolateState(DocumentObject, next, fraction);
                    break;
                }
            }

            if (oldHash != m_lastStateHashCode)
            {
                if (DocumentObject is IGH_ActiveObject activeObj && activeObj.Phase != GH_SolutionPhase.Blank)
                {
                    DocumentObject.ExpireSolution(false);
                }
            }

            return oldHash != m_lastStateHashCode;
        }
    }
}
