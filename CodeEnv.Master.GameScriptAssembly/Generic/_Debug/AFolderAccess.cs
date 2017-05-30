// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFolderAccess.cs
// Generic singleton abstract class providing static access to the folder named after T.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Generic singleton abstract class providing static access to the folder named after T.
/// </summary>
public abstract class AFolderAccess<T> : AMonoSingleton<T> where T : AFolderAccess<T> {

    public string DebugName { get { return typeof(T).Name; } }

    protected virtual string FolderName { get { return typeof(T).Name; } }

    /// <summary>
    /// Gets the folder for this Instance.
    /// Note: As Folder can only be called using Instance.Folder, it is guaranteed to return the right transform.
    /// WARNING: Do not call from within derived classes as use of Instance.Folder is not required then.
    /// </summary>
    public Transform Folder {
        get {
            if (gameObject.name != FolderName) {
                D.Warn("Expecting folder named {0} but got {1}.", FolderName, gameObject.name);
            }
            // As this is a singleton, it can be called using Instance.Folder before Awake() is run
            //return transform ?? transform;
            return transform;
        }
    }

    public sealed override string ToString() {
        return DebugName;
    }

}

