// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdData.cs
// All the data associated with a StarbaseCmd.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// All the data associated with a StarbaseCmd.
    /// </summary>
    public class StarbaseCmdData : ACommandData {

        private StarbaseCategory _category;
        public StarbaseCategory Category {
            get { return _category; }
            private set { SetProperty<StarbaseCategory>(ref _category, value, "Category"); }
        }

        public new FacilityData HQElementData {
            get { return base.HQElementData as FacilityData; }
            set { base.HQElementData = value; }
        }

        public BaseComposition Composition { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarbaseCmdData" /> class.
        /// </summary>
        /// <param name="stat">The stat.</param>
        public StarbaseCmdData(StarbaseCmdStat stat)
            : base(stat.Name, stat.MaxHitPoints) {
            MaxCmdEffectiveness = stat.MaxCmdEffectiveness;
            UnitFormation = stat.UnitFormation;
        }

        protected override void InitializeComposition() {
            Composition = new BaseComposition();
        }

        protected override void ChangeComposition(AElementData elementData, bool toAdd) {
            bool isChanged = toAdd ? Composition.Add(elementData as FacilityData) : Composition.Remove(elementData as FacilityData);
            if (isChanged) {
                AssessCommandCategory();
                OnCompositionChanged();
            }
        }

        private void AssessCommandCategory() {
            int elementCount = Composition.ElementCount;
            switch (elementCount) {
                case 1:
                    Category = StarbaseCategory.Outpost;
                    break;
                case 2:
                case 3:
                    Category = StarbaseCategory.LocalBase;
                    break;
                case 4:
                case 5:
                    Category = StarbaseCategory.DistrictBase;
                    break;
                case 6:
                case 7:
                    Category = StarbaseCategory.RegionalBase;
                    break;
                case 8:
                case 9:
                    Category = StarbaseCategory.TerritorialBase;
                    break;
                case 0:
                    // element count of 0 = dead, so don't generate a change to be handled
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(elementCount));
            }
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

