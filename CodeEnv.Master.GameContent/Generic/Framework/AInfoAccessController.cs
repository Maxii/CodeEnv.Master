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

    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract base class that controls other player's access to Item info.
    /// </summary>
    public abstract class AInfoAccessController {

        private const string DebugNameFormat = "{0}.{1}";

        public string DebugName { get { return DebugNameFormat.Inject(_data.DebugName, GetType().Name); } }

        protected bool ShowDebugLog { get { return _data.ShowDebugLog; } }

        protected AItemData _data;

        public AInfoAccessController(AItemData data) {
            _data = data;
        }

        public abstract bool HasAccessToInfo(Player player, ItemInfoID infoID);

        public sealed override string ToString() {
            return DebugName;
        }

    }
}

