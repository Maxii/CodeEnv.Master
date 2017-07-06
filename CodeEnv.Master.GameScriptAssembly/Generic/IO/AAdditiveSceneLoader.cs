// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2017 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AAdditiveSceneLoader.cs
// Abstract base class for the Loader (and unloader) of an Additive Scene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine.SceneManagement;

/// <summary>
/// Abstract base class for the Loader (and unloader) of an Additive Scene.
/// <remarks>6.15.17 Not currently used.</remarks>
/// </summary>
public abstract class AAdditiveSceneLoader {

    public string DebugName { get { return GetType().Name; } }

    protected abstract SceneID SceneID { get; }

    public void Load() {
        SceneManager.LoadScene((int)SceneID, LoadSceneMode.Additive);
    }

    public void Unload() {
        SceneManager.UnloadSceneAsync((int)SceneID);
    }

    public sealed override string ToString() {
        return DebugName;
    }

}

