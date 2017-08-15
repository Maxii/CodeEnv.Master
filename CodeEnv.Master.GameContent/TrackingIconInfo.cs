// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: TrackingIconInfo.cs
// Immutable class containing info needed to construct TrackingIcons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Immutable class containing info needed to construct TrackingIcons.
    /// </summary>
    public class TrackingIconInfo : AIconInfo {

        private const string DebugNameFormat = "{0}[Filename: {1}, AtlasID: {2}, Color: {3}, Placement: {4}, Size: {5}, Layer: {6}]";

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(TrackingIconInfo left, TrackingIconInfo right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(TrackingIconInfo left, TrackingIconInfo right) {
            return !(left == right);
        }

        #endregion

        public override string DebugName {
            get {
                return DebugNameFormat.Inject(GetType().Name, Filename, AtlasID.GetValueName(), Color.GetValueName(), Placement.GetValueName(),
                Size, Layer.GetValueName());
            }
        }

        public Vector2 Size { get; private set; }

        public WidgetPlacement Placement { get; private set; }

        public Layers Layer { get; private set; }

        public TrackingIconInfo(string filename, AtlasID atlasID, GameColor color, Vector2 size, WidgetPlacement placement, Layers layer)
            : base(filename, atlasID, color) {
            Size = size;
            Placement = placement;
            Layer = layer;
        }

        #region Object.Equals and GetHashCode Override

        public override int GetHashCode() {
            unchecked {
                int hash = base.GetHashCode();
                hash = hash * 31 + Size.GetHashCode();
                hash = hash * 31 + Placement.GetHashCode();
                hash = hash * 31 + Layer.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) {
            if (base.Equals(obj)) {
                TrackingIconInfo oInfo = (TrackingIconInfo)obj;
                return oInfo.Size == Size && oInfo.Placement == Placement && oInfo.Layer == Layer;
            }
            return false;
        }

        #endregion

    }
}

