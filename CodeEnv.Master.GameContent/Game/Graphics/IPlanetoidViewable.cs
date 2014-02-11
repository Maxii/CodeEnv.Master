// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IPlanetoidViewable.cs
//  Interface used by a PlanetoidPresenter to communicate with their associated PlanetoidView.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface used by a PlanetoidPresenter to communicate with their associated PlanetoidView.
    /// </summary>
    public interface IPlanetoidViewable : IMortalViewable {

        void ShowHit();

    }
}

