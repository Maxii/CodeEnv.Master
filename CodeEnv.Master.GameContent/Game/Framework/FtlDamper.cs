// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FtlDamper.cs
// Equipment that projects an FTL damping field.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Equipment that projects an FTL damping field.
    /// <remarks>Damp refers to reducing amplitude. Dampen refers to making something wet.</remarks>
    /// </summary>
    public class FtlDamper : ARangedEquipment {

        public override string DebugName {
            get {
                return RangeMonitor != null ? DebugNameFormat.Inject(RangeMonitor.DebugName, Name) : Name;
            }
        }

        private IFtlDamperRangeMonitor _rangeMonitor;
        public IFtlDamperRangeMonitor RangeMonitor {
            get { return _rangeMonitor; }
            set { SetProperty<IFtlDamperRangeMonitor>(ref _rangeMonitor, value, "RangeMonitor"); }
        }

        protected bool ShowDebugLog { get { return RangeMonitor != null ? RangeMonitor.ShowDebugLog : true; } }

        protected new FtlDamperStat Stat { get { return base.Stat as FtlDamperStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtlDamper"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public FtlDamper(FtlDamperStat stat, string name = null) : base(stat, name) { }

        public override bool AreSpecsEqual(AEquipmentStat otherStat) {
            return Stat == otherStat as FtlDamperStat;
        }
    }
}

