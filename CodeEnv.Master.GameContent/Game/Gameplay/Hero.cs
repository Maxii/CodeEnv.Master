// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: Hero.cs
// Character that can be deployed to UnitCmds to improve CmdEffectiveness.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using Common;

    /// <summary>
    /// Character that can be deployed to UnitCmds to improve CmdEffectiveness.
    /// </summary>
    public class Hero {

        private const string DebugNameFormat = "{0}[{1}]";
        private const float CmdEffectivenessImprovementPerLevel = 0.05F;    // TODO externalize

        private static IDictionary<int, float> _reqdExperiencePerLevelLookup = new Dictionary<int, float>() {   // TODO externalize
            { 0, 10F },
            { 1, 30F }, // + 20
            { 2, 60F }, // + 30
            { 3, 100F }, // + 40
            { 4, 150F }, // + 50
            { 5, 210F }, // + 60
            { 6, 280F }, // + 70
            { 7, 360F }, // + 80
            { 8, 450F }, // + 90
            { 9, 550F }, // + 100
            { 10, 660F }, // + 110
            { 11, 780F }, // + 120
            { 12, 910F }, // + 130
            { 13, 1050F }, // + 140
            { 14, 1200F }, // + 150
            { 15, 1360F }, // + 160
            { 16, 1530F }, // + 170
            { 17, 1710F }, // + 180
            { 18, 1900F }, // + 190
            { 19, 2100F }, // + 200
        };

        private string _debugName;
        public string DebugName {
            get {
                if (_debugName == null) {
                    _debugName = DebugNameFormat.Inject(GetType().Name, Name);
                }
                return _debugName;
            }
        }

        public string Name { get { return _stat.Name; } }

        public AtlasID ImageAtlasID { get { return _stat.ImageAtlasID; } }

        public string ImageFilename { get { return _stat.ImageFilename; } }

        public Species Species { get { return _stat.Species; } }

        public HeroCategory Category { get { return _stat.Category; } }

        public string Description { get { return _stat.Description; } }

        public float CmdEffectiveness { get; private set; }

        public float Experience { get; private set; }

        public int Level { get; private set; }

        private HeroStat _stat;

        public Hero(HeroStat stat) {
            _stat = stat;
            CmdEffectiveness = stat.StartingCmdEffectiveness;
            Experience = stat.StartingExperience;
            AssessLevel();
        }

        public virtual void IncrementExperienceBy(float increasedExperience) {
            Experience += increasedExperience;
            AssessLevel();
        }

        private void AssessLevel() {
            float reqdExperienceForNextLevel;
            if (_reqdExperiencePerLevelLookup.TryGetValue(Level, out reqdExperienceForNextLevel)) {
                if (Experience >= reqdExperienceForNextLevel) {
                    Level++;
                    AssessLevel();
                    AssessCmdEffectiveness();
                }
            }
        }

        private void AssessCmdEffectiveness() {
            CmdEffectiveness = _stat.StartingCmdEffectiveness + CmdEffectivenessImprovementPerLevel * Level;
        }

        public sealed override string ToString() {
            return DebugName;
        }

        #region Nested Classes

        public enum HeroCategory {  // UNDONE Use
            None,

            Admiral,
            Administrator,
            Scientist,
            Economist,
            Diplomat
        }

        #endregion

    }
}

