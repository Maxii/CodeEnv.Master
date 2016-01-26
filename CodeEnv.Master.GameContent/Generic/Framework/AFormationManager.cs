// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFormationManager.cs
// Abstract base class for Unit Formation Generators.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System.Collections.Generic;
    using System.Linq;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Abstract base class for Unit Formation Generators.
    /// </summary>
    public abstract class AFormationManager {

        protected abstract int MaxElementCountPerUnit { get; }

        private Formation _currentFormation;
        private IList<Vector3> _nonHQFormationStationOffsets;
        private IDictionary<IUnitElementItem, Vector3> _occupiedFormationStationOffsetLookup;
        private IFormationMgrClient _unitCmd;

        public AFormationManager(IFormationMgrClient unitCmd) {
            _unitCmd = unitCmd;
            // initializing _occupiedFormationOffsetLookup here generates CS2214 warnings.
        }

        /// <summary>
        /// Repositions all the Unit elements in the formation designated in the Cmd's Data.
        /// Repositioning is accomplished by calling Cmd.PositionElementInFormation().
        /// </summary>
        /// <param name="allElements">All elements.</param>
        public void RepositionAllElementsInFormation(IList<IUnitElementItem> allElements) {
            if (_occupiedFormationStationOffsetLookup == null) {
                _occupiedFormationStationOffsetLookup = new Dictionary<IUnitElementItem, Vector3>(MaxElementCountPerUnit);
            }
            _occupiedFormationStationOffsetLookup.Clear();
            Formation formation = _unitCmd.Data.UnitFormation;
            if (formation != _currentFormation) {
                float maxFormationRadius;
                _nonHQFormationStationOffsets = GenerateFormationStationOffsets(formation, out maxFormationRadius);
                _unitCmd.Data.UnitMaxFormationRadius = maxFormationRadius;
                _currentFormation = formation;
            }
            Stack<Vector3> nonHQAvailableFormationOffsets = new Stack<Vector3>(_nonHQFormationStationOffsets);
            allElements.ForAll(e => {
                Vector3 formationStationOffset = e.IsHQ ? Vector3.zero : nonHQAvailableFormationOffsets.Pop();
                _unitCmd.PositionElementInFormation(e, formationStationOffset);
                _occupiedFormationStationOffsetLookup.Add(e, formationStationOffset);
            });
            _unitCmd.CleanupAfterFormationChanges();
        }

        protected abstract IList<Vector3> GenerateFormationStationOffsets(Formation formation, out float maxFormationRadius);

        /// <summary>
        /// Adds the provided non_HQ element to the formation and positions it properly.
        /// </summary>
        /// <param name="element">The element.</param>
        public void AddAndPositionNonHQElement(IUnitElementItem element) {
            D.Assert(!element.IsHQ);
            D.Assert(!_occupiedFormationStationOffsetLookup.ContainsKey(element));
            Stack<Vector3> availableNonHQFormationStationOffsets = new Stack<Vector3>(_nonHQFormationStationOffsets.Except(_occupiedFormationStationOffsetLookup.Values));
            var availableFormationStationOffset = availableNonHQFormationStationOffsets.Pop();
            _unitCmd.PositionElementInFormation(element, availableFormationStationOffset);
            _occupiedFormationStationOffsetLookup.Add(element, availableFormationStationOffset);
        }

        /// <summary>
        /// Removes the provided element from the formation. The element's position is
        /// not changed by this operation.
        /// </summary>
        /// <param name="element">The element.</param>
        public void RemoveElement(IUnitElementItem element) {
            var isElementRemoved = _occupiedFormationStationOffsetLookup.Remove(element);
            D.Assert(isElementRemoved);
        }

    }
}

