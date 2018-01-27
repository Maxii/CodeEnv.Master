// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: SettlementDisplayInfoFactory.cs
// Factory that makes instances of text containing info about Settlements.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using CodeEnv.Master.Common.LocalResources;

    /// <summary>
    /// Factory that makes instances of text containing info about Settlements.
    /// </summary>
    public class SettlementDisplayInfoFactory : AUnitCmdDisplayInfoFactory<SettlementCmdReport, SettlementDisplayInfoFactory> {

        private static ItemInfoID[] _infoIDsToDisplay = new ItemInfoID[] {
            //ItemInfoID.Name,
            ItemInfoID.UnitName,
            ItemInfoID.Category,
            ItemInfoID.Composition,
            ItemInfoID.Owner,
            ItemInfoID.Hero,
            ItemInfoID.SectorID,
            ItemInfoID.AlertStatus,

           //ItemInfoID.CurrentCmdEffectiveness,
            ItemInfoID.Formation,
            //ItemInfoID.UnitOffense,
            //ItemInfoID.UnitDefense,
            ItemInfoID.UnitHealth,
            //ItemInfoID.UnitWeaponsRange,
            ItemInfoID.UnitSensorRange,
            //ItemInfoID.OrderDirective,

            ItemInfoID.UnitOutputs,

            ItemInfoID.CurrentConstruction,
            //ItemInfoID.Capacity,

            ItemInfoID.Resources,

            //ItemInfoID.Population,
            //ItemInfoID.Approval,

            ItemInfoID.Separator,

            ItemInfoID.IntelState,

            ItemInfoID.Separator,

            ItemInfoID.CameraDistance
        };

        protected override ItemInfoID[] OrderedInfoIDsToDisplay { get { return _infoIDsToDisplay; } }

        private SettlementDisplayInfoFactory() {
            Initialize();
        }

        protected sealed override void Initialize() { }

        protected override bool TryMakeColorizedText(ItemInfoID infoID, SettlementCmdReport report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                switch (infoID) {
                    case ItemInfoID.Category:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Category != SettlementCategory.None ? report.Category.GetValueName() : Unknown);
                        break;
                    case ItemInfoID.Composition:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.UnitComposition != null ? report.UnitComposition.ToString() : Unknown);
                        break;
                    case ItemInfoID.Capacity:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Capacity.HasValue ? GetFormat(infoID).Inject(report.Capacity.Value) : Unknown);
                        break;
                    case ItemInfoID.Resources:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Resources != default(ResourcesYield) ? report.Resources.ToString() : Unknown);
                        break;
                    case ItemInfoID.Population:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.Population.HasValue ? GetFormat(infoID).Inject(report.Population.Value) : Unknown);
                        break;
                    case ItemInfoID.Approval:
                        isSuccess = true;
                        colorizedText = GetColorizedApprovalText(report.Approval);
                        break;
                    case ItemInfoID.CurrentConstruction:
                        isSuccess = true;
                        colorizedText = GetCurrentConstructionText(report.CurrentConstruction);
                        break;
                    case ItemInfoID.OrderDirective:
                        isSuccess = true;
                        colorizedText = _lineTemplate.Inject(report.__OrderDirective.GetValueName());
                        break;
                    default:
                        throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(infoID));
                }
            }
            return isSuccess;
        }

    }
}

