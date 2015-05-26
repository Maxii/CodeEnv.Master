// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerPrefsManager.cs
// Singleton. Manages saving and acquiring player preference values via UnityEngine.PlayerPrefs. 
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LOG
#define DEBUG_WARN
#define DEBUG_ERROR

namespace CodeEnv.Master.GameContent {

    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// Singleton. Manages saving and acquiring player preference values via UnityEngine.PlayerPrefs. 
    /// Save default location on disk:
    /// Windows Standalone players: HKCU\Software[company name][product name] key, 
    /// where company and product names are the names set up in Project Settings.
    /// Windows Web players: %APPDATA%\Unity\WebPlayerPrefs
    /// </summary>
    public class PlayerPrefsManager : AGenericSingleton<PlayerPrefsManager>, IInstanceCount {

        private string _universeSizeKey = "Universe Size";
        private string _userPlayerSpeciesKey = "User Player Species";
        private string _userPlayerColorKey = "User Player Color";

        private string _gameSpeedOnLoadKey = "Game Speed On Load";
        private string _isZoomOutOnCursorEnabledKey = "Zoom Out On Cursor";
        private string _isCameraRollEnabledKey = "Camera Roll";
        private string _isResetOnFocusEnabledKey = "Reset On Focus";
        private string _isPauseOnLoadEnabledKey = "Paused On Load";
        private string _isElementIconsEnabledKey = "Element Icons";
        private string _qualitySettingKey = "Quality Setting";

        // WARNING: Changing the name of a Property here requires a comensurate change in the name returned by GuiMenuElementIDExtensions
        // Notifications are not needed for properties that cannot change during a game instance
        public UniverseSizeGuiSelection UniverseSizeSelection { get; private set; }

        public SpeciesGuiSelection UserPlayerSpeciesSelection { get; private set; }
        public GameColor UserPlayerColor { get; private set; }
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

        public void RecordNewGameSettings(GameSettings gameSettings) {
            UniverseSizeSelection = gameSettings.UniverseSizeSelection;
            UserPlayerSpeciesSelection = gameSettings.UserPlayerSpeciesSelection;
            UserPlayerColor = gameSettings.UserPlayerColor;
        }

        /// <summary>
        /// Stores all PlayerPrefs to disk.
        /// </summary>
        public void Store() {
            StoreEnumPref<UniverseSizeGuiSelection>(_universeSizeKey, UniverseSizeSelection);
            StoreEnumPref<GameSpeed>(_gameSpeedOnLoadKey, GameSpeedOnLoad);
            StoreEnumPref<SpeciesGuiSelection>(_userPlayerSpeciesKey, UserPlayerSpeciesSelection);
            StoreEnumPref<GameColor>(_userPlayerColorKey, UserPlayerColor);

            StoreBooleanPref(_isZoomOutOnCursorEnabledKey, IsZoomOutOnCursorEnabled);
            StoreBooleanPref(_isCameraRollEnabledKey, IsCameraRollEnabled);
            StoreBooleanPref(_isResetOnFocusEnabledKey, IsResetOnFocusEnabled);
            StoreBooleanPref(_isPauseOnLoadEnabledKey, IsPauseOnLoadEnabled);
            StoreBooleanPref(_isElementIconsEnabledKey, IsElementIconsEnabled);

            StoreStringPref(_qualitySettingKey, QualitySetting);
            PlayerPrefs.Save();
        }

        private void StoreBooleanPref(string key, bool value) {
            PlayerPrefs.SetString(key, Encrypt(value));
        }

        private void StoreStringPref(string key, string value) {
            Arguments.ValidateForContent(value);
            PlayerPrefs.SetString(key, Encrypt(value));
        }

        private void StoreIntPref(string key, int value) {
            PlayerPrefs.SetInt(key, Encrypt(value));
        }

        private void StoreFloatPref(string key, float value) {
            PlayerPrefs.SetFloat(key, Encrypt(value));
        }

