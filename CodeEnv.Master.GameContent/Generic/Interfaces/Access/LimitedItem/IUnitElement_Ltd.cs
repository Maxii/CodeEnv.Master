﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IUnitElement_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are AUnitElementItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are AUnitElementItems.
    /// </summary>
    public interface IUnitElement_Ltd : IMortalItem_Ltd {

        event EventHandler isHQChanged;

        bool IsHQ { get; }

        IUnitCmd_Ltd Command { get; }

    }
}

