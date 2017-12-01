// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: RefitConstructionInfo.cs
// Tracks progress of a element construction during a refit.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// Tracks progress of a element construction during a refit.
    /// </summary>
    [System.Obsolete]
    public class RefitConstructionInfo : ConstructionInfo {

        public override bool IsRefitConstruction { get { return true; } }

        public new IUnitElement Element {
            get { return base.Element; }
            private set { base.Element = value; } // hides the set accessor for Element
        }

        private float _refitCost;
        public override float CostToConstruct {
            get {
                D.AssertNotEqual(Constants.ZeroF, _refitCost);
                return _refitCost;
            }
        }

        public RefitConstructionInfo(AUnitElementDesign design, IUnitElement element, float refitCost)
            : base(design) {
            Element = element;
            _refitCost = refitCost;
        }


    }
}

