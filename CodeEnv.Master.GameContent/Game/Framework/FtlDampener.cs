// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FtlDampener.cs
// Equipment that projects an FTL dampening field.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Equipment that projects an FTL dampening field.
    /// </summary>
    public class FtlDampener : ARangedEquipment {

        public override string DebugName {
            get {
                return RangeMonitor != null ? DebugNameFormat.Inject(RangeMonitor.DebugName, Name) : Name;
            }
        }

        private IFtlDampenerRangeMonitor _rangeMonitor;
        public IFtlDampenerRangeMonitor RangeMonitor {
            get { return _rangeMonitor; }
            set { SetProperty<IFtlDampenerRangeMonitor>(ref _rangeMonitor, value, "RangeMonitor"); }
        }

        protected bool ShowDebugLog { get { return RangeMonitor != null ? RangeMonitor.ShowDebugLog : true; } }

        protected new FtlDampenerStat Stat { get { return base.Stat as FtlDampenerStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FtlDampener"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public FtlDampener(FtlDampenerStat stat, string name = null) : base(stat, name) { }

        public override bool AreSpecsEqual(AEquipmentStat otherStat) {
            return Stat == otherStat as FtlDampenerStat;
        }
    }
}

