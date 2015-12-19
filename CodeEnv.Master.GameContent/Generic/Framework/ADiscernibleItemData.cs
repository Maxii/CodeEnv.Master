// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: ADiscernibleItemData.cs
// Abstract class for Data associated with an ADiscernibleItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract class for Data associated with an ADiscernibleItem.
    /// </summary>
    public abstract class ADiscernibleItemData : AItemData {

        public ACameraItemStat CameraStat { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ADiscernibleItemData"/> class.
        /// </summary>
        /// <param name="itemTransform">The item transform.</param>
        /// <param name="name">The name.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="cameraStat">The camera stat.</param>
        public ADiscernibleItemData(Transform itemTransform, string name, Player owner, ACameraItemStat cameraStat)
            : base(itemTransform, name, owner) {
            CameraStat = cameraStat;
        }


    }
}

