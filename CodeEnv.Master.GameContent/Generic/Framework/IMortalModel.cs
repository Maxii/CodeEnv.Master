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

        /// <summary>
        /// Occurs when this mortal model has died. Intended for internal
        /// communication from the model to its view and presenter as the
        /// object reference included makes the whole model accessible.
        /// </summary>
        event Action<IMortalModel> onDeathOneShot;

        //new AMortalItemData Data { get; set; }

        void OnShowCompletion();

        bool IsAlive { get; }

        bool IsOperational { get; }

        void __SimulateAttacked();

    }
}

