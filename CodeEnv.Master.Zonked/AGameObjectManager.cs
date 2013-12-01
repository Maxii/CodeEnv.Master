// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: AGameObjectManager.cs
// Abstract generic base class for all managers of 3D interactable objects in the game.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

// default namespace

using System.Collections.Generic;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.Unity;
using UnityEngine;

/// <summary>
/// Abstract generic base class for all managers of 3D interactable objects in the game.
/// </summary>
public abstract class AGameObjectManager<DataType> : AMonoBase, INotifyVisibilityChanged where DataType : Data {

    private static GuiHudLineKeys[] _noOptionalCursorHudLinesToUpdate = new GuiHudLineKeys[0];

    public virtual float CurrentSpeed { get { return Constants.ZeroF; } }
    public Vector3 CurrentPosition { get { return Data.Position; } }
    public IntelLevel HumanPlayerIntelLevel { get; set; }
    public DataType Data { get; set; }

    protected GuiHudText _guiCursorHudText;
    protected Transform _transform;
    protected GuiCursorHud _cursorHud;
    public bool IsVisible { get; set; }

    private IList<Transform> _visibleMeshes = new List<Transform>();    // OPTIMIZE can be simplified to simple incrementing/decrementing counter


    void Awake() {
        InitializeOnAwake();
        IsVisible = true;
    }

    protected virtual void InitializeOnAwake() {
        _transform = transform;
        _cursorHud = GuiCursorHud.Instance;
    }

    void Start() {
        InitializeOnStart();
    }

    protected virtual void InitializeOnStart() {
        HumanPlayerIntelLevel = __InitializeIntelLevel();
    }

    /// <summary>
    /// Initializes the IntelLevel for this manager. Default implementation is IntelLevel.Complete.
    /// </summary>
    /// <returns></returns>
    protected virtual IntelLevel __InitializeIntelLevel() {
        return HumanPlayerIntelLevel != IntelLevel.Nil ? HumanPlayerIntelLevel : IntelLevel.Complete;
    }

    /// <summary>
    /// Initializes the AData for this manager. Clients are responsible for doing this in the right sequence as 
    /// one manager can be dependant on another managers data.
    /// </summary>
    protected abstract void __InitializeData();

    public void DisplayCursorHUD() {        // OPTIMIZE Detect individual data property changes and replace them individually
        if (_guiCursorHudText == null || _guiCursorHudText.IntelLevel != HumanPlayerIntelLevel || Data.IsChanged) {
            // don't have the right version of GuiCursorHudText so make one
            _guiCursorHudText = GuiHudTextFactory.MakeInstance(HumanPlayerIntelLevel, Data);
            Data.AcceptChanges();   // once we make a new one from current data, it is no longer dirty, if it ever was
        }
        else {
            // we have the right clean version so simply update the values that routinely change
            UpdateGuiCursorHudText(GuiHudLineKeys.Distance);
            if (HumanPlayerIntelLevel == IntelLevel.OutOfDate) {
                UpdateGuiCursorHudText(GuiHudLineKeys.IntelState);
            }
            if (OptionalCursorHudLinesToUpdate().Length != 0) {
                UpdateGuiCursorHudText(OptionalCursorHudLinesToUpdate());
            }
        }
        _cursorHud.Set(_guiCursorHudText);
    }

    /// <summary>
    /// Clients can optionally provide additional GuiCursorHudLineKeys they wish to routinely update whenever DisplayCursorHUD is called.
    ///LineKeys already currently handled for all managers include Distance and IntelState. If the client does not override this method, it returns 
    ///an empty array of line keys.
    /// </summary>
    ///<returns></returns>
    protected virtual GuiHudLineKeys[] OptionalCursorHudLinesToUpdate() {
        return _noOptionalCursorHudLinesToUpdate;
    }

    /// <summary>
    /// Updates the current GuiCursorHudText instance by replacing the lines identified by keys.
    /// </summary>
    /// <param name="keys">The line keys.</param>
    protected abstract void UpdateGuiCursorHudText(params GuiHudLineKeys[] keys);

    public void ClearCursorHUD() {
        _cursorHud.Clear();
    }

    void Update() {
        if (ToUpdate()) {
            OnToUpdate();
        }
    }

    /// <summary>
    /// Called by Update() at a frequency determined by UpdateRate.
    /// </summary>
    protected virtual void OnToUpdate() {
        OptimizeDisplay();
    }

    protected abstract void OptimizeDisplay();

    private void ProcessMeshVisibilityChanged(Transform sender, bool isVisible) {
        if (isVisible) {
            D.Assert(!_visibleMeshes.Contains(sender));
            _visibleMeshes.Add(sender);
        }
        else {
            if (!_visibleMeshes.Remove(sender)) {
                D.Warn("{0} was not removed from VisibleMeshes.", sender.name);
            }
        }
        if (IsVisible == (_visibleMeshes.Count == 0)) {
            // visibility state is changing
            IsVisible = !IsVisible;
            Logger.Log("{0} isVisible changed to {1}.", gameObject.name, IsVisible);
            OptimizeDisplay();
        }
    }

    #region INotifyVisibilityChanged Members

    public void NotifyVisibilityChanged(Transform sender, bool isVisible) {
        ProcessMeshVisibilityChanged(sender, isVisible);
    }

    #endregion
}

