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

        private string _universeSizeKey = "Universe Size Preference";
        private string _playerRaceKey = "Player Race Preference";
        private string _playerColorKey = "Player Color Preference";

        private string _gameSpeedOnLoadKey = "Game Speed On Load Option";
        private string _isZoomOutOnCursorEnabledKey = "Zoom Out On Cursor Option";
        private string _isCameraRollEnabledKey = "Camera Roll Option";
        private string _isResetOnFocusEnabledKey = "Reset On Focus Option";
        private string _isPauseAfterLoadEnabledKey = "Paused On Load Option";
        private string _isElementIconsEnabledKey = "Element Icons Option";
        private string _qualitySettingKey = "Quality Setting Option";

        // notifications not needed as no change will be allowed to affect an existing game instance    // TODO
        public UniverseSize UniverseSize { get; private set; }
        public GameClockSpeed GameSpeedOnLoad { get; private set; }
        public Species PlayerRace { get; private set; }
        public GameColor PlayerColor { get; private set; }

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

        private int _qualitySetting;
        public int QualitySetting {
            get { return _qualitySetting; }
            set { SetProperty<int>(ref _qualitySetting, value, "QualitySetting"); }
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
            Subscribe();
        }

        private void Subscribe() {
            _gameMgr.onNewGameBuilding += OnNewGameBuilding;
        }

        private void OnNewGameBuilding() {
            var settings = _gameMgr.GameSettings;
            UniverseSize = settings.UniverseSize;
            PlayerRace = settings.PlayerRace.Species;
            PlayerColor = settings.PlayerRace.Color;
            ValidateState();
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
            QualitySetting = settings.QualitySetting;
            IsElementIconsEnabled = settings.IsElementIconsEnabled;
        }

        /// <summary>
        /// Stores all PlayerPrefs to disk.
        /// </summary>
        public void Store() {
            // if variable not null/empty or None, convert the value to a string, encrypt it, and using the key, set it 
            string encryptedStringValue = string.Empty;
            if (UniverseSize != UniverseSize.None) {
                encryptedStringValue = Encrypt(UniverseSize.GetName());
                PlayerPrefs.SetString(_universeSizeKey, encryptedStringValue);
            }
            if (GameSpeedOnLoad != GameClockSpeed.None) {
                encryptedStringValue = Encrypt(GameSpeedOnLoad.GetName());
                PlayerPrefs.SetString(_gameSpeedOnLoadKey, encryptedStringValue);
            }
            if (PlayerRace != Species.None) {
                encryptedStringValue = Encrypt(PlayerRace.GetName());
                PlayerPrefs.SetString(_playerRaceKey, encryptedStringValue);
            }
            if (PlayerColor != GameColor.None) {
                encryptedStringValue = Encrypt(PlayerColor.GetName());
                PlayerPrefs.SetString(_playerColorKey, encryptedStringValue);
            }
            PlayerPrefs.SetString(_isZoomOutOnCursorEnabledKey, Encrypt(IsZoomOutOnCursorEnabled.ToString()));
            PlayerPrefs.SetString(_isCameraRollEnabledKey, Encrypt(IsCameraRollEnabled.ToString()));
            PlayerPrefs.SetString(_isResetOnFocusEnabledKey, Encrypt(IsResetOnFocusEnabled.ToString()));
            PlayerPrefs.SetString(_isPauseAfterLoadEnabledKey, Encrypt(IsPauseOnLoadEnabled.ToString()));
            PlayerPrefs.SetString(_isElementIconsEnabledKey, Encrypt(IsElementIconsEnabled.ToString()));
            //D.Log("At Store, PlayerPrefsMgr.IsElementIconsEnabled = " + IsElementIconsEnabled);

            PlayerPrefs.SetInt(_qualitySettingKey, Encrypt(QualitySetting));
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
            UniverseSize = PlayerPrefs.HasKey(_universeSizeKey) ? RetrieveEnumPref<UniverseSize>(_universeSizeKey) : UniverseSize.Normal;
            //D.Log("GameSpeedOnLoad = {0} before retrieval.", GameSpeedOnLoad);
            GameSpeedOnLoad = PlayerPrefs.HasKey(_gameSpeedOnLoadKey) ? RetrieveEnumPref<GameClockSpeed>(_gameSpeedOnLoadKey) : GameClockSpeed.Normal;
            //D.Log("GameSpeedOnLoad = {0} after retrieval.", GameSpeedOnLoad);
            PlayerRace = PlayerPrefs.HasKey(_playerRaceKey) ? RetrieveEnumPref<Species>(_playerRaceKey) : Species.Human;
            PlayerColor = PlayerPrefs.HasKey(_playerColorKey) ? RetrieveEnumPref<GameColor>(_playerColorKey) : GameColor.Blue;

            IsPauseOnLoadEnabled = (PlayerPrefs.HasKey(_isPauseAfterLoadEnabledKey)) ? bool.Parse(Decrypt(PlayerPrefs.GetString(_isPauseAfterLoadEnabledKey))) : false;

            // the initial change notification sent out by these Properties occur so early they won't be heard by anyone so clients must initialize by calling the Properties directly
            IsZoomOutOnCursorEnabled = (PlayerPrefs.HasKey(_isZoomOutOnCursorEnabledKey)) ? bool.Parse(Decrypt(PlayerPrefs.GetString(_isZoomOutOnCursorEnabledKey))) : true;
            IsCameraRollEnabled = (PlayerPrefs.HasKey(_isCameraRollEnabledKey)) ? bool.Parse(Decrypt(PlayerPrefs.GetString(_isCameraRollEnabledKey))) : false;
            IsResetOnFocusEnabled = (PlayerPrefs.HasKey(_isResetOnFocusEnabledKey)) ? bool.Parse(Decrypt(PlayerPrefs.GetString(_isResetOnFocusEnabledKey))) : true;
            IsElementIconsEnabled = (PlayerPrefs.HasKey(_isElementIconsEnabledKey)) ? bool.Parse(Decrypt(PlayerPrefs.GetString(_isElementIconsEnabledKey))) : true;
            QualitySetting = (PlayerPrefs.HasKey(_qualitySettingKey)) ? Decrypt(PlayerPrefs.GetInt(_qualitySettingKey)) : QualitySettings.GetQualityLevel();
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

        private void Cleanup() {
            Unsubscribe();
        }

        private void Unsubscribe() {
            _gameMgr.onNewGameBuilding -= OnNewGameBuilding;
        }

        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region IDisposable
        private bool alreadyDisposed = false;

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources. Derived classes that need to perform additional resource cleanup
        /// should override this Dispose(isDisposing) method, using its own alreadyDisposed flag to do it before calling base.Dispose(isDisposing).
        /// </summary>
        /// <param name="isDisposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool isDisposing) {
            // Allows Dispose(isDisposing) to be called more than once
            if (alreadyDisposed) {
                return;
            }

            if (isDisposing) {
                // free managed resources here including unhooking events
                Cleanup();
            }
            // free unmanaged resources here
            alreadyDisposed = true;
        }


        // Example method showing check for whether the object has been disposed
        //public void ExampleMethod() {
        //    // throw Exception if called on object that is already disposed
        //    if(alreadyDisposed) {
        //        throw new ObjectDisposedException(ErrorMessages.ObjectDisposed);
        //    }

        //    // method content here
        //}
        #endregion

        #region Debug

        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        private void ValidateState() {
            // Grab the name of the calling method
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            string callerIdMessage = " Called by {0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);

            D.Assert(UniverseSize != UniverseSize.None, callerIdMessage + " SizeOfUniverse cannot be None.", true);
            D.Assert(GameSpeedOnLoad != GameClockSpeed.None, callerIdMessage + "GameSpeedOnLoad cannot be None.", true);
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


