// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AFolderAccess.cs
// Generic abstract class providing static access to the folder named after T.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using UnityEngine;

/// <summary>
/// Generic abstract class providing static access to the folder named after T.
/// </summary>
public abstract class AFolderAccess<T> : AMonoBaseSingleton<T> where T : AMonoBase {

    private static string _folderName = typeof(T).Name;

    /// <summary>
    /// Gets the folder.
    /// </summary>
    public static Transform Folder {
        get {
            if (Instance.gameObject.name != _folderName) {
                D.Error("Expecting folder {0} but got {1}.", _folderName, Instance.gameObject.name);
            }
            return (Instance as AFolderAccess<T>)._transform;
        }
    }

}

