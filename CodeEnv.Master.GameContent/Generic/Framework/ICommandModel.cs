// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ICommandModel.cs
//  Interface for UnitCommandModels.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Interface for UnitCommandModels.
    /// </summary>
    public interface ICommandModel : IMortalModel {

        event Action onElementsInitializationCompleted_OneShot;

        event Action<IElementModel> onSubordinateElementDeath;

        new ACommandData Data { get; set; }

        string UnitName { get; }

        IElementModel HQElement { get; set; }

        IList<IElementModel> Elements { get; set; }

        /// Adds the Element to this Command including parenting if needed.
        /// </summary>
        /// <param name="element">The Element to add.</param>
        void AddElement(IElementModel element);

        void RemoveElement(IElementModel element);

        bool __CheckForDamage(bool isHQElementAlive);

    }
}

