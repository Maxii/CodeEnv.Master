﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ActiveCountermeasureStat.cs
// Stat for an active countermeasure.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Stat for an active countermeasure.
    /// </summary>
    public class ActiveCountermeasureStat : ARangedEquipmentStat {

        private static string _toStringFormat = "{0}({1}, {2})";

        public DeliveryStrength InterceptStrength { get; private set; }

        public float InterceptAccuracy { get; private set; }

        public float ReloadPeriod { get; private set; }

        public DamageStrength DamageMitigation { get; private set; }

        public ActiveCountermeasureStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float pwrRqmt, RangeCategory rangeCat, float baseRangeDistance, DeliveryStrength interceptStrength, float accuracy, float reloadPeriod, DamageStrength damageMitigation)
            : base(name, imageAtlasID, imageFilename, description, size, pwrRqmt, rangeCat, baseRangeDistance) {
            InterceptStrength = interceptStrength;
            InterceptAccuracy = accuracy;
            ReloadPeriod = reloadPeriod;
            DamageMitigation = damageMitigation;
        }

        public override string ToString() {
            return _toStringFormat.Inject(Name, InterceptStrength, DamageMitigation);
        }

    }
}

