// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2018 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: APopupDialogForm.cs
// Abstract base class for Dialog Forms that popup.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

////#define DEBUG_LOG
////#define DEBUG_WARN
////#define DEBUG_ERROR

// default namespace

using CodeEnv.Master.Common;
using CodeEnv.Master.GameContent;

/// <summary>
/// Abstract base class for Dialog Forms that popup.
/// </summary>
public abstract class APopupDialogForm : AForm {

    private DialogSettings _settings;
    public DialogSettings Settings {
        get { return _settings; }
        set {
            D.AssertNull(_settings);    // occurs only once between Resets
            _settings = value;
            __Validate(value);
        }
    }

    public sealed override void PopulateValues() {  // Called by AFormWindow just before window shows form
        AssignValuesToMembers();
    }

    protected override void AssignValuesToMembers() {
        DeactivateAllMenuControls();
        InitializeMenuControls();
    }

    protected abstract void InitializeMenuControls();

    protected abstract void UnsubscribeFromMenuControls();

    protected abstract void DeactivateAllMenuControls();

    protected override void ResetForReuse_Internal() {
        if (Settings != null) {
            UnsubscribeFromMenuControls();
            _settings = null;
        }
        DeactivateAllMenuControls();
    }

    protected override void Cleanup() {
        if (Settings != null) {
            UnsubscribeFromMenuControls();
        }
    }

    #region Debug

    protected abstract void __Validate(DialogSettings settings);

    #endregion

    #region Nested Classes

    /// <summary>
    /// Flexible class for specifying the settings in a DialogForm.
    /// </summary>
    public class DialogSettings {

        public bool ShowCancelButton { get; set; }
        public EventDelegate CancelButtonDelegate { get; set; }

        public bool ShowDoneButton { get; set; }
        public EventDelegate DoneButtonDelegate { get; set; }

        public bool ShowAcceptButton { get; set; }
        public EventDelegate AcceptButtonDelegate { get; set; }

        private Player _player = TempGameValues.NoPlayer;
        public Player Player {
            get { return _player; }
            set { _player = value; }
        }

        public string Title { get; set; }

        public AtlasID IconAtlasID { get; set; }

        public string IconFilename { get; set; }

        public string Text { get; set; }

        public string DebugName { get { return GetType().Name; } }

        public DialogSettings() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="DialogSettings"/> class.
        /// <remarks>This version is for a simple textual dialog with a done button.</remarks>
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="text">The text.</param>
        /// <param name="doneDelegate">The done delegate.</param>
        public DialogSettings(string title, string text, EventDelegate doneDelegate) {
            Title = title;
            Text = text;
            ShowDoneButton = true;
            DoneButtonDelegate = doneDelegate;
        }

        public DialogSettings(Player player, EventDelegate acceptDelegate, EventDelegate cancelDelegate) {
            Player = player;
            ShowAcceptButton = true;
            AcceptButtonDelegate = acceptDelegate;
            ShowCancelButton = true;
            CancelButtonDelegate = cancelDelegate;
        }

        public override string ToString() {
            return DebugName;
        }

    }

    #endregion


}

