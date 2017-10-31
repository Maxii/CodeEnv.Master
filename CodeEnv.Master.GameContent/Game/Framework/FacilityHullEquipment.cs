// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FacilityHullEquipment.cs
// Holds a reference to a facility hull, values associated with that hull along with any equipment that uses mounts attached to the hull.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using Common.LocalResources;

    /// <summary>
    /// Holds a reference to a facility hull, values associated with that hull along with any equipment that uses mounts attached to the hull.
    /// </summary>
    public class FacilityHullEquipment : AHullEquipment {

        public new IFacilityHull Hull {
            get { return base.Hull as IFacilityHull; }
            set { base.Hull = value; }
        }

        public FacilityHullCategory HullCategory { get { return Stat.HullCategory; } }

        protected new FacilityHullStat Stat { get { return base.Stat as FacilityHullStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="FacilityHullEquipment"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public FacilityHullEquipment(FacilityHullStat stat, string name = null) : base(stat, name) { }

        #region Event and Property Change Handlers

        protected override void HullPropSetHandler() {
            D.AssertEqual(Hull.HullCategory, HullCategory);
        }

        #endregion

        public override bool TryGetYield(OutputID outputID, out float yield) {
            switch (outputID) {
                case OutputID.Food:
                    yield = Stat.Food;
                    break;
                case OutputID.Production:
                    yield = Stat.Production;
                    break;
                case OutputID.Income:
                    yield = Stat.Income;
                    break;
                case OutputID.Expense:
                    yield = Expense;
                    break;
                case OutputID.NetIncome:
                    yield = Stat.Income - Expense;
                    break;
                case OutputID.Science:
                    yield = Stat.Science;
                    break;
                case OutputID.Culture:
                    yield = Stat.Culture;
                    break;
                case OutputID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outputID));
            }
            return true;
        }

        public override bool AreSpecsEqual(AEquipmentStat otherStat) {
            return Stat == otherStat as FacilityHullStat;
        }
    }
}

