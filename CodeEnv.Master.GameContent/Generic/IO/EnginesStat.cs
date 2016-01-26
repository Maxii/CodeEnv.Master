// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: EnginesStat.cs
// Immutable stat for a ship's engine(s).
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable stat for a ship's engine(s).
    /// </summary>
    public class EnginesStat : AEquipmentStat {

        /// <summary>
        /// The maximum power the engine(s) can project as thrust when operating in STL mode. 
        /// <remarks>FullStlSpeed = FullStlPropulsionPower / (Mass * rigidbody.drag).
        /// This value uses a Game Hour denominator. It is adjusted in
        /// realtime to a Unity seconds value in EngineRoom.ApplyThrust() using GeneralSettings.HoursPerSecond.</remarks>
        /// </summary>
        public float FullStlPropulsionPower { get; private set; }

        /// <summary>
        /// The maximum power the engine(s) can project as thrust when operating in FTL mode. 
        /// <remarks>FullFtlSpeed = FullFtlPropulsionPower / (Mass * rigidbody.drag).
        /// This value uses a Game Hour denominator. It is adjusted in
        /// realtime to a Unity seconds value in EngineRoom.ApplyThrust() using GeneralSettings.HoursPerSecond.</remarks>
        /// </summary>
        public float FullFtlPropulsionPower { get; private set; }

        public float MaxTurnRate { get; private set; }  // IMPROVE replace with LateralThrust and calc maxTurnRate using mass

        /// <summary>
        /// Initializes a new instance of the <see cref="EngineStat"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="imageAtlasID">The image atlas identifier.</param>
        /// <param name="imageFilename">The image filename.</param>
        /// <param name="description">The description.</param>
        /// <param name="fullStlPropulsionPower">The maximum total propulsion power the STL engine(s) can generate.</param>
        /// <param name="maxTurnRate">The maximum turn rate the engines are capable of.</param>
        /// <param name="size">The total physical space consumed by the engine(s).</param>
        /// <param name="mass">The total mass of the engin(s).</param>
        /// <param name="expense">The total expense consumed by the engine(s).</param>
        /// <param name="ftlPropulsionPowerFactor">The FTL power multiplier of these engine(s).</param>
        /// <param name="engineQty">The number of engine(s).</param>
        public EnginesStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float fullStlPropulsionPower,
            float maxTurnRate, float size, float mass, float expense, float ftlPropulsionPowerFactor, int engineQty = Constants.One)
            : base(name, imageAtlasID, imageFilename, description, size * engineQty, mass * engineQty, Constants.ZeroF, expense * engineQty) {
            FullStlPropulsionPower = fullStlPropulsionPower * engineQty;
            FullFtlPropulsionPower = fullStlPropulsionPower * engineQty * ftlPropulsionPowerFactor;
            MaxTurnRate = maxTurnRate;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

