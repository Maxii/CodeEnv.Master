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

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// Tracks progress of an element construction during a refit.
    /// </summary>
    public class RefitConstruction : Construction {

        public override bool IsRefitConstruction { get { return true; } }

        private float _refitCost;
        public override float CostToConstruct { get { return _refitCost; } }

        public RefitConstruction(AUnitElementDesign design, IUnitElement element, float refitCost)
            : base(design, element) {
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

