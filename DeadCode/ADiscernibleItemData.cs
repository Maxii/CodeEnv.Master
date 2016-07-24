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

    /// <summary>
    /// Abstract class for Data associated with an ADiscernibleItem.
    /// </summary>
    [System.Obsolete]
    public abstract class ADiscernibleItemData : AItemData {

        public ACameraItemStat CameraStat { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ADiscernibleItemData" /> class.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="cameraStat">The camera stat.</param>
        public ADiscernibleItemData(IDiscernibleItem item, Player owner, ACameraItemStat cameraStat)
            : base(item, owner) {
            CameraStat = cameraStat;
        }


    }
}

