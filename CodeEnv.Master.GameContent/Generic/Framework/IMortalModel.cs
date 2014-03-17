// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IMortalModel.cs
// Interface for MortalModels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;

    /// <summary>
    /// Interface for MortalModels.
    /// </summary>
    public interface IMortalModel : IModel {

        event Action<MortalAnimations> onShowAnimation;
        event Action<MortalAnimations> onStopAnimation;
        event Action<IMortalModel> onItemDeath;

        new AMortalItemData Data { get; set; }

        void OnShowCompletion();

        void __SimulateAttacked();

    }
}

