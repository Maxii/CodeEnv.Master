// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2014 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdData.cs
// All the data associated with a Starbase.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// All the data associated with a Starbase.
    /// </summary>
    public class StarbaseCmdData : ACommandData {

        public StarbaseCategory Category { get; set; }

        public new FacilityData HQElementData {
            get { return base.HQElementData as FacilityData; }
            set { base.HQElementData = value; }
        }

        private BaseComposition _composition;
        public BaseComposition Composition {
            get { return _composition; }
            private set { SetProperty<BaseComposition>(ref _composition, value, "Composition"); }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarbaseCmdData" /> class.
        /// </summary>
        /// <param name="starbaseName">Name of the starbase.</param>
        /// <param name="cmdMaxHitPoints">The command maximum hit points.</param>
        public StarbaseCmdData(string starbaseName, float cmdMaxHitPoints) : base(starbaseName, cmdMaxHitPoints) { }

        protected override void InitializeComposition() {
            Composition = new BaseComposition();
        }

        protected override void ChangeComposition(AElementData elementData, bool toAdd) {
            bool isChanged = false;
            if (toAdd) {
                isChanged = Composition.Add(elementData as FacilityData);
            }
            else {
                isChanged = Composition.Remove(elementData as FacilityData);
            }

            if (isChanged) {
                Composition = new BaseComposition(Composition);
            }

        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

