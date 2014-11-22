// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: JobRunner.cs
// Singleton. A MonoBehaviour-based proxy for executing Jobs as Coroutines.
// Derived from P31 Job Manager.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System;
using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. A MonoBehaviour-based proxy for executing Jobs as Coroutines.
/// Derived from P31 Job Manager.
/// </summary>
public class JobRunner : AMonoSingleton<JobRunner>, IJobRunner {

    protected override bool IsPersistentAcrossScenes { get { return true; } }

    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        Job.jobRunner = Instance;
    }

    protected override void Cleanup() {
        Job.jobRunner = null;
    }

    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }

}


