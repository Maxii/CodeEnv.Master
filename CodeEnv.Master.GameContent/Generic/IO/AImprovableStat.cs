// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ATechStat.cs
// An immutable abstract base class for stats that can be improved upon.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An immutable abstract base class for stats that can be improved upon.
    /// </summary>
    public abstract class AImprovableStat {

        private const string DebugNameFormat = "{0}.{1}";

        protected string _debugName;
        public virtual string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(Name, GetType().Name);
                }
                return _debugName;
            }
        }

        /// <summary>
        /// Display name of the stat.
        /// </summary>
        public string Name { get; private set; }

        public AtlasID ImageAtlasID { get; private set; }

        public string ImageFilename { get; private set; }

        public string Description { get; private set; }

        /// <summary>
        /// The improvement level of this stat.
        /// </summary>
        public Level Level { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AImprovableStat" /> class.
        /// </summary>
        /// <param name="name">The display name of the Equipment.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="level">The improvement level of this stat.</param>
        public AImprovableStat(string name, AtlasID imageAtlasID, string imageFilename, string description, Level level) {
            Name = name;
            ImageAtlasID = imageAtlasID;
            ImageFilename = imageFilename;
            Description = description;
            D.AssertNotDefault((int)level);
            Level = level;
        }

        public sealed override string ToString() {
            return DebugName;
        }

        #region Value-based Equality Archive
        // 2.23.18 ATechStat instances are always the same as they are acquired via factory caching

        ////public static bool operator ==(ATechStat left, ATechStat right) {
        ////    // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
        ////    if (ReferenceEquals(left, right)) { return true; }
        ////    if (((object)left == null) || ((object)right == null)) { return false; }
        ////    return left.Equals(right);
        ////}

        ////public static bool operator !=(ATechStat left, ATechStat right) {
        ////    return !(left == right);
        ////}

        ////public override int GetHashCode() {
        ////    unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
        ////        int hash = 17;  // 17 = some prime number
        ////        hash = hash * 31 + Name.GetHashCode(); // 31 = another prime number
        ////        hash = hash * 31 + ImageAtlasID.GetHashCode();
        ////        hash = hash * 31 + ImageFilename.GetHashCode();
        ////        hash = hash * 31 + Description.GetHashCode();
        ////        hash = hash * 31 + Level.GetHashCode();
        ////        return hash;
        ////    }
        ////}

        ////public override bool Equals(object obj) {
        ////    if (obj == null) { return false; }
        ////    if (ReferenceEquals(obj, this)) { return true; }
        ////    if (obj.GetType() != GetType()) { return false; }

        ////    ATechStat objStat = (ATechStat)obj;
        ////    return objStat.Name == Name && objStat.ImageAtlasID == ImageAtlasID
        ////        && objStat.ImageFilename == ImageFilename && objStat.Description == Description && objStat.Level == Level;
        ////}

        #endregion


    }
}

