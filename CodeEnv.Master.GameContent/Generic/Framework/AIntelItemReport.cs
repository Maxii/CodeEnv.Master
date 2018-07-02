// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIntelItemReport.cs
// Abstract class for Reports that support Items with PlayerIntel.
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

    /// <summary>
    ///  Abstract class for Reports that support Items with PlayerIntel.
    /// </summary>
    public abstract class AIntelItemReport : AItemReport {

        private const string DebugNameFormat = "{0}[{1}]";

        private static IList<ResourcesYield.ResourceValuePair> _resourcesToInclude = new List<ResourcesYield.ResourceValuePair>();
        private static IList<OutputsYield.OutputValuePair> _outputsToInclude = new List<OutputsYield.OutputValuePair>();

        public override string DebugName { get { return DebugNameFormat.Inject(base.DebugName, IntelCoverage.GetValueName()); } }

        public AIntel Intel { get; private set; }

        public IntelCoverage IntelCoverage { get { return Intel.CurrentCoverage; } }

        public AIntelItemReport(AIntelItemData data, Player player) : base(data, player) {
            Intel = data.GetIntelCopy(player);
            AssignValues(data);
            // IntelCoverage.None can occur as reports are requested when an element/cmd loses all IntelCoverage and the Cmd re-evaluates its icon
        }

        #region Assess Awareness of Resources 

        /// <summary>
        /// Assesses what Resources the Report is allowed to show and returns that value. Assessment is based on 
        /// 1) whether the player's technology is sufficient to have discovered a Resource, and 
        /// 2) whether the player's IntelCoverage is sufficient to see a Resource when tech is sufficient.
        /// <remarks>Virtual to allow SystemReport to override and throw an error if used. SystemReport cannot use
        /// this method with its own Resources from SystemData as SystemReport should only show what the reports
        /// from the members of the system are allowed to show. Use of this method for SystemReport may result in 
        /// a different result than calculating from System member reports if IsAwarenessOfPresenceAllowed(resID) is
        /// ever overridden by SystemReport, StarReport or PlanetoidReport.
        /// </remarks>
        /// </summary>
        /// <param name="dataResources">The resources taken from Data which by definition, have no filtering applied
        /// from either Technology or IntelCoverage. This value will always be comprehensive.</param>
        /// <returns></returns>
        protected virtual ResourcesYield AssessResources(ResourcesYield dataResources) {
            _resourcesToInclude.Clear();
            PlayerAIManager aiMgr = GameReferences.GameManager.GetAIManagerFor(Player);
            IEnumerable<ResourceID> resIDsPresent = dataResources.ResourcesPresent;
            foreach (var resID in resIDsPresent) {
                if (aiMgr.__IsTechSufficientForAwarenessOf(resID)) {
                    if (IsAwarenessOfPresenceAllowed(resID)) {
                        float? yield = IsAwarenessOfValueAllowed(resID) ? dataResources.GetYield(resID) : null;
                        _resourcesToInclude.Add(new ResourcesYield.ResourceValuePair(resID, yield));
                    }
                }
            }
            if (_resourcesToInclude.Any()) {
                //D.Log("{0} is including resources: {1}.", DebugName, _resourcesToInclude.Concatenate());
                return new ResourcesYield(_resourcesToInclude.ToArray());
            }
            return default(ResourcesYield);
        }

        /// <summary>
        /// Returns <c>true</c> if Player is allowed to be aware of the presence of the provided ResourceID
        /// based on Player's IntelCoverage of this Item.
        /// <remarks>Should be used only after Player has discovered the requisite tech to know about the resource.</remarks>
        /// <remarks>6.12.18 Currently overridden by SystemReport to throw an error if used.</remarks>
        /// </summary>
        /// <param name="resourceID">The resource identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected virtual bool IsAwarenessOfPresenceAllowed(ResourceID resourceID) {
            switch (resourceID) {
                case ResourceID.Organics:
                case ResourceID.Particulates:
                case ResourceID.Energy:
                    return IntelCoverage >= IntelCoverage.Basic;
                case ResourceID.Titanium:
                    return IntelCoverage >= IntelCoverage.Essential;
                case ResourceID.Duranium:
                    return IntelCoverage >= IntelCoverage.Broad;
                case ResourceID.Unobtanium:
                    return IntelCoverage >= IntelCoverage.Comprehensive;
                case ResourceID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        /// <summary>
        /// Returns <c>true</c> if Player is allowed to be aware of the value (aka 4.0) of the provided ResourceID
        /// based on Player's IntelCoverage of this Item.
        /// <remarks>Should be used only after Player is aware of the presence of the resource.</remarks>
        /// </summary>
        /// <param name="resourceID">The resource identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected virtual bool IsAwarenessOfValueAllowed(ResourceID resourceID) {
            switch (resourceID) {
                case ResourceID.Organics:
                case ResourceID.Particulates:
                case ResourceID.Energy:
                    return IntelCoverage >= IntelCoverage.Essential;
                case ResourceID.Titanium:
                    return IntelCoverage >= IntelCoverage.Broad;
                case ResourceID.Duranium:
                    return IntelCoverage >= IntelCoverage.Comprehensive;
                case ResourceID.Unobtanium:
                    return IntelCoverage >= IntelCoverage.Comprehensive;
                case ResourceID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(resourceID));
            }
        }

        #endregion

        #region Assess Awareness of Outputs

        protected OutputsYield AssessOutputs(OutputsYield dataOutputs) {
            _outputsToInclude.Clear();
            IEnumerable<OutputID> outputIDs = dataOutputs.OutputsPresent;
            foreach (var id in outputIDs) {
                if (IsAwarenessOfPresenceAllowed(id)) {
                    float? yield = IsAwarenessOfValueAllowed(id) ? dataOutputs.GetYield(id) : null;
                    _outputsToInclude.Add(new OutputsYield.OutputValuePair(id, yield));
                }
            }
            if (_outputsToInclude.Any()) {
                //D.Log("{0} is including outputs: {1}.", DebugName, _outputsToInclude.Concatenate());
                return new OutputsYield(_outputsToInclude.ToArray());
            }
            return default(OutputsYield);
        }

        /// <summary>
        /// Returns <c>true</c> if Player is allowed to be aware of the presence of the provided OutputID
        /// based on Player's IntelCoverage of this Item.
        /// </summary>
        /// <param name="outputID">The output identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected virtual bool IsAwarenessOfPresenceAllowed(OutputID outputID) {
            switch (outputID) {
                case OutputID.Food:
                    return IntelCoverage >= IntelCoverage.Broad;
                case OutputID.Production:
                    return IntelCoverage >= IntelCoverage.Broad;
                case OutputID.NetIncome:
                case OutputID.Income:
                    // Income and NetIncome should always be the same
                    return IntelCoverage >= IntelCoverage.Broad;
                case OutputID.Expense:
                    return IntelCoverage >= IntelCoverage.Broad;
                case OutputID.Science:
                    return IntelCoverage >= IntelCoverage.Broad;
                case OutputID.Culture:
                    return IntelCoverage >= IntelCoverage.Broad;
                case OutputID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outputID));
            }
        }

        /// <summary>
        /// Returns <c>true</c> if Player is allowed to be aware of the value (aka 4.0) of the provided OutputID
        /// based on Player's IntelCoverage of this Item.
        /// <remarks>Should be used only after Player is aware of the presence of the output.</remarks>
        /// </summary>
        /// <param name="outputID">The output identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        protected virtual bool IsAwarenessOfValueAllowed(OutputID outputID) {
            switch (outputID) {
                case OutputID.Food:
                    return IntelCoverage >= IntelCoverage.Comprehensive;
                case OutputID.Production:
                    return IntelCoverage >= IntelCoverage.Comprehensive;
                case OutputID.NetIncome:
                case OutputID.Income:
                    // Income and NetIncome should always be the same
                    return IntelCoverage >= IntelCoverage.Comprehensive;
                case OutputID.Expense:
                    return IntelCoverage >= IntelCoverage.Comprehensive;
                case OutputID.Science:
                    return IntelCoverage >= IntelCoverage.Comprehensive;
                case OutputID.Culture:
                    return IntelCoverage >= IntelCoverage.Comprehensive;
                case OutputID.None:
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(outputID));
            }
        }

        #endregion

        #region Archive

        //private void AssignValues(AItemData data) {
        //    switch (IntelCoverage) {
        //        case IntelCoverage.Comprehensive:
        //            AssignIncrementalValues_IntelCoverageComprehensive(data);
        //            goto case IntelCoverage.Broad;
        //        case IntelCoverage.Broad:
        //            AssignIncrementalValues_IntelCoverageBroad(data);
        //            goto case IntelCoverage.Essential;
        //        case IntelCoverage.Essential:
        //            AssignIncrementalValues_IntelCoverageEssential(data);
        //            goto case IntelCoverage.Basic;
        //        case IntelCoverage.Basic:
        //            AssignIncrementalValues_IntelCoverageBasic(data);
        //            goto case IntelCoverage.None;
        //        case IntelCoverage.None:
        //            AssignIncrementalValues_IntelCoverageNone(data);
        //            break;
        //        default:
        //            throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(IntelCoverage));
        //    }
        //}

        //protected virtual void AssignIncrementalValues_IntelCoverageComprehensive(AItemData data) { }
        //protected virtual void AssignIncrementalValues_IntelCoverageBroad(AItemData data) { }
        //protected virtual void AssignIncrementalValues_IntelCoverageEssential(AItemData data) { }
        //protected virtual void AssignIncrementalValues_IntelCoverageBasic(AItemData data) { }
        //protected virtual void AssignIncrementalValues_IntelCoverageNone(AItemData data) { }

        #endregion
    }
}

