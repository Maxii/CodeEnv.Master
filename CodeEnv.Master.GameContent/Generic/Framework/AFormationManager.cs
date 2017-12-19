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

    using System;
    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for Unit Formation Managers.
    /// </summary>
    public abstract class AFormationManager {

        /// <summary>
        /// Returns <c>true</c> if this FormationManager currently has a station slot available for one element.
        /// </summary>
        public bool HasRoom { get { return HasRoomFor(Constants.One); } }

        public virtual string DebugName { get { return "{0}_{1}".Inject(_client.DebugName, GetType().Name); } }

        private bool ShowDebugLog { get { return _client.ShowDebugLog; } }

        private IDictionary<IUnitElement, FormationStationSlotInfo> _occupiedStationSlotLookup;
        private List<FormationStationSlotInfo> _availableStationSlots;
        private Formation _currentFormation;
        private IFormationMgrClient _client;

        public AFormationManager(IFormationMgrClient client) {
            _client = client;
            _occupiedStationSlotLookup = new Dictionary<IUnitElement, FormationStationSlotInfo>(40);    // HACK
        }

        /// <summary>
        /// Repositions all the Unit elements in the formation designated by the Client.
        /// Called when 1) the desired formation has changed, 2) the Unit's HQElement has changed before
        /// the Unit has become operational, and 3) a Hanger client has its first ship added.
        /// <remarks>Repositioning is accomplished by calling Client.PositionElementInFormation().</remarks>
        /// </summary>
        /// <param name="allElements">All elements.</param>
        public void RepositionAllElementsInFormation(IEnumerable<IUnitElement> allElements) {
            Formation formation = _client.Formation;
            if (formation != _currentFormation) {
                _occupiedStationSlotLookup.Clear();
                float formationRadius;
                _availableStationSlots = GenerateFormationSlotInfo(formation, _client.transform, out formationRadius);
                D.AssertEqual(formation.MaxFormationSlots(), _availableStationSlots.Count);
                //D.Log(ShowDebugLog, "{0} generated {1} {2}s for Formation {3} => {4}.", DebugName, _availableStationSlots.Count, 
                // typeof(FormationStationSlotInfo).Name, formation.GetValueName(), _availableStationSlots.Concatenate());
                _client.HandleMaxFormationRadiusDetermined(formationRadius);
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
            D.Assert(hqCount < 2);  // HACK allows 0 and 1. Future option to have more than 1 designated HQ slot?
        }

        private void ReturnAllOccupiedStationSlotsToAvailable() {
            var occupiedStationSlots = _occupiedStationSlotLookup.Values;
            _availableStationSlots.AddRange(occupiedStationSlots);
            D.AssertEqual(_currentFormation.MaxFormationSlots(), _availableStationSlots.Count, "{0}: {1} != Max {2}.".Inject(DebugName, _availableStationSlots.Count, _currentFormation.MaxFormationSlots()));
            //D.Log(ShowDebugLog, "{0}: available {1} count = {2} after {3} occupied slots returned.", DebugName, typeof(FormationStationSlotInfo).Name, _availableStationSlots.Count, occupiedStationSlots.Count);
            _occupiedStationSlotLookup.Clear(); // clear AFTER occupiedStationSlots no longer needed!
        }

        protected abstract List<FormationStationSlotInfo> GenerateFormationSlotInfo(Formation formation, Transform followTransform, out float formationRadius);

        /// <summary>
        /// Replaces an existing element in the formation with another.
        /// <remarks>Handles both HQ and non-HQ elements but can't be mixed. Used primarily when a refit is completed.</remarks>
        /// </summary>
        /// <param name="elementToReplace">The element to replace.</param>
        /// <param name="replacingElement">The replacing element.</param>
        public void ReplaceElement(IUnitElement elementToReplace, IUnitElement replacingElement) {
            D.Assert(elementToReplace.IsHQ == replacingElement.IsHQ);
            FormationStationSlotInfo slot;
            bool isFound = _occupiedStationSlotLookup.TryGetValue(elementToReplace, out slot);
            D.Assert(isFound);
            _occupiedStationSlotLookup.Remove(elementToReplace);

            _occupiedStationSlotLookup.Add(replacingElement, slot); // throws exception if element already present
            _client.PositionElementInFormation(replacingElement, slot);
        }

        /// <summary>
        /// Selects a FormationStation slot for the provided (non-HQ) element based on the selection constraints
        /// provided, if any, and then calls Command.PositionElementInFormation() using the slot selected.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="selectionConstraint">The optional selection constraint.</param>
        public void AddAndPositionNonHQElement(IUnitElement element, FormationStationSelectionCriteria selectionConstraint = default(FormationStationSelectionCriteria)) {
            // 10.25.17 Elements will not yet be operational when added upon starting initial construction
            D.Assert(!element.IsHQ);
            AddAndPositionElement(element, selectionConstraint);
        }

        /// <summary>
        /// Selects the already returned HQ FormationStation slot for the provided new hqElement,
        /// and then calls Command.PositionElementInFormation() using the slot selected.
        /// <remarks>The returned HQ slot was returned by RestoreSlotToAvailable().</remarks>
        /// </summary>
        /// <param name="hqElement">The HQElement.</param>
        public void AddAndPositionHQElement(IUnitElement hqElement) {
            D.Assert(!hqElement.IsDead, hqElement.DebugName); // 10.30.17 Should be operational to handle HQChange events
            D.Assert(hqElement.IsHQ);
            AddAndPositionElement(hqElement);
        }

        /// <summary>
        /// Selects a FormationStation slot for the provided element based on the selection constraints
        /// provided, if any, and then calls Command.PositionElementInFormation() using the slot selected.
        /// </summary>
        /// <param name="element">The element.</param>
        /// <param name="selectionConstraint">The selection constraint.</param>
        private void AddAndPositionElement(IUnitElement element, FormationStationSelectionCriteria selectionConstraint = default(FormationStationSelectionCriteria)) {
            D.Assert(HasRoom, "No room available to add element to formation.");
            var slot = SelectAndRecordSlotAsOccupied(element, selectionConstraint);
            _client.PositionElementInFormation(element, slot);
        }

        private FormationStationSlotInfo SelectAndRecordSlotAsOccupied(IUnitElement element, FormationStationSelectionCriteria selectionConstraint) {
            bool isRemoved;
            FormationStationSlotInfo slotInfo;
            if (_occupiedStationSlotLookup.TryGetValue(element, out slotInfo)) {
                // return element's existing slotInfo BEFORE selecting another
                isRemoved = _occupiedStationSlotLookup.Remove(element);
                D.Assert(isRemoved, element.DebugName);
                _availableStationSlots.Add(slotInfo);
                D.Assert(_availableStationSlots.Count <= _currentFormation.MaxFormationSlots(),
                    "{0}: {1} > Max {2}.".Inject(DebugName, _availableStationSlots.Count, _currentFormation.MaxFormationSlots()));
            }
            slotInfo = SelectSlotInfoFor(element, selectionConstraint);
            isRemoved = _availableStationSlots.Remove(slotInfo);
            D.Assert(isRemoved, slotInfo.DebugName);
            _occupiedStationSlotLookup.Add(element, slotInfo);
            D.Assert(_occupiedStationSlotLookup.Count <= _currentFormation.MaxFormationSlots(),
                "{0}: {1} > Max {2}.".Inject(DebugName, _occupiedStationSlotLookup.Count, _currentFormation.MaxFormationSlots()));
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
            D.Assert(!_availableStationSlots.Where(sInfo => sInfo.IsHQSlot).Any());    // HQ slot should never be available here
            return _availableStationSlots.Where(sInfo => sInfo.IsReserve == selectionConstraint.IsReserveReqd).Any();
        }

        /// <summary>
        /// Restores the formation slot associated with this element (including the HQ Element) to available status.
        /// WARNING: This does not include removing the FormationStation from the ship and the ship
        /// from the FormationStation, nor does it deal with returning the Station to the pool.
        /// </summary>
        /// <param name="element">The element.</param>
        public void RestoreSlotToAvailable(IUnitElement element) {
            FormationStationSlotInfo elementStationInfo;
            bool isStationSlotFound = _occupiedStationSlotLookup.TryGetValue(element, out elementStationInfo);
            D.Assert(isStationSlotFound, element.DebugName);
            _occupiedStationSlotLookup.Remove(element);
            _availableStationSlots.Add(elementStationInfo);
            D.Assert(_availableStationSlots.Count <= _currentFormation.MaxFormationSlots(),
                "{0}: {1} > Max {2}.".Inject(DebugName, _availableStationSlots.Count, _currentFormation.MaxFormationSlots()));
        }

        /// <summary>
        /// Returns <c>true</c> if this FormationManager currently has station slots available for an additional <c>elementCount</c> elements.
        /// </summary>
        /// <param name="additionalElementCount">The additional element count.</param>
        /// <returns></returns>
        public bool HasRoomFor(int additionalElementCount) {
            return _occupiedStationSlotLookup.Count + additionalElementCount <= _currentFormation.MaxFormationSlots();
        }

        #region Debug

        [System.Diagnostics.Conditional("DEBUG")]
        private void __ValidateSingleHqSlotAvailable() {
            int count = _availableStationSlots.Where(sInfo => sInfo.IsHQSlot).Count();
            if (count != Constants.One) {
                D.Error("{0}: Expecting 1 HQ formation slot but found {1}. Formation = {2}, AvailableSlots = {3}, OccupiedSlots = {4}.",
                    DebugName, count, _currentFormation.GetValueName(), _availableStationSlots.Concatenate(), _occupiedStationSlotLookup.Values.Concatenate());
            }
        }

        #endregion

        public sealed override string ToString() {
            return DebugName;
        }

        #region Nested Classes

        /// <summary>
        /// Mutable struct that holds the criteria used for the selection of FormationStations.
        /// WARNING: Not suitable for dictionary keys.
        /// </summary>
        public struct FormationStationSelectionCriteria : IEquatable<FormationStationSelectionCriteria> {

            private const string ToStringFormat = "{0}: isReserveReqd = {1}";

            #region Comparison Operators Override

            // see C# 4.0 In a Nutshell, page 254

            public static bool operator ==(FormationStationSelectionCriteria left, FormationStationSelectionCriteria right) {
                return left.Equals(right);
            }

            public static bool operator !=(FormationStationSelectionCriteria left, FormationStationSelectionCriteria right) {
                return !left.Equals(right);
            }

            #endregion

            public bool IsReserveReqd { get; set; }

            #region Object.Equals and GetHashCode Override

            public override bool Equals(object obj) {
                if (!(obj is FormationStationSelectionCriteria)) { return false; }
                return Equals((FormationStationSelectionCriteria)obj);
            }

            /// <summary>
            /// Returns a hash code for this instance.
            /// See "Page 254, C# 4.0 in a Nutshell."
            /// </summary>
            /// <returns>
            /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
            /// </returns>
            public override int GetHashCode() {
                unchecked { // http://dobrzanski.net/2010/09/13/csharp-gethashcode-cause-overflowexception/
                    int hash = 17;  // 17 = some prime number
                    hash = hash * 31 + IsReserveReqd.GetHashCode(); // 31 = another prime number
                    return hash;
                }
            }

            #endregion


            public override string ToString() {
                return ToStringFormat.Inject(typeof(FormationStationSelectionCriteria).Name, IsReserveReqd);
            }

            #region IEquatable<FormationStationSelectionCriteria> Members

            public bool Equals(FormationStationSelectionCriteria other) {
                return IsReserveReqd == other.IsReserveReqd;
            }

            #endregion

        }

        #endregion
    }
}


