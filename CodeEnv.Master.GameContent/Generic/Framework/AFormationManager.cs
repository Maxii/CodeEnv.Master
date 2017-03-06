// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFormationManager.cs
// Abstract base class for Unit Formation Managers.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for Unit Formation Managers.
    /// </summary>
    public abstract class AFormationManager {

        private string DebugName { get { return "{0}_{1}".Inject(_unitCmd.DebugName, GetType().Name); } }

        private bool ShowDebugLog { get { return _unitCmd.ShowDebugLog; } }

        private IList<FormationStationSlotInfo> _availableStationSlots;
        private IDictionary<IUnitElement, FormationStationSlotInfo> _occupiedStationSlotLookup;
        private Formation _currentFormation;
        private IFormationMgrClient _unitCmd;

        public AFormationManager(IFormationMgrClient unitCmd) {
            _unitCmd = unitCmd;
            _occupiedStationSlotLookup = new Dictionary<IUnitElement, FormationStationSlotInfo>(40);
        }

        /// <summary>
        /// Repositions all the Unit elements in the formation designated by the Cmd.
        /// Called when 1) a fleet is first formed, 2) the desired formation has changed, 3) the HQ Element has changed,
        /// or 4) when another large fleet joins this one.
        /// <remarks>Repositioning is accomplished by calling Cmd.PositionElementInFormation().</remarks>
        /// </summary>
        /// <param name="allElements">All elements.</param>
        public void RepositionAllElementsInFormation(IList<IUnitElement> allElements) {
            Formation formation = _unitCmd.UnitFormation;
            if (formation != _currentFormation) {
                _occupiedStationSlotLookup.Clear();
                float formationRadius;
                _availableStationSlots = GenerateFormationSlotInfo(formation, _unitCmd.transform, out formationRadius);
                __maxFormationStationSlots = _availableStationSlots.Count;
                //D.Log(ShowDebugLog, "{0} generated {1} {2}s for Formation {3} => {4}.", DebugName, _availableStationSlots.Count, typeof(FormationStationSlotInfo).Name, formation.GetValueName(), _availableStationSlots.Concatenate());
                _unitCmd.UnitMaxFormationRadius = formationRadius;
                _currentFormation = formation;
            }
            else {
                ReturnAllOccupiedStationSlotsToAvailable();
            }

            int hqCount = Constants.Zero;
            allElements.ForAll(e => {
                if (e.IsHQ) {
                    hqCount++;
                }
                AddAndPositionElement(e);
            });
            D.AssertEqual(Constants.One, hqCount);
        }

        private void ReturnAllOccupiedStationSlotsToAvailable() {
            var occupiedStationSlots = _occupiedStationSlotLookup.Values;
            (_availableStationSlots as List<FormationStationSlotInfo>).AddRange(occupiedStationSlots);
            D.Assert(_availableStationSlots.Count <= __maxFormationStationSlots, "{0}: {1} > Max {2}.".Inject(DebugName, _availableStationSlots.Count, __maxFormationStationSlots));
            //D.Log(ShowDebugLog, "{0}: available {1} count = {2} after {3} occupied slots returned.", DebugName, typeof(FormationStationSlotInfo).Name, _availableStationSlots.Count, occupiedStationSlots.Count);
            _occupiedStationSlotLookup.Clear(); // clear AFTER occupiedStationSlots no longer needed!
        }

        protected abstract IList<FormationStationSlotInfo> GenerateFormationSlotInfo(Formation formation, Transform cmdTransform, out float formationRadius);

        /// <summary>
        /// Selects a FormationStation slot for the provided (non-HQ) element based on the selection constraints
        /// provided, and then calls Command.PositionElementInFormation() using the slot selected.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="selectionConstraint">The selection constraint.</param>
        public void AddAndPositionNonHQElement(IUnitElement element, FormationStationSelectionCriteria selectionConstraint) {
            D.Assert(!element.IsHQ);
            AddAndPositionElement(element, selectionConstraint);
        }

        /// <summary>
        /// Selects a FormationStation slot for the provided (non-HQ) element based on default selection constraints, 
        /// and then calls Command.PositionElementInFormation() using the slot selected.
        /// </summary>
        /// <param name="element">The element.</param>
        public void AddAndPositionNonHQElement(IUnitElement element) {
            D.Assert(!element.IsHQ);
            AddAndPositionElement(element);
        }

        /// <summary>
        /// Selects a FormationStation slot for the provided element based on the selection constraints
        /// provided, if any, and then calls Command.PositionElementInFormation() using the slot selected.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="selectionConstraint">The selection constraint.</param>
        private void AddAndPositionElement(IUnitElement element, FormationStationSelectionCriteria selectionConstraint = default(FormationStationSelectionCriteria)) {
            var slotInfo = SelectAndRecordSlotAsOccupied(element, selectionConstraint);
            _unitCmd.PositionElementInFormation(element, slotInfo);
        }

        private FormationStationSlotInfo SelectAndRecordSlotAsOccupied(IUnitElement element, FormationStationSelectionCriteria selectionConstraints) {
            bool isRemoved;
            FormationStationSlotInfo slotInfo;
            if (_occupiedStationSlotLookup.TryGetValue(element, out slotInfo)) {
                // return element's existing slotInfo BEFORE selecting another
                isRemoved = _occupiedStationSlotLookup.Remove(element);
                D.Assert(isRemoved, element.DebugName);
                _availableStationSlots.Add(slotInfo);
                D.Assert(_availableStationSlots.Count <= __maxFormationStationSlots, "{0}: {1} > Max {2}.".Inject(DebugName, _availableStationSlots.Count, __maxFormationStationSlots));
            }
            slotInfo = SelectSlotInfoFor(element, selectionConstraints);
            isRemoved = _availableStationSlots.Remove(slotInfo);
            D.Assert(isRemoved, slotInfo.ToString());
            _occupiedStationSlotLookup.Add(element, slotInfo);
            D.Assert(_occupiedStationSlotLookup.Count <= __maxFormationStationSlots, "{0}: {1} > Max {2}.".Inject(DebugName, _occupiedStationSlotLookup.Count, __maxFormationStationSlots));
            return slotInfo;
        }

        /// <summary>
        /// TEMP method that selects the stationSlot for the provided element based off of
        /// the provided selectionConstraints.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="selectionConstraints">The selection constraints.</param>
        /// <returns></returns>
        private FormationStationSlotInfo SelectSlotInfoFor(IUnitElement element, FormationStationSelectionCriteria selectionConstraints) {
            if (element.IsHQ) {
                //D.Log(ShowDebugLog, "{0} is about to validate {1} is only HQ.", DebugName, element.DebugName);
                __ValidateSingleHqSlotAvailable();
                return _availableStationSlots.Single(sInfo => sInfo.IsHQSlot);
            }

            FormationStationSlotInfo slot = _availableStationSlots.Where(sInfo => !sInfo.IsHQSlot && sInfo.IsReserve == selectionConstraints.IsReserveReqd).FirstOrDefault();
            if (slot == default(FormationStationSlotInfo)) {
                //D.Log(ShowDebugLog, "{0}: Cannot find {1} meeting Constraint {2} in Formation {3} for {4}. Available Slot Qty = {5}. Occupied Slot Qty = {6}.",
                //    DebugName, typeof(FormationStationSlotInfo).Name, selectionConstraints, _currentFormation.GetValueName(), element.DebugName, _availableStationSlots.Count, _occupiedStationSlotLookup.Count);
                slot = _availableStationSlots.Where(sInfo => !sInfo.IsHQSlot).FirstOrDefault();
            }
            if (slot == default(FormationStationSlotInfo)) {
                D.Error("{0}: Cannot find any available {1} in Formation {2} for {3}. Available Slot Qty = {4}. Occupied Slot Qty = {5}.",
                    DebugName, typeof(FormationStationSlotInfo).Name, _currentFormation.GetValueName(), element.DebugName, _availableStationSlots.Count, _occupiedStationSlotLookup.Count);
                D.Error("{0}: Slots occupied = {1}.", DebugName, _occupiedStationSlotLookup.Keys.Select(e => e.DebugName).Concatenate());
            }
            return slot;
        }

        /// <summary>
        /// Returns <c>true</c> if a FormationStation (slot) that matches the <c>selectionConstraint</c> is available for assignment.
        /// </summary>
        /// <param name="selectionConstraint">The selection constraint.</param>
        /// <returns></returns>
        public bool IsSlotAvailable(FormationStationSelectionCriteria selectionConstraint) {
            D.Assert(_availableStationSlots.Where(sInfo => sInfo.IsHQSlot).IsNullOrEmpty());    // HQ slot should never be available here
            return _availableStationSlots.Where(sInfo => sInfo.IsReserve == selectionConstraint.IsReserveReqd).Any();
        }

        /// <summary>
        /// Handles changes the FormationManager needs to make when an element is removed from the Unit.
        /// WARNING: This does not include removing the FormationStation from the ship and the ship
        /// from the FormationStation, nor does it deal with returning the Station to the pool.
        /// </summary>
        /// <param name="element">The element.</param>
        public void HandleElementRemoval(IUnitElement element) {
            FormationStationSlotInfo elementStationInfo;
            bool isStationSlotFound = _occupiedStationSlotLookup.TryGetValue(element, out elementStationInfo);
            D.Assert(isStationSlotFound, element.DebugName);
            _occupiedStationSlotLookup.Remove(element);
            _availableStationSlots.Add(elementStationInfo);
            D.Assert(_availableStationSlots.Count <= __maxFormationStationSlots, "{0}: {1} > Max {2}.".Inject(DebugName, _availableStationSlots.Count, __maxFormationStationSlots));
        }

        #region Debug

        private int __maxFormationStationSlots;

        private void __ValidateSingleHqSlotAvailable() {
            int count = _availableStationSlots.Where(sInfo => sInfo.IsHQSlot).Count();
            if (count != Constants.One) {
                D.Error("{0}: Expecting 1 HQ formation slot but found {1}. Formation = {2}, AvailableSlots = {3}, OccupiedSlots = {4}.",
                    DebugName, count, _currentFormation.GetValueName(), _availableStationSlots.Concatenate(), _occupiedStationSlotLookup.Values.Concatenate());
            }
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Mutable struct that holds the criteria used for the selection of FormationStations.
        /// WARNING: Not suitable for dictionary keys.
        /// </summary>
        public struct FormationStationSelectionCriteria {

            private const string ToStringFormat = "{0}: isReserveReqd = {1}";

            public bool IsReserveReqd { get; set; }

            public override string ToString() {
                return ToStringFormat.Inject(typeof(FormationStationSelectionCriteria).Name, IsReserveReqd);
            }
        }

        #endregion

    }
}