        private void StoreEnumPref<T>(string key, T value) where T : struct {
            if (!Enums<T>.IsDefined(value)) {
                D.Error("Undefined value {0} of EnumType {1}.", value.ToString(), typeof(T));
            }
            if (!value.Equals(default(T))) {
                PlayerPrefs.SetString(key, Encrypt(value.ToString()));
            }
        }

        private string Encrypt(string value) { return value; }
        private float Encrypt(float value) { return value; }
        private int Encrypt(int value) { return value; }
        private string Encrypt(bool value) { return value.ToString(); }

        /// <summary>
        /// Retrieves all PlayerPrefs and makes them accessible as Properties from this instance. This is where
        /// we set the value at initial startup (there is no preference recorded to disk yet) rather than relying on the default value.
        /// </summary>
        /// <remarks>I considered externalizing these initial startup values, but as they are only used once, there is little value to the modder
        /// having access to them.
        /// </remarks>
        public void Retrieve() {
            D.Log("{0}.Retrieve() called.", GetType().Name);
            UniverseSizeSelection = RetrieveEnumPref<UniverseSizeGuiSelection>(_universeSizeKey, UniverseSizeGuiSelection.Normal);
            GameSpeedOnLoad = RetrieveEnumPref<GameSpeed>(_gameSpeedOnLoadKey, GameSpeed.Normal);
            UserPlayerSpeciesSelection = RetrieveEnumPref<SpeciesGuiSelection>(_userPlayerSpeciesKey, SpeciesGuiSelection.Human);
            UserPlayerColor = RetrieveEnumPref<GameColor>(_userPlayerColorKey, GameColor.Blue);

            IsPauseOnLoadEnabled = RetrieveBooleanPref(_isPauseOnLoadEnabledKey, false);

            // the initial change notification sent out by these Properties occur so early they won't be heard by anyone so clients must initialize by calling the Properties directly
            IsZoomOutOnCursorEnabled = RetrieveBooleanPref(_isZoomOutOnCursorEnabledKey, true);
            IsCameraRollEnabled = RetrieveBooleanPref(_isCameraRollEnabledKey, false);
            IsResetOnFocusEnabled = RetrieveBooleanPref(_isResetOnFocusEnabledKey, true);
            IsElementIconsEnabled = RetrieveBooleanPref(_isElementIconsEnabledKey, true);
            QualitySetting = RetrieveStringPref(_qualitySettingKey, QualitySettings.names[QualitySettings.GetQualityLevel()]);
        }

        private bool RetrieveBooleanPref(string key, bool defaultValue) {
            return PlayerPrefs.HasKey(key) ? DecryptToBool(PlayerPrefs.GetString(key)) : defaultValue;
        }

        private string RetrieveStringPref(string key, string defaultValue) {
            return PlayerPrefs.HasKey(key) ? DecryptToString(PlayerPrefs.GetString(key)) : defaultValue;
        }

        private float RetrieveFloatPref(string key, float defaultValue) {
            return PlayerPrefs.HasKey(key) ? DecryptToFloat(PlayerPrefs.GetFloat(key)) : defaultValue;
        }

        private int RetrieveIntPref(string key, int defaultValue) {
            return PlayerPrefs.HasKey(key) ? DecryptToInt(PlayerPrefs.GetInt(key)) : defaultValue;
        }

        private T RetrieveEnumPref<T>(string key, T defaultValue) where T : struct {
            if (PlayerPrefs.HasKey(key)) {
                string decryptedStringValue = DecryptToString(PlayerPrefs.GetString(key));
                T pref;
                if (!Enums<T>.TryParse(decryptedStringValue, out pref)) {
                    D.Error("Unable to parse Preference {0} of Type {1}.", decryptedStringValue, typeof(T));
                }
                return pref;
            }
            return defaultValue;
        }

        private string DecryptToString(string value) { return value; }
        private float DecryptToFloat(float value) { return value; }
        private int DecryptToInt(int value) { return value; }
        private bool DecryptToBool(string value) { return bool.Parse(value); }

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


