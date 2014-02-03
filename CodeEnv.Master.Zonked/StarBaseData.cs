// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarBaseData.cs
// All the data associated with a particular StarBase.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;

    /// <summary>
    /// All the data associated with a particular StarBase.
    /// </summary>
    public class StarBaseData : AMortalItemData {

        private StarbaseCategory _size;
        public StarbaseCategory Size {
            get { return _size; }
            set {
                SetProperty<StarbaseCategory>(ref _size, value, "Size");
            }
        }

        private IPlayer _owner;
        public IPlayer Owner {
            get { return _owner; }
            set {
                SetProperty<IPlayer>(ref _owner, value, "Owner");
            }
        }

        private CombatStrength _combatStrength;
        public CombatStrength Strength {
            get { return _combatStrength; }
            set {
                SetProperty<CombatStrength>(ref _combatStrength, value, "Strength");
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarBaseData" /> class.
        /// </summary>
        /// <param name="name">Name of the StarBase.</param>
        /// <param name="maxHitPoints">The maximum hit points.</param>
        public StarBaseData(string name, float maxHitPoints)
            : base(name, maxHitPoints) {
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

