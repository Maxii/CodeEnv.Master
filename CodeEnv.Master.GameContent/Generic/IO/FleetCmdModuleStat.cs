// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: FleetCmdModuleStat.cs
// Immutable AEquipmentStat for Fleet Command Module equipment.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

using CodeEnv.Master.Common;

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Immutable AEquipmentStat for Fleet Command Module equipment.
    /// <remarks>Implements value-based Equality and HashCode.</remarks>
    /// </summary>
    public class FleetCmdModuleStat : ACmdModuleStat {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(FleetCmdModuleStat left, FleetCmdModuleStat right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(FleetCmdModuleStat left, FleetCmdModuleStat right) {
            return !(left == right);
        }

        #endregion


        public FleetCmdModuleStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass,
            float pwrRqmt, decimal expense, float maxHitPts, float maxCmdStaffEffectiveness)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense, maxHitPts, maxCmdStaffEffectiveness) {
        }

        public FleetCmdModuleStat(string name, float maxCmdStaffEffectiveness)
            : this(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Basic CmdModule Stat", 0F, 0F, 0F, Constants.ZeroCurrency, 10, maxCmdStaffEffectiveness) {
        }

        public FleetCmdModuleStat(string name)
            : this(name, Constants.OneHundredPercent) {
        }


        #region Object.Equals and GetHashCode Override

        public override int GetHashCode() {
            unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                return base.GetHashCode();
            }
        }

        public override bool Equals(object obj) {
            return base.Equals(obj);
        }

        #endregion

    }
}

