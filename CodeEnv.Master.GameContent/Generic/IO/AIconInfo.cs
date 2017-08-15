// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIconInfo.cs
// Immutable abstract base class holding info for Icons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable abstract base class holding info for Icons.
    /// <remarks>Implements value-based Equality and HashCode.</remarks>
    /// <remarks>Not currently used. Deferred waiting for use case where needed. Only current use of this "IconInfo"
    /// approach is IconInfo which is currently a struct.</remarks>
    /// </summary>
    public abstract class AIconInfo {

        private const string DebugNameFormat = "{0}[Filename: {1}, AtlasID: {2}, Color: {3}]";

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(AIconInfo left, AIconInfo right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(AIconInfo left, AIconInfo right) {
            return !(left == right);
        }

        #endregion

        public virtual string DebugName {
            get {
                return DebugNameFormat.Inject(GetType().Name, Filename, AtlasID.GetValueName(), Color.GetValueName());
            }
        }

        public string Filename { get; private set; }

        public AtlasID AtlasID { get; private set; }

        public GameColor Color { get; private set; }

        public AIconInfo(string filename, AtlasID atlasID, GameColor color) {
            Filename = filename;
            AtlasID = atlasID;
            Color = color;
        }

        #region Object.Equals and GetHashCode Override

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
                return hash;
            }
        }

        public override bool Equals(object obj) {
            if (obj == null) { return false; }
            if (ReferenceEquals(obj, this)) { return true; }
            if (obj.GetType() != GetType()) { return false; }

            AIconInfo oInfo = (AIconInfo)obj;
            return oInfo.Filename == Filename && oInfo.AtlasID == AtlasID && oInfo.Color == Color;
        }

        #endregion

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

