﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2015 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AIntelItemDisplayInfoFactory.cs
// Abstract generic base factory that makes instances of text containing info about IntelItems.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;

    /// <summary>
    /// Abstract generic base factory that makes instances of text containing info about IntelItems.
    /// </summary>
    public abstract class AIntelItemDisplayInfoFactory<ReportType, FactoryType> : AItemDisplayInfoFactory<ReportType, FactoryType>
        where ReportType : AIntelItemReport
        where FactoryType : AIntelItemDisplayInfoFactory<ReportType, FactoryType> {

        protected override bool TryMakeColorizedText(ItemInfoID infoID, ReportType report, out string colorizedText) {
            bool isSuccess = base.TryMakeColorizedText(infoID, report, out colorizedText);
            if (!isSuccess) {
                if (infoID == ItemInfoID.IntelState) {
                    isSuccess = true;
                    colorizedText = ConstructIntelText(report.Intel);
                }
            }
            return isSuccess;
        }

        private string ConstructIntelText(AIntel intel) {
            string intelMsg = intel.CurrentCoverage.GetValueName();
            string addendum = ". Intel is current.";
            var intelWithDatedCoverage = intel as RegressibleIntel;
            if (intelWithDatedCoverage != null && intelWithDatedCoverage.IsDatedCoverageValid) {
                //D.Log("DateStamp = {0}, CurrentDate = {1}.", intelWithDatedCoverage.DateStamp, GameTime.Instance.CurrentDate);
                GameTimeDuration intelAge = new GameTimeDuration(intelWithDatedCoverage.DateStamp, GameTime.Instance.CurrentDate);
                addendum = String.Format(". Intel age {0}.", intelAge.ToString());
            }
            intelMsg = intelMsg + addendum;
            //D.Log(intelMsg);
            return _lineTemplate.Inject(intelMsg);
        }

    }
}

