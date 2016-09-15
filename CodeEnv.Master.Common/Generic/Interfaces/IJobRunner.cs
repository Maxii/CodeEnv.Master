// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: IJobRunner.cs
// Interface that allows Job to hold a reference to the MonoBehaviour that executes Job Coroutines.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.Common {

    using System.Collections;
    using UnityEngine;

    /// <summary>
    /// Interface that allows Job to hold a reference to the MonoBehaviour that executes Job Coroutines.
    /// </summary>
    public interface IJobRunner {

        Coroutine StartCoroutine(IEnumerator coroutine);

        void StopCoroutine(IEnumerator coroutine);


    }
}

