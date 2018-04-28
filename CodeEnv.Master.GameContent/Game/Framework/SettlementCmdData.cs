// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementCmdData.cs
// Class for Data associated with a SettlementCmdItem.
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
    using UnityEngine;

    /// <summary>
    /// Class for Data associated with a SettlementCmdItem.
    /// </summary>
    public class SettlementCmdData : AUnitBaseCmdData {

        private SettlementCategory _category;
        public SettlementCategory Category {
            get { return _category; }
            private set { SetProperty<SettlementCategory>(ref _category, value, "Category"); }
        }

        private int _capacity;
        public int Capacity {
            get { return _capacity; }
            private set { SetProperty<int>(ref _capacity, value, "Capacity"); }
        }

        private SystemData _parentSystemData;
        public SystemData ParentSystemData {
            get { return _parentSystemData; }
            set { SetProperty<SystemData>(ref _parentSystemData, value, "ParentSystemData", ParentSystemDataPropSetHandler); }
        }

        public new SettlementInfoAccessController InfoAccessCntlr { get { return base.InfoAccessCntlr as SettlementInfoAccessController; } }

        private SettlementPublisher _publisher;
        public SettlementPublisher Publisher {
            get { return _publisher = _publisher ?? new SettlementPublisher(this); }
        }

        public new SettlementCmdDesign CmdDesign { get { return base.CmdDesign as SettlementCmdDesign; } }

        private IList<IDisposable> _systemDataSubscriptions = new List<IDisposable>(2);

        #region Initialization 

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdData" /> class
        /// with no passive countermeasures.
        /// </summary>
        /// <param name="settlementCmd">The settlement command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="sensors">The MR and LR sensors for this UnitCmd.</param>
        /// <param name="ftlDampener">The FTL dampener.</param>
        /// <param name="cmdDesign">The command design.</param>
        public SettlementCmdData(ISettlementCmd settlementCmd, Player owner, IEnumerable<CmdSensor> sensors, FtlDampener ftlDampener,
           SettlementCmdDesign cmdDesign)
               : this(settlementCmd, owner, Enumerable.Empty<PassiveCountermeasure>(), sensors, ftlDampener, cmdDesign) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SettlementCmdData" /> class.
        /// </summary>
        /// <param name="settlementCmd">The settlement command.</param>
        /// <param name="owner">The owner.</param>
        /// <param name="passiveCMs">The passive countermeasures.</param>
        /// <param name="sensors">The MR and LR sensors for this UnitCmd.</param>
        /// <param name="ftlDampener">The FTL dampener.</param>
        /// <param name="cmdDesign">The command design.</param>
        public SettlementCmdData(ISettlementCmd settlementCmd, Player owner, IEnumerable<PassiveCountermeasure> passiveCMs,
            IEnumerable<CmdSensor> sensors, FtlDampener ftlDampener, SettlementCmdDesign cmdDesign)
            : base(settlementCmd, owner, passiveCMs, sensors, ftlDampener, cmdDesign) {
            Population = cmdDesign.ReqdCmdStat.StartingPopulation;
            Approval = cmdDesign.ReqdCmdStat.StartingApproval;
        }

        protected override AInfoAccessController InitializeInfoAccessController() {
            return new SettlementInfoAccessController(this);
        }

        #endregion

        private void SubscribeToSystemDataProperties() {
            _systemDataSubscriptions.Add(ParentSystemData.SubscribeToPropertyChanged<SystemData, int>(sd => sd.Capacity, SystemCapacityPropChangedHandler));
            _systemDataSubscriptions.Add(ParentSystemData.SubscribeToPropertyChanged<SystemData, ResourcesYield>(sd => sd.Resources, SystemResourceYieldPropChangedHandler));
        }

        public override void AddElement(AUnitElementData elementData) {
            base.AddElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public override void RemoveElement(AUnitElementData elementData) {
            base.RemoveElement(elementData);
            Category = GenerateCmdCategory(UnitComposition);
        }

        public SettlementCategory GenerateCmdCategory(BaseComposition unitComposition) {
            int elementCount = unitComposition.GetTotalElementsCount();
            //D.Log(ShowDebugLog, "{0}'s known elements count = {1}.", DebugName, elementCount);
            if (elementCount >= 8) {
                return SettlementCategory.Territory;
            }
            if (elementCount >= 6) {
                return SettlementCategory.Province;
            }
            if (elementCount >= 4) {
                return SettlementCategory.CityState;
            }
            if (elementCount >= 2) {
                return SettlementCategory.City;
            }
            if (elementCount >= 1) {
                return SettlementCategory.Colony;
            }
            return SettlementCategory.None;
        }

        public SettlementCmdReport GetReport(Player player) { return Publisher.GetReport(player); }

        #region Event and Property Change Handlers

        private void ParentSystemDataPropSetHandler() {
            SubscribeToSystemDataProperties();
        }

        private void SystemCapacityPropChangedHandler() {
            UpdateCapacity();
        }

        private void SystemResourceYieldPropChangedHandler() {
            UpdateResources();
        }

        #endregion


        public override void ChangeDesign(AUnitCmdDesign cmdDesign, IEnumerable<PassiveCountermeasure> passiveCMs, IEnumerable<CmdSensor> sensors, FtlDampener ftlDampener) {
            base.ChangeDesign(cmdDesign, passiveCMs, sensors, ftlDampener);
            // 4.28.18 Not currently planning on fooling with Population or Approval
        }

        private void UpdateCapacity() {
            Capacity = ParentSystemData.Capacity;
        }

        private void UpdateResources() {
            Resources = ParentSystemData.Resources;
        }

        protected override void Unsubscribe() {
            base.Unsubscribe();
            _systemDataSubscriptions.ForAll(s => s.Dispose());
            _systemDataSubscriptions.Clear();
        }

    }
}

