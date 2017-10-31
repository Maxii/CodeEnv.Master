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
    using UnityEngine;

    /// <summary>
    /// Character that can be deployed to UnitCmds to improve CmdEffectiveness.
    /// </summary>
    public class Hero {

        private const string DebugNameFormat = "{0}[{1}]";
        private const float CmdEffectivenessImprovementPerLevel = 0.05F;    // TODO externalize

        private static IDictionary<int, float> _reqdExperienceForNextLevelLookup = new Dictionary<int, float>() {   // TODO externalize
            { 1, 10F },
            { 2, 30F }, // + 20
            { 3, 60F }, // + 30
            { 4, 100F }, // + 40
            { 5, 150F }, // + 50
            { 6, 210F }, // + 60
            { 7, 280F }, // + 70
            { 8, 360F }, // + 80
            { 9, 450F }, // + 90
            { 10, 550F }, // + 100
            { 11, 660F }, // + 110
            { 12, 780F }, // + 120
            { 13, 910F }, // + 130
            { 14, 1050F }, // + 140
            { 15, 1200F }, // + 150
            { 16, 1360F }, // + 160
            { 17, 1530F }, // + 170
            { 18, 1710F }, // + 180
            { 19, 1900F }, // + 190
            { 20, 2100F }, // + 200
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

        public float NextLevelCompletionPercentage { get; private set; }

        private int _level = Constants.One;
        public virtual int Level { get { return _level; } }

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
            bool isMaxLevel = false;
            float reqdExperienceForNextLevel;
            if (_reqdExperienceForNextLevelLookup.TryGetValue(Level, out reqdExperienceForNextLevel)) {
                if (Experience >= reqdExperienceForNextLevel) {
                    _level++;
                    AssessLevel();
                    AssessCmdEffectiveness();
                }
            }
            else {
                isMaxLevel = true;
            }

            if (Level == 1) {
                NextLevelCompletionPercentage = Mathf.Clamp01(Experience / reqdExperienceForNextLevel);
            }
            else if (isMaxLevel) {
                NextLevelCompletionPercentage = Constants.ZeroF;
            }
            else {
                float currentLevelReqdExperience;
                bool hasNextLevel = _reqdExperienceForNextLevelLookup.TryGetValue(Level - 1, out currentLevelReqdExperience);
                D.Assert(hasNextLevel);
                NextLevelCompletionPercentage = Mathf.Clamp01((Experience - currentLevelReqdExperience) / (reqdExperienceForNextLevel - currentLevelReqdExperience));
            }
        }

        private void AssessCmdEffectiveness() {
            CmdEffectiveness = _stat.StartingCmdEffectiveness + CmdEffectivenessImprovementPerLevel * Level;
        }

        private float GetExperienceReqdForNextLevel(int level) {
            float nextLevelReqdExperience;
            if (!_reqdExperienceForNextLevelLookup.TryGetValue(level, out nextLevelReqdExperience)) {
                nextLevelReqdExperience = Mathf.Infinity;
            }
            return nextLevelReqdExperience;
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

