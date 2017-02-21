// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: INavTaskClient.cs
// COMMENT - one line to give a brief idea of what the file does.
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
    using CodeEnv.Master.GameContent;
    using UnityEngine;

    /// <summary>
    /// 
    /// </summary>
    public interface INavTaskClient {

        Speed ApSpeed { get; }

        Speed CurrentSpeedSetting { get; }

        float UnitFullSpeedValue { get; }

        float FullSpeedValue { get; }

        bool IsApCurrentSpeedFleetwide { get; }

        Vector3 IntendedHeading { get; }

        string DebugName { get; }

        bool ShowDebugLog { get; }

        Vector3 Position { get; }

        void ChangeHeading_Internal(Vector3 newHeading, Action headingConfirmed = null);

        void ChangeSpeed_Internal(Speed newSpeed, bool isFleetSpeed);

        void HandleCourseChanged();

        void RefreshCourse(CourseRefreshMode mode, AutoPilotDestinationProxy wayPtProxy = null);

        void ResumeDirectCourseToTarget();

    }
}

