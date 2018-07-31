﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: UnitCreatorConfiguration.cs
// The configuration of the Unit the Creator is to build and deploy.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;
    using CodeEnv.Master.Common;

    /// <summary>
    /// The configuration of the Unit the Creator is to build and deploy.
    /// </summary>
    public class UnitCreatorConfiguration {

        public string DebugName { get { return GetType().Name; } }

        public Player Owner { get; private set; }

        public GameDate DeployDate { get; private set; }

        public string CmdModDesignName { get; private set; }

        public IEnumerable<string> ElementDesignNames { get; private set; }

        public UnitCreatorConfiguration(Player owner, GameDate deployDate, string cmdModDesignName, IEnumerable<string> elementDesignNames) {
            Owner = owner;
            DeployDate = deployDate;
            CmdModDesignName = cmdModDesignName;
            ElementDesignNames = elementDesignNames;
            __ValidateDeployDate();
        }

        public override string ToString() {
            return DebugName;
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __ValidateDeployDate() {
            GameDate earliestDate;
            if (!GameReferences.GameManager.IsRunning) {
                earliestDate = GameTime.GameStartDate;
            }
            else {
                earliestDate = GameTime.Instance.CurrentDate;
            }
            D.AssertNotDefault(earliestDate);
            if (DeployDate < earliestDate) {
                D.Error("{0}.DeployDate {1} < {2}!", CmdModDesignName, DeployDate, earliestDate);
            }
        }

        #endregion

    }
}

