// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AAutoPilotTask.cs
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
    public abstract class AAutoPilotTask {

        private const string DebugNameFormat = "{0}.{1}";

        public abstract bool IsEngaged { get; }



        protected string DebugName { get { return DebugNameFormat.Inject(_taskClient.DebugName, GetType().Name); } }

        protected bool ShowDebugLog { get { return _taskClient.ShowDebugLog; } }


        protected INavTaskClient _taskClient;
        protected IJobManager _jobMgr;

        public AAutoPilotTask(INavTaskClient taskClient) {
            _taskClient = taskClient;
            InitializeValuesAndReferences();
        }

        protected virtual void InitializeValuesAndReferences() {
            _jobMgr = References.JobManager;
        }

        public abstract void RunTask(AutoPilotDestinationProxy destProxy, Action onCompletion = null);

        public virtual void ResetForReuse() {
            KillJob();
        }

        protected abstract void KillJob();

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

    }
}

