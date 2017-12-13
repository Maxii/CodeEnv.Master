// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RefitConstruction.cs
// Tracks progress of an element construction during a refit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Tracks progress of an element construction during a refit.
    /// </summary>
    public class RefitConstruction : Construction {

        private float _refitCost;
        public override float CostToConstruct { get { return _refitCost; } }

        public RefitConstruction(AUnitElementDesign refitDesign, IUnitElement element, float refitCost)
            : base(refitDesign, element) {
            _refitCost = refitCost;
            __Validate();
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __Validate() {
            D.AssertNotEqual(Constants.ZeroF, _refitCost);
        }

        #endregion

    }
}

