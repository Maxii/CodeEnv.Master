// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMortalItem_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are AMortalItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using Common;

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are AMortalItems.
    /// </summary>
    public interface IMortalItem_Ltd : IIntelItem_Ltd {

        event EventHandler deathOneShot;

        event EventHandler __death;

        IntVector3 SectorID { get; }

        void __LogDeathEventSubscribers();


    }
}

