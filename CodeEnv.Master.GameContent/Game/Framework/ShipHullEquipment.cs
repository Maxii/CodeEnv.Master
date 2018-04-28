// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ShipHullEquipment.cs
// Holds a reference to a ship hull, values associated with that hull along with any equipment that uses mounts attached to the hull.
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
    /// Holds a reference to a ship hull, values associated with that hull along with any equipment that uses mounts attached to the hull.
    /// </summary>
    public class ShipHullEquipment : AHullEquipment {

        public new IShipHull Hull {
            get { return base.Hull as IShipHull; }
            set { base.Hull = value; }
        }

        public ShipHullCategory HullCategory { get { return Stat.HullCategory; } }

        /// <summary>
        /// The constraint on the ability of the engine(s) to turn this hull. 
        /// <remarks>Used as a multiplier with the Engine's MaxTurnRate to derive a ship's turn rate.</remarks>
        /// </summary>
        public float TurnRateConstraint { get { return Stat.TurnRateConstraint; } }

        /// <summary>
        /// The drag of this hull in Topography.OpenSpace.
        /// </summary>
        public float Drag { get { return Stat.Drag; } }

        protected new ShipHullStat Stat { get { return base.Stat as ShipHullStat; } }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShipHullEquipment"/> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        /// <param name="name">The optional unique name for this equipment. If not provided, the name embedded in the stat will be used.</param>
        public ShipHullEquipment(ShipHullStat stat, string name = null) : base(stat, name) { }

        #region Event and Property Change Handlers

        protected override void HullPropSetHandler() {
            D.AssertEqual(Hull.HullCategory, HullCategory);
        }

        #endregion

        public override bool TryGetYield(OutputID outputID, out float yield) {
            switch (outputID) {
                case OutputID.Food:
                    yield = Constants.ZeroF;
                    return false;
                case OutputID.Production:
                    yield = Constants.ZeroF;
                    return false;
                case OutputID.Income:
                    yield = Stat.Income;
                    return true;
                case OutputID.Expense:
                    yield = Expense;
                    return true;
                case OutputID.NetIncome:
                    yield = Stat.Income - Expense;
                    return true;
                case OutputID.Science:
                    yield = Stat.Science;
                    return true;
                case OutputID.Culture:
                    yield = Stat.Culture;
                    return true;
                case OutputID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outputID));
            }
        }

        public override bool AreSpecsEqual(AEquipmentStat otherStat) {
            return Stat == otherStat as ShipHullStat;
        }

    }
}

