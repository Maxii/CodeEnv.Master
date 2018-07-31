// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: StarbaseCmdData.cs
// Class for Data associated with a StarbaseCmdItem.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Class for Data associated with a StarbaseCmdItem.
    /// </summary>
    public class StarbaseCmdData : AUnitBaseCmdData {

        private const StarbaseCategory IsEstablishedThreshold = StarbaseCategory.LocalBase;

        private StarbaseCategory _category;
        public StarbaseCategory Category {
            get { return _category; }
            private set { SetProperty<StarbaseCategory>(ref _category, value, "Category", CategoryPropChangedHandler); }
        }

        private int _capacity;
        public int Capacity {
            get { return _capacity; }
            private set { SetProperty<int>(ref _capacity, value, "Capacity"); }
        }

        public new StarbaseInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as StarbaseInfoAccessController; } }

        private StarbasePublisher _publisher;
        public StarbasePublisher Publisher {
            get { return _publisher = _publisher ?? new StarbasePublisher(this); }
        }

        public new StarbaseCmdModuleDesign CmdModuleDesign { get { return base.CmdModuleDesign as StarbaseCmdModuleDesign; } }

        public bool IsCrippled { get; set; }    // UNDONE has no effect

        private bool _isEstablished;
        public bool IsEstablished {
            get { return _isEstablished; }
            private set { SetProperty<bool>(ref _isEstablished, value, "IsEstablished"); }
        }

        private IList<Player> _playersFullyExplored;

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="StarbaseCmdData" /> class
        /// with no passive countermeasures.
        /// </summary>
        /// <param name="starbaseCmd">The starbase command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="sensors">The MR and LR sensors for this UnitCmd.</param>
        /// <param name="ftlDamper">The FTL damper.</param>
        /// <param name="cmdModDesign">The cmd module design.</param>
        public StarbaseCmdData(IStarbaseCmd starbaseCmd, Player owner, IEnumerable<CmdSensor> sensors, FtlDamper ftlDamper,
            StarbaseCmdModuleDesign cmdModDesign)
            : this(starbaseCmd, owner, Enumerable.Empty<PassiveCountermeasure>(), sensors, ftlDamper, cmdModDesign) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StarbaseCmdData" /> class.
        /// </summary>
        /// <param name="starbaseCmd">The starbase command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="sensors">The MR and LR sensors for this UnitCmd.</param>
        /// <param name="ftlDamper">The FTL damper.</param>
        /// <param name="cmdModDesign">The cmd module design.</param>
        public StarbaseCmdData(IStarbaseCmd starbaseCmd, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs,
            IEnumerable<CmdSensor> sensors, FtlDamper ftlDamper, StarbaseCmdModuleDesign cmdModDesign)
            : base(starbaseCmd, owner, passiveCMs, sensors, ftlDamper, cmdModDesign) {
            __PopulatePropertyValuesFromSector();
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new StarbaseInfoAccessController(this);
        }

        #endregion

        public override void AddElement(AUnitElementData elementData) {
            base.AddElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public override void RemoveElement(AUnitElementData elementData) {
            base.RemoveElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public StarbaseCategory GenerateCmdCategory(BaseComposition unitComposition) {
            int elementCount = unitComposition.GetTotalElementsCount();
            //D.Log(ShowDebugLog, "{0}'s known elements count = {1}.", DebugName, elementCount);
            if (elementCount >= 8) {
                return StarbaseCategory.TerritorialBase;
            }
            if (elementCount >= 6) {
                return StarbaseCategory.RegionalBase;
            }
            if (elementCount >= 4) {
                return StarbaseCategory.DistrictBase;
            }
            if (elementCount >= 2) {
                return StarbaseCategory.LocalBase;
            }
            if (elementCount >= 1) {
                return StarbaseCategory.Outpost;
            }
            return StarbaseCategory.None;
        }

        public StarbaseCmdReport GetReport(Player player) { return Publisher.GetReport(player); }

        public override void ReplaceCmdModuleWith(AUnitCmdModuleDesign cmdModDesign, IEnumerable<PassiveCountermeasure> passiveCMs, 
            IEnumerable<CmdSensor> sensors, FtlDamper ftlDamper) {
            base.ReplaceCmdModuleWith(cmdModDesign, passiveCMs, sensors, ftlDamper);
            // CmdModuleDesign does have StarbaseCmdModule-specific values (StartingPopulation, StartingApproval) but they should be ignored
        }

        public bool IsFullyExploredBy(Player player) {
            return _playersFullyExplored != null && _playersFullyExplored.Contains(player);
        }

        #region Event and Property Change Handlers

        private void CategoryPropChangedHandler() {
            HandleCategoryChanged();
        }

        #endregion

        private void HandleCategoryChanged() {
            bool isEstablished = Category >= IsEstablishedThreshold;
            if (IsEstablished != isEstablished) {
                IsEstablished = isEstablished;
            }
        }

        protected override void HandleIntelCoverageChangedFor(Player player) {
            base.HandleIntelCoverageChangedFor(player);
            AssessRecordFullyExploredBy(player);
        }

        private void AssessRecordFullyExploredBy(Player player) {
            if (GetIntelCoverage(player) == IntelCoverage.Comprehensive) {
                _playersFullyExplored = _playersFullyExplored ?? new List<Player>();
                if (!_playersFullyExplored.Contains(player)) {
                    _playersFullyExplored.Add(player);
                }
            }
        }

        #region Debug

        // UNDONE Acquire resource values this starbase has access too, ala SettlementCmdData approach.
        // 10.15.17 Need to determine what other Resources besides a System's Resources can be present in
        // a Sector. Then decide what Resources in a Sector a Starbase can have access too.
        private void __PopulatePropertyValuesFromSector() {
            Capacity = 10;
            var resources = new ResourcesYield.ResourceValuePair[] {
                new ResourcesYield.ResourceValuePair(ResourceID.Organics, UnityEngine.Random.Range(0F, 0.3F)),
                new ResourcesYield.ResourceValuePair(ResourceID.Particulates, UnityEngine.Random.Range(0.2F, 0.6F)),
                new ResourcesYield.ResourceValuePair(ResourceID.Energy, UnityEngine.Random.Range(1F, 2F)),
                new ResourcesYield.ResourceValuePair(ResourceID.Titanium, UnityEngine.Random.Range(0F, 1F)),
                new ResourcesYield.ResourceValuePair(ResourceID.Duranium, UnityEngine.Random.Range(0F, 1F)),
                new ResourcesYield.ResourceValuePair(ResourceID.Unobtanium, UnityEngine.Random.Range(0F, 0.6F))
            };
            Resources = new ResourcesYield(resources);
        }

        #endregion

    }
}

