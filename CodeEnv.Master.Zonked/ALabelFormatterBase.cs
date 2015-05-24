// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ALabelFormatterBase.cs
// Abstract base class for Item Data LabelFormatters.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class for Item Data LabelFormatters.
    /// </summary>
    public abstract class ALabelFormatterBase {

        protected static IColoredTextList _unknownValue = new ColoredTextList_String(Constants.QuestionMark);

        protected static IColoredTextList _emptyValue = new ColoredTextList();

        #region Nested Classes

        public enum LabelLineID {

            None,

            Name,
            ParentName,
            Owner,

            IntelCoverage,

            Category,

            Capacity,

            Resources,

            Specials,

            MaxHitPoints,
            CurrentHitPoints,
            Health,
            Defense,
            Mass,

            SectorIndex,

            MaxWeaponsRange,
            MaxSensorRange,
            Offense,


            Target,
            CombatStance,
            CurrentSpeed,
            FullSpeed,
            MaxTurnRate,

            Composition,
            Formation,
            CurrentCmdEffectiveness,

            UnitMaxWeaponsRange,
            UnitMaxSensorRange,
            UnitOffense,
            UnitDefense,
            UnitMaxHitPts,
            UnitCurrentHitPts,
            UnitHealth,

            Population,
            CapacityUsed,
            ResourcesUsed,
            SpecialsUsed,

            UnitFullSpeed,
            UnitMaxTurnRate,
            Density

        }

        #endregion

    }
}

