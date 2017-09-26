// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdModuleStat.cs
// Immutable AEquipmentStat for Settlement Command Module equipment.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Immutable AEquipmentStat for Settlement Command Module equipment.
    /// <remarks>Implements value-based Equality and HashCode.</remarks>
    /// </summary>
    public class SettlementCmdModuleStat : ACmdModuleStat {

        #region Comparison Operators Override

        // see C# 4.0 In a Nutshell, page 254

        public static bool operator ==(SettlementCmdModuleStat left, SettlementCmdModuleStat right) {
            // https://msdn.microsoft.com/en-us/library/ms173147(v=vs.90).aspx
            if (ReferenceEquals(left, right)) { return true; }
            if (((object)left == null) || ((object)right == null)) { return false; }
            return left.Equals(right);
        }

        public static bool operator !=(SettlementCmdModuleStat left, SettlementCmdModuleStat right) {
            return !(left == right);
        }

        #endregion

        public int StartingPopulation { get; private set; }

        public float StartingApproval { get; private set; }

        public SettlementCmdModuleStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float mass,
            float pwrRqmt, decimal expense, float maxHitPts, float maxCmdStaffEffectiveness, int startingPopulation, float startingApproval)
            : base(name, imageAtlasID, imageFilename, description, size, mass, pwrRqmt, expense, maxHitPts, maxCmdStaffEffectiveness) {
            StartingPopulation = startingPopulation;
            Utility.ValidateForRange(startingApproval, Constants.ZeroPercent, Constants.OneHundredPercent);
            StartingApproval = startingApproval;
        }

        public SettlementCmdModuleStat(string name)
            : this(name, AtlasID.MyGui, TempGameValues.AnImageFilename, "Basic CmdModule Stat", 0F, 0F, 0F, Constants.ZeroCurrency, 10,
            Constants.OneHundredPercent, 100, Constants.OneHundredPercent) { }



        #region Object.Equals and GetHashCode Override

        public override int GetHashCode() {
            unchecked {
                int hash = base.GetHashCode();
                hash = hash * 31 + StartingPopulation.GetHashCode(); // 31 = another prime number
                hash = hash * 31 + StartingApproval.GetHashCode();
                return hash;
            }
        }

        public override bool Equals(object obj) {
            if (base.Equals(obj)) {
                SettlementCmdModuleStat oStat = (SettlementCmdModuleStat)obj;
                return oStat.StartingApproval == StartingApproval && oStat.StartingPopulation == StartingPopulation;
            }
            return false;
        }

        #endregion



    }
}

