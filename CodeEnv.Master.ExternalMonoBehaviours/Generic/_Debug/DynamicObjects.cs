// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DynamicObjects.cs
// Singleton for easy access to DynamicObjects folder in startScene.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR



// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Singleton for easy access to DynamicObjects folder in startScene.
/// </summary>
public class DynamicObjects : MonoBehaviour {

    /// <summary>
    /// Gets the DynamicObjects folder.
    /// </summary>
    /// <value>
    /// The folder.
    /// </value>
    public static Transform Folder {
        get {
            if (Instance.gameObject.name != TempGameValues.DynamicObjectsFolderName) {
                D.Error("Expecting folder {0} but got {1}.", TempGameValues.DynamicObjectsFolderName, Instance.gameObject.name);
            }
            return Instance.transform;
        }
    }

    #region Custom MonoBehaviour Singleton Pattern
    private static DynamicObjects instance = null;
    public static DynamicObjects Instance {
        get {
            if (instance == null) {
                // values is required for the first time, so look for it
                instance = GameObject.FindObjectOfType(typeof(DynamicObjects)) as DynamicObjects;
                if (instance == null) {
                    // no instance created yet, so create one
                    GameObject dynamicObjectsFolder = GameObject.Find(TempGameValues.DynamicObjectsFolderName);
                    if (dynamicObjectsFolder != null) {
                        // if our destination folder exists, add our newly created instance to it
                        instance = dynamicObjectsFolder.AddComponent<DynamicObjects>();
                    }
                    else {
                        // DynamicObjects folder isn't in the startScene, so create it
                        D.Warn("No DynamicObjects folder found, so creating one.");
                        dynamicObjectsFolder = new GameObject(TempGameValues.DynamicObjectsFolderName, typeof(DynamicObjects));
                        instance = dynamicObjectsFolder.GetComponent<DynamicObjects>();
                        if (instance == null) {
                            D.Error("Problem during the creation of {0}.", typeof(DynamicObjects).ToString());
                        }
                    }
                }
                instance.Initialize();
            }
            return instance;
        }
    }

    void Awake() {
        // If no other MonoBehaviour has requested values in an Awake() call executing
        // before this one, then we are it. There is no reason to search for an object as we must be attached to it.
        if (instance == null) {
            instance = this;
            instance.Initialize();
        }
    }

    // Make sure values isn't referenced anymore
    void OnApplicationQuit() {
        instance = null;
    }
    #endregion

    private void Initialize() {
        // do any required initialization here as you would normally do in Awake()
        D.Log("A {0} instance is being initialized.", this.name);
    }

    void OnDestroy() {
        D.Log("A {0} instance is being destroyed.", this.name);
    }


    public override string ToString() {
        return new ObjectAnalyzer().ToString(this);
    }
}


