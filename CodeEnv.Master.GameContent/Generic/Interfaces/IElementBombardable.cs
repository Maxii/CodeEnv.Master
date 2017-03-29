// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IElementBombardable.cs
// Interface for Items that can be attacked by Unit Elements from very long range with 'bombard' weapons.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using UnityEngine;

    /// <summary>
    /// Interface for Items that can be attacked by Unit Elements from very long range with 'bombard' weapons.
    /// <remarks>3.26.17 TODO Planetoids should be 'bombardable' by Unit Elements with specialized PlanetBuster Weapons.
    /// Perhaps also entire units.</remarks>
    /// </summary>
    public interface IElementBombardable : IElementAttackable {


    }
}

