// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IModel.cs
// Interface for an ItemModel.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.ComponentModel;
    using UnityEngine;

    /// <summary>
    /// Interface for an ItemModel.
    /// </summary>
    public interface IModel : IChangeTracking, INotifyPropertyChanged, INotifyPropertyChanging {

        //AItemData Data { get; set; }

        string FullName { get; }

        /// <summary>
        /// The radius in units of the conceptual 'globe' that encompasses this Item. Readonly.
        /// </summary>
        float Radius { get; }

        Transform Transform { get; }

    }
}

