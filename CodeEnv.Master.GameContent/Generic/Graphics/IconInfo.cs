// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IconInfo.cs
// Immutable struct containing info needed to construct TrackingIcons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Immutable struct containing info needed to construct TrackingIcons.
    /// </summary>
    [Obsolete("Use TrackingIconInfo")]
    public struct IconInfo : IEquatable<IconInfo> {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(IconInfo left, IconInfo right) {
            return left.Equals(right);
        }

        public static bool operator !=(IconInfo left, IconInfo right) {
            return !left.Equals(right);
        }

        #endregion

        private const string DebugNameFormat = "{0}[Filename: {1}, AtlasID: {2}, Color: {3}, Placement: {4}, Size: {5}, Layer: {6}]";

        public string DebugName {
            get {
                return DebugNameFormat.Inject(GetType().Name, Filename, AtlasID.GetValueName(), Color.GetValueName(), Placement.GetValueName(),
                Size, Layer.GetValueName());
            }
        }

        public string Filename { get; private set; }

        public AtlasID AtlasID { get; private set; }

        public GameColor Color { get; private set; }

        public Vector2 Size { get; private set; }

        public WidgetPlacement Placement { get; private set; }

        public Layers Layer { get; private set; }

        public IconInfo(string filename, AtlasID atlasID, GameColor color, Vector2 size, WidgetPlacement placement, Layers layer)
            : this() {
            Filename = filename;
            AtlasID = atlasID;
            Color = color;
            Size = size;
            Placement = placement;
            Layer = layer;
        }

        #region Object.Equals and GetHashCode Override

        public override bool Equals(object obj) {
            if (!(obj is IconInfo)) { return false; }
            return Equals((IconInfo)obj);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// See "Page 254, C# 4.0 in a Nutshell."
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                int hash = 17;  // 17 = some prime number
                hash = hash * 31 + Filename.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + AtlasID.GetHashCode();
                hash = hash * 31 + Color.GetHashCode();
                hash = hash * 31 + Placement.GetHashCode();
                hash = hash * 31 + Size.GetHashCode();
                hash = hash * 31 + Layer.GetHashCode();
                return hash;
            }
        }

        #endregion

        public override string ToString() { return DebugName; }

        #region IEquatable<IconInfo> Members

        public bool Equals(IconInfo other) {
            return Filename == other.Filename && AtlasID == other.AtlasID && Color == other.Color && Placement == other.Placement
                && Size == other.Size && Layer == other.Layer;
        }

        #endregion
    }
}

