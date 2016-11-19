// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IStar_Ltd.cs
// limited InfoAccess Interface for easy access to MonoBehaviours that are StarItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR


namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// limited InfoAccess Interface for easy access to MonoBehaviours that are StarItems.
    /// </summary>
    public interface IStar_Ltd : IIntelItem_Ltd {

        IntVector3 SectorID { get; }

        ISystem_Ltd ParentSystem { get; }

    }
}

