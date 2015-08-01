﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PassiveCountermeasureStat.cs
// COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public class PassiveCountermeasureStat : AEquipmentStat {

        public DamageStrength DamageMitigation { get; private set; }


        public PassiveCountermeasureStat(string name, AtlasID imageAtlasID, string imageFilename, string description, float size, float pwrRqmt, DamageStrength damageMitigation)
            : base(name, imageAtlasID, imageFilename, description, size, pwrRqmt) {
            DamageMitigation = damageMitigation;
        }

        /// <summary>
        /// Initializes a new instance of the most basic <see cref="PassiveCountermeasureStat"/> class.
        /// </summary>
        public PassiveCountermeasureStat()
            : this("BasicPassiveCM", AtlasID.MyGui, TempGameValues.AnImageFilename, "BasicDescription..", 0F, 0F, new DamageStrength(1F, 1F, 1F)) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

