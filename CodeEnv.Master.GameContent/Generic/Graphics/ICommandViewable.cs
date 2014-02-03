﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICommandViewable.cs
//  Interface used by a CommandPresenter to communicate with their associated CommandView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    ///  Interface used by a CommandPresenter to communicate with their associated CommandView.
    /// </summary>
    public interface ICommandViewable : IMortalViewable {

        Transform TrackingTarget { set; }

        void ChangeCmdIcon(IIcon icon);

        // ShowHit() from IMortalViewable allows us to optionally show lucky hits on commands

    }
}

