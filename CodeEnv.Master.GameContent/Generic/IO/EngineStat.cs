// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EngineStat.cs
// Immutable stat for a ship's engine.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat for a ship's engine.
    /// </summary>
    public class EngineStat : AEquipmentStat {

        /// <summary>
        /// The maximum power the STL engines can put out. FullStlSpeed = FullStlPower / (Mass * rigidbody.drag).
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in
        /// realtime to a Unity seconds value in EngineRoom.ApplyThrust() using GeneralSettings.HoursPerSecond.
        /// </summary>
        public float FullStlPower { get; private set; }

        /// <summary>
        /// The maximum power the FTL engines can put out. FullFtlSpeed = FullFtlPower / (Mass * rigidbody.drag).
        /// NOTE: This value uses a Game Hour denominator. It is adjusted in
        /// realtime to a Unity seconds value in EngineRoom.ApplyThrust() using GeneralSettings.HoursPerSecond.
        /// </summary>
        public float FullFtlPower { get; private set; }

        public float MaxTurnRate { get; private set; }  // IMPROVE replace with LateralThrust and calc maxTurnRate using mass

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineStat" /> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="fullStlPower">The maximum power the STL engines can put out.</param>
        /// <param name="fullFtlPower">The maximum power the FTL engines can put out.</param>
        /// <param name="maxTurnRate">The maximum turn rate.</param>
        /// <param name="size">The physical size of the equipment.</param>
        /// <param name="mass">The mass.</param>
        /// <param name="pwrRqmt">The power required to operate the equipment.</param>
        /// <param name="expense">The expense.</param>
        public EngineStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float fullStlPower, float fullFtlPower,
            float maxTurnRate, float size, float mass, float pwrRqmt, float expense)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense) {
            FullStlPower = fullStlPower;
            FullFtlPower = fullStlPower;
            MaxTurnRate = maxTurnRate;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

