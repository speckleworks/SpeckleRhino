using Rhino.DocObjects;
using Rhino.Geometry;
using SpeckleCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SpeckleRhino
{
    /// <summary>
    /// Generalises some methhods for both senders and receivers.
    /// </summary>
    public interface ISpeckleRhinoClient : IDisposable, ISerializable
    {
        SpeckleCore.ClientRole GetRole();

        string GetClientId();

        void TogglePaused(bool status);

        void ToggleVisibility(bool status);

        void ToggleLayerVisibility(string layerId, bool status);

        void ToggleLayerHover(string layerId, bool status);

        void Dispose(bool delete = false);
    }
}
