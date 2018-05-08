// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: DialogWindow.cs
// Singleton. COMMENT - one line to give a brief idea of what this file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using System;
using System.Collections.Generic;
using System.Linq;
using CodeEnv.Master.Common;
using CodeEnv.Master.Common.LocalResources;
using CodeEnv.Master.GameContent;
using UnityEngine;

/// <summary>
/// Singleton. AGuiWindow that handles dialogs of various types that popup in the center of the screen.
/// <remarks>Operates as a modal popup that blocks all other interaction with the screen through the use of a 
/// 'Blocking Background and Collider'. Allows for dedicated forms that contain their own menu controls.</remarks>
/// <remarks>Also automatically handles pausing - RequestsPause onShowBegin, RequestsUnpause onHideComplete.</remarks>
/// </summary>
public class DialogWindow : AHudWindow<DialogWindow> {

    protected override void InitializeValuesAndReferences() {
        base.InitializeValuesAndReferences();
        if (!_panel.widgetsAreStatic) {
            // Widget Buttons and/or EnvelopContent move and scale
        }
    }

    public void Show(FormID formID, DialogSettings settings) {
        var form = PrepareForm(formID);
        (form as APopupDialogForm).Settings = settings;
        ShowForm(form);
    }

    protected override void PositionWindow() {
        // do nothing as Window is anchored
    }

    protected override void HandleShowBegin() {
        base.HandleShowBegin();
        _gameMgr.RequestPauseStateChange(toPause: true);
    }

    protected override void HandleHideComplete() {
        base.HandleHideComplete();
        // Calls ResetForReuse from base class AGuiWindow
        _gameMgr.RequestPauseStateChange(toPause: false);
    }

    #region Nested Classes

    public class DialogSettings {

        public EventDelegate CancelButtonDelegate { get; set; }

        public EventDelegate DoneButtonDelegate { get; set; }

        public EventDelegate AcceptButtonDelegate { get; set; }

        public bool ShowCancelButton { get; set; }

        public bool ShowDoneButton { get; set; }

        public bool ShowAcceptButton { get; set; }

        public string DebugName { get { return GetType().Name; } }

        public string Title { get; set; }

        public AtlasID IconAtlasID { get; set; }

        public string IconFilename { get; set; }

        public string Text { get; set; }

        public DialogSettings() { }

        public override string ToString() {
            return DebugName;
        }

    }

    #endregion

}

