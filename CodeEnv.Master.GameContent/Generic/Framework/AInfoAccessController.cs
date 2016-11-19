// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AInfoAccessController.cs
// Abstract base class that controls other player's access to Item info.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    /// <summary>
    /// Abstract base class that controls other player's access to Item info.
    /// </summary>
    public abstract class AInfoAccessController {

        protected bool ShowDebugLog { get { return _data.ShowDebugLog; } }

        protected AItemData _data;

        public AInfoAccessController(AItemData data) {
            _data = data;
        }

        public abstract bool HasAccessToInfo(Player player, ItemInfoID infoID);

    }
}

