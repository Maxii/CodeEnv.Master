// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AEquipmentStat.cs
// Immutable abstract base class for Equipment stats.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable abstract base class for Equipment stats.
    /// </summary>
    public abstract class AEquipmentStat {

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
        /// Display name of the equipment.
        /// </summary>
        public string Name { get; private set; }

        public AtlasID ImageAtlasID { get; private set; }

        public string ImageFilename { get; private set; }

        public string Description { get; private set; }

        /// <summary>
        /// The physical space this equipment requires or, in the case of a hull,
        /// the physical space provided.
        /// </summary>
        public float Size { get; private set; }

        public float Mass { get; private set; }

        public float PowerRequirement { get; private set; }

        public float Expense { get; private set; }

        public bool IsDamageable { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AEquipmentStat" /> class.
        /// </summary>
        /// <param name="name">The display name of the Equipment.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="size">The physical size of the equipment.</param>
        /// <param name="mass">The mass of the equipment.</param>
        /// <param name="pwrRqmt">The power required to operate the equipment.</param>
        /// <param name="expense">The expense required to operate this equipment.</param>
        /// <param name="isDamageable">if set to <c>true</c> [is damageable].</param>
        public AEquipmentStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass, float pwrRqmt,
            float expense, bool isDamageable) {
            Name = name;
            ImageAtlasID = imageAtlasID;
            ImageFilename = imageFilename;
            Description = description;
            Size = size;
            Mass = mass;
            PowerRequirement = pwrRqmt;
            Expense = expense;
            IsDamageable = isDamageable;
        }

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

