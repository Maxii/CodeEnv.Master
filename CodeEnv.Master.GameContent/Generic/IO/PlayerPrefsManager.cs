// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerPrefsManager.cs
// Manages saving and acquiring player preference values via UnityEngine.PlayerPrefs. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

//#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using System;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Manages saving and acquiring player preference values via UnityEngine.PlayerPrefs. Save default location on disk:
    /// Windows Standalone players: HKCU\Software[company name][product name] key, where company and product names are the names set up in Project Settings.
    /// Windows Web players: %APPDATA%\Unity\WebPlayerPrefs
    /// </summary>
    [SerializeAll]
    public class PlayerPrefsManager : AGenericSingleton<PlayerPrefsManager>, IInstanceCount {

        private string _universeSizeKey = "Universe Size";
        private string _userPlayerSpeciesKey = "User Player Species";
        private string _userPlayerColorKey = "User Player Color";

        private string _gameSpeedOnLoadKey = "Game Speed On Load";
        private string _isZoomOutOnCursorEnabledKey = "Zoom Out On Cursor";
        private string _isCameraRollEnabledKey = "Camera Roll";
        private string _isResetOnFocusEnabledKey = "Reset On Focus";
        private string _isPauseAfterLoadEnabledKey = "Paused On Load";
        private string _isElementIconsEnabledKey = "Element Icons";
        private string _qualitySettingKey = "Quality Setting";

        // WARNING: Changing the name of a Property here requires a comensurate change in the name returned by GuiMenuElementIDExtensions
        // TODO: notifications are not needed for properties that cannot change during a game instance
        public UniverseSizeGuiSelection UniverseSizeSelection { get; set; }

        public SpeciesGuiSelection UserPlayerSpeciesSelection { get; set; }
        public GameColor UserPlayerColor { get; set; }
        public GameSpeed GameSpeedOnLoad { get; private set; }

        public bool IsPauseOnLoadEnabled { get; private set; }

        private bool _isZoomOutOnCursorEnabled;
        public bool IsZoomOutOnCursorEnabled {
            get { return _isZoomOutOnCursorEnabled; }
            set { SetProperty<bool>(ref _isZoomOutOnCursorEnabled, value, "IsZoomOutOnCursorEnabled"); }
        }

        private bool _isCameraRollEnabled;
        public bool IsCameraRollEnabled {
            get { return _isCameraRollEnabled; }
            set { SetProperty<bool>(ref _isCameraRollEnabled, value, "IsCameraRollEnabled"); }
        }

        private bool _isResetOnFocusEnabled;
        public bool IsResetOnFocusEnabled {
            get { return _isResetOnFocusEnabled; }
            set { SetProperty<bool>(ref _isResetOnFocusEnabled, value, "IsResetOnFocusEnabled"); }
        }

        private bool _isElementIconsEnabled;
        public bool IsElementIconsEnabled {
            get { return _isElementIconsEnabled; }
            set { SetProperty<bool>(ref _isElementIconsEnabled, value, "IsElementIconsEnabled"); }
        }

        private string _qualitySetting;
        public string QualitySetting {
            get { return _qualitySetting; }
            set { SetProperty<string>(ref _qualitySetting, value, "QualitySetting"); }
        }

        private IGameManager _gameMgr;
        private GeneralSettings _generalSettings;

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="PlayerPrefsManager"/>.
        /// </summary>
        private PlayerPrefsManager() {
            Initialize();
        }

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        protected override void Initialize() {
            IncrementInstanceCounter();
            _generalSettings = GeneralSettings.Instance;
            _gameMgr = References.GameManager;
            Retrieve();
        }

        public void RecordGamePlayOptions(GamePlayOptionSettings settings) {
            GameSpeedOnLoad = settings.GameSpeedOnLoad;
            IsZoomOutOnCursorEnabled = settings.IsZoomOutOnCursorEnabled;
            //D.Log("At OptionChangeEvent, PlayerPrefsMgr.IsZoomOutOnCursorEnabled = " + IsZoomOutOnCursorEnabled);
            IsResetOnFocusEnabled = settings.IsResetOnFocusEnabled;
            IsCameraRollEnabled = settings.IsCameraRollEnabled;
            IsPauseOnLoadEnabled = settings.IsPauseOnLoadEnabled;
            ValidateState();
        }

        public void RecordGraphicsOptions(GraphicsOptionSettings settings) {
            if (!QualitySetting.Equals(settings.QualitySetting)) {  // HACK avoids property equal warning
                QualitySetting = settings.QualitySetting;
            }
            IsElementIconsEnabled = settings.IsElementIconsEnabled;
        }

        /// <summary>
        /// Stores all PlayerPrefs to disk.
        /// </summary>
        public void Store() {
            // if variable not null/empty or None, convert the value to a string, encrypt it, and using the key, set it 
            string encryptedStringValue = string.Empty;
            if (UniverseSizeSelection != UniverseSizeGuiSelection.None) {
                encryptedStringValue = Encrypt(UniverseSizeSelection.GetName());
                PlayerPrefs.SetString(_universeSizeKey, encryptedStringValue);
            }
            if (GameSpeedOnLoad != GameSpeed.None) {
                encryptedStringValue = Encrypt(GameSpeedOnLoad.GetName());
                PlayerPrefs.SetString(_gameSpeedOnLoadKey, encryptedStringValue);
            }
            if (UserPlayerSpeciesSelection != SpeciesGuiSelection.None) {
                encryptedStringValue = Encrypt(UserPlayerSpeciesSelection.GetName());
                PlayerPrefs.SetString(_userPlayerSpeciesKey, encryptedStringValue);
            }
            if (UserPlayerColor != GameColor.None) {
                encryptedStringValue = Encrypt(UserPlayerColor.GetName());
                PlayerPrefs.SetString(_userPlayerColorKey, encryptedStringValue);
            }
            PlayerPrefs.SetString(_isZoomOutOnCursorEnabledKey, Encrypt(IsZoomOutOnCursorEnabled.ToString()));
            PlayerPrefs.SetString(_isCameraRollEnabledKey, Encrypt(IsCameraRollEnabled.ToString()));
            PlayerPrefs.SetString(_isResetOnFocusEnabledKey, Encrypt(IsResetOnFocusEnabled.ToString()));
            PlayerPrefs.SetString(_isPauseAfterLoadEnabledKey, Encrypt(IsPauseOnLoadEnabled.ToString()));
            PlayerPrefs.SetString(_isElementIconsEnabledKey, Encrypt(IsElementIconsEnabled.ToString()));
            //D.Log("At Store, PlayerPrefsMgr.IsElementIconsEnabled = " + IsElementIconsEnabled);

            PlayerPrefs.SetString(_qualitySettingKey, Encrypt(QualitySetting)); // changed from SetInt(key, value)
            PlayerPrefs.Save();
        }

        // IMPROVE combine key and value into a delimited string, then encrypt/decript   
        private string Encrypt(string item) { return item; }
        private float Encrypt(float value) { return value; }
        private int Encrypt(int value) { return value; }

        /// <summary>
        /// Retrieves all PlayerPrefs and makes them accessible as Properties from this instance. This is where
        /// we set the value at initial startup (there is no preference recorded to disk yet) rather than relying on the default value.
        /// </summary>
        /// <remarks>I considered externalizing these initial startup values, but as they are only used once, there is little value to the modder
        /// having access to them.
        /// </remarks>
        public void Retrieve() {
            D.Log("{0}.Retrieve() called.", GetType().Name);
            UniverseSizeSelection = PlayerPrefs.HasKey(_universeSizeKey) ? RetrieveEnumPref<UniverseSizeGuiSelection>(_universeSizeKey) : UniverseSizeGuiSelection.Normal;
            //D.Log("GameSpeedOnLoad = {0} before retrieval.", GameSpeedOnLoad);
            GameSpeedOnLoad = PlayerPrefs.HasKey(_gameSpeedOnLoadKey) ? RetrieveEnumPref<GameSpeed>(_gameSpeedOnLoadKey) : GameSpeed.Normal;
            //D.Log("GameSpeedOnLoad = {0} after retrieval.", GameSpeedOnLoad);
            UserPlayerSpeciesSelection = PlayerPrefs.HasKey(_userPlayerSpeciesKey) ? RetrieveEnumPref<SpeciesGuiSelection>(_userPlayerSpeciesKey) : SpeciesGuiSelection.Human;
            UserPlayerColor = PlayerPrefs.HasKey(_userPlayerColorKey) ? RetrieveEnumPref<GameColor>(_userPlayerColorKey) : GameColor.Blue;

            IsPauseOnLoadEnabled = (PlayerPrefs.HasKey(_isPauseAfterLoadEnabledKey)) ? bool.Parse(Decrypt(PlayerPrefs.GetString(_isPauseAfterLoadEnabledKey))) : false;

            // the initial change notification sent out by these Properties occur so early they won't be heard by anyone so clients must initialize by calling the Properties directly
            IsZoomOutOnCursorEnabled = (PlayerPrefs.HasKey(_isZoomOutOnCursorEnabledKey)) ? bool.Parse(Decrypt(PlayerPrefs.GetString(_isZoomOutOnCursorEnabledKey))) : true;
            IsCameraRollEnabled = (PlayerPrefs.HasKey(_isCameraRollEnabledKey)) ? bool.Parse(Decrypt(PlayerPrefs.GetString(_isCameraRollEnabledKey))) : false;
            IsResetOnFocusEnabled = (PlayerPrefs.HasKey(_isResetOnFocusEnabledKey)) ? bool.Parse(Decrypt(PlayerPrefs.GetString(_isResetOnFocusEnabledKey))) : true;
            IsElementIconsEnabled = (PlayerPrefs.HasKey(_isElementIconsEnabledKey)) ? bool.Parse(Decrypt(PlayerPrefs.GetString(_isElementIconsEnabledKey))) : true;
            QualitySetting = (PlayerPrefs.HasKey(_qualitySettingKey)) ? Decrypt(PlayerPrefs.GetString(_qualitySettingKey)) : QualitySettings.names[QualitySettings.GetQualityLevel()];
        }

        private T RetrieveEnumPref<T>(string key) where T : struct {
            string decryptedStringValue = Decrypt(PlayerPrefs.GetString(key));
            T pref;
            if (!Enums<T>.TryParse(decryptedStringValue, out pref)) {
                D.Error("Unable to parse Preference {0} of Type {1}.", decryptedStringValue, typeof(T));
            }
            return pref;
        }

        // IMPROVE combine key and value into a delimited string, then encrypt/decript
        private string Decrypt(string item) { return item; }
        private float Decrypt(float value) { return value; }
        private int Decrypt(int value) { return value; }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void ValidateState() {
            // Grab the name of the calling method
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            string callerIdMessage = " Called by {0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);

            D.Assert(UniverseSizeSelection != UniverseSizeGuiSelection.None, callerIdMessage + "UniverseSize selection cannot be None.", true);
            D.Assert(GameSpeedOnLoad != GameSpeed.None, callerIdMessage + "GameSpeedOnLoad cannot be None.", true);
        }

        #endregion

        #region IInstanceIdentity Members

        private static int _instanceCounter = 0;
        public int InstanceCount { get; private set; }

        private void IncrementInstanceCounter() {
            InstanceCount = System.Threading.Interlocked.Increment(ref _instanceCounter);
        }

        #endregion

    }
}


