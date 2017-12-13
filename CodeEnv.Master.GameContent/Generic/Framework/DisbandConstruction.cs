﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DisbandConstruction.cs
// Tracks progress of an element construction while disbanding.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Tracks progress of an element construction while disbanding.
    /// </summary>
    public class DisbandConstruction : Construction {

        private float _disbandCost;
        public override float CostToConstruct { get { return _disbandCost; } }

        public DisbandConstruction(AUnitElementDesign designToDisband, IUnitElement element, float disbandCost)
            : base(designToDisband, element) {
            _disbandCost = disbandCost;
            __Validate();
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __Validate() {
            D.AssertNotEqual(Constants.ZeroF, _disbandCost);
        }

        #endregion


    }
}

