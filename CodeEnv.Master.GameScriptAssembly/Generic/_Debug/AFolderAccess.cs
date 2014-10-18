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

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Generic singleton abstract class providing static access to the folder named after T.
/// </summary>
public abstract class AFolderAccess<T> : AMonoBaseSingleton<T> where T : AMonoBase {

    private static string _folderName = typeof(T).Name;

    /// <summary>
    /// Gets the folder.
    /// </summary>
    public Transform Folder {
        get {
            if (gameObject.name != _folderName) {
                D.Error("Expecting folder {0} but got {1}.", _folderName, gameObject.name);
            }
            // As this is a singleton, it can be called using Instance.Folder before Awake() is run
            return _transform ?? transform;
        }

    }

}

