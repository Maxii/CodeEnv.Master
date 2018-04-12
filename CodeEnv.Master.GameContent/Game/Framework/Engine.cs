// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Engine.cs
// An engine that produces power to generate thrust for an element.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// An engine that produces power to generate thrust for an element.
    /// </summary>
    public class Engine : AEquipment {

        private const string NameFormat = "{0}_Power[{1:0.}]";

        public override string Name {
            get {
                return NameFormat.Inject(base.Name, FullPropulsionPower);
            }
        }

        /// <summary>
        /// The maximum power the engine can project as thrust when operating. 
        /// <remarks>FullSpeed = FullPropulsionPower / (Mass * rigidbody.drag).
        /// This value uses a Game Hour denominator. It is adjusted in
        /// realtime to a Unity seconds value in EngineRoom.ApplyThrust() using GeneralSettings.HoursPerSecond.</remarks>
        /// </summary>
        public float FullPropulsionPower { get; private set; }

        public float MaxTurnRate { get { return Stat.MaxTurnRate; } }

        protected new EngineStat Stat { get { return base.Stat as EngineStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="Engine"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public Engine(EngineStat stat, string name = null) : base(stat, name) { }

        public void CalculatePropulsionPower(float shipMass, float shipOpenSpaceDrag) {
            FullPropulsionPower = GameUtility.CalculateReqdPropulsionPower(Stat.MaxAttainableSpeed, shipMass, shipOpenSpaceDrag);
        }

        public override bool AreSpecsEqual(AEquipmentStat otherStat) {
            return Stat == otherStat as EngineStat;
        }

    }
}

