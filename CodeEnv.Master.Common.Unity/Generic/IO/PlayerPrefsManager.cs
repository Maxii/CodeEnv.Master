// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2013 Strategic Forge
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: PlayerPrefsManager.cs
// SingletonPattern. COMMENT - one line to give a brief idea of what the file does.
// </summary> 
// -------------------------------------------------------------------------------------------------------------------- 

#define DEBUG_LEVEL_LOG
#define DEBUG_LEVEL_WARN
#define DEBUG_LEVEL_ERROR


namespace CodeEnv.Master.Common.Unity {

    using System;
    using System.Diagnostics;
    using CodeEnv.Master.Common;
    using UnityEngine;

    /// <summary>
    /// SingletonPattern. COMMENT
    /// </summary>
    [SerializeAll]
    public class PlayerPrefsManager : AInstanceIdentity, IInstanceIdentity {

        private string sizeOfUniverseKey = "Universe Size Preference";
        private string gameSpeedOnLoadKey = "Game Speed On Load Option";
        private string isZoomOutOnCursorEnabledKey = "Zoom Out On Cursor Option";
        private string isCameraRollEnabledKey = "Camera Roll Option";
        private string isResetOnFocusEnabledKey = "Reset On Focus Option";
        private string isPauseAfterLoadEnabledKey = "Paused On Load Option";


        public UniverseSize SizeOfUniverse { get; private set; }
        public GameClockSpeed GameSpeedOnLoad { get; private set; }
        public bool IsZoomOutOnCursorEnabled { get; private set; }
        public bool IsCameraRollEnabled { get; private set; }
        public bool IsResetOnFocusEnabled { get; private set; }
        public bool IsPauseOnLoadEnabled { get; private set; }

        private GameEventManager eventMgr;

        #region SingletonPattern
        private static readonly PlayerPrefsManager instance;

        /// <summary>
        /// Explicit static constructor that enables lazy instantiation by telling C# compiler
        /// not to mark type as beforefieldinit.
        /// </summary>
        static PlayerPrefsManager() {
            // try, catch and resolve any possible exceptions here
            instance = new PlayerPrefsManager();
        }

        /// <summary>
        /// Private constructor that prevents the creation of another externally requested instance of <see cref="PlayerPrefsManager"/>.
        /// </summary>
        private PlayerPrefsManager() {
            Initialize();
        }

        /// <summary>Returns the singleton instance of this class.</summary>
        public static PlayerPrefsManager Instance {
            get { return instance; }
        }
        #endregion

        ///<summary>
        /// Called once from the constructor, this does all required initialization
        /// </summary>
        private void Initialize() {
            IncrementInstanceCounter();
            Retrieve();
            eventMgr = GameEventManager.Instance;
            eventMgr.AddListener<OptionChangeEvent>(this, OnOptionChange);
            eventMgr.AddListener<BuildNewGameEvent>(this, OnBuildNewGame);
        }

        private void OnBuildNewGame(BuildNewGameEvent e) {
            GameSettings settings = e.Settings;
            SizeOfUniverse = settings.SizeOfUniverse;
            ValidateState();
        }

        private void OnOptionChange(OptionChangeEvent e) {
            OptionSettings settings = e.Settings;
            GameSpeedOnLoad = settings.GameSpeedOnLoad;
            IsZoomOutOnCursorEnabled = settings.IsZoomOutOnCursorEnabled;
            //D.Log("At OptionChangeEvent, PlayerPrefsMgr.IsZoomOutOnCursorEnabled = " + IsZoomOutOnCursorEnabled);
            IsResetOnFocusEnabled = settings.IsResetOnFocusEnabled;
            IsCameraRollEnabled = settings.IsCameraRollEnabled;
            IsPauseOnLoadEnabled = settings.IsPauseOnLoadEnabled;
            ValidateState();
        }

        /// <summary>
        /// Stores all PlayerPrefs to disk.
        /// </summary>
        public void Store() {
            // if variable not null/empty or None, convert the value to a string, encrypt it, and using the key, set it 
            string encryptedStringValue = string.Empty;
            if (SizeOfUniverse != UniverseSize.None) {
                encryptedStringValue = Encrypt(SizeOfUniverse.GetName());
                PlayerPrefs.SetString(sizeOfUniverseKey, encryptedStringValue);
            }
            if (GameSpeedOnLoad != GameClockSpeed.None) {
                encryptedStringValue = Encrypt(GameSpeedOnLoad.GetName());
                PlayerPrefs.SetString(gameSpeedOnLoadKey, encryptedStringValue);
            }
            //D.Log("At Store, PlayerPrefsMgr.IsZoomOutOnCursorEnabled = " + IsZoomOutOnCursorEnabled);
            PlayerPrefs.SetString(isZoomOutOnCursorEnabledKey, Encrypt(IsZoomOutOnCursorEnabled.ToString()));
            PlayerPrefs.SetString(isCameraRollEnabledKey, Encrypt(IsCameraRollEnabled.ToString()));

            PlayerPrefs.SetString(isResetOnFocusEnabledKey, Encrypt(IsResetOnFocusEnabled.ToString()));
            PlayerPrefs.SetString(isPauseAfterLoadEnabledKey, Encrypt(IsPauseOnLoadEnabled.ToString()));
            PlayerPrefs.Save();
        }

        private string Encrypt(string item) {
            return item;    // UNDONE combine key and value into a delimited string, then encrypt/decript
        }

        /// <summary>
        /// Retrieves all PlayerPrefs and makes them accessible as Properties from this instance.
        /// </summary>
        public void Retrieve() {
            // for enums, if there is no preference set yet, default would be NONE, so start with NORMAL
            SizeOfUniverse = (PlayerPrefs.HasKey(sizeOfUniverseKey)) ? RetrieveEnumPref<UniverseSize>(sizeOfUniverseKey) : UniverseSize.Normal;
            GameSpeedOnLoad = (PlayerPrefs.HasKey(gameSpeedOnLoadKey)) ? RetrieveEnumPref<GameClockSpeed>(gameSpeedOnLoadKey) : GameClockSpeed.Normal;

            if (PlayerPrefs.HasKey(isZoomOutOnCursorEnabledKey)) {
                IsZoomOutOnCursorEnabled = bool.Parse(Decrypt(PlayerPrefs.GetString(isZoomOutOnCursorEnabledKey)));
                //D.Log("At Retrieve, PlayerPrefsMgr.IsZoomOutOnCursorEnabled = " + IsZoomOutOnCursorEnabled);
            }
            if (PlayerPrefs.HasKey(isCameraRollEnabledKey)) {
                IsCameraRollEnabled = bool.Parse(Decrypt(PlayerPrefs.GetString(isCameraRollEnabledKey)));
            }
            if (PlayerPrefs.HasKey(isResetOnFocusEnabledKey)) {
                IsResetOnFocusEnabled = bool.Parse(Decrypt(PlayerPrefs.GetString(isResetOnFocusEnabledKey)));
            }
            if (PlayerPrefs.HasKey(isPauseAfterLoadEnabledKey)) {
                IsPauseOnLoadEnabled = bool.Parse(Decrypt(PlayerPrefs.GetString(isPauseAfterLoadEnabledKey)));
            }
        }

        private T RetrieveEnumPref<T>(string key) where T : struct {
            string decryptedStringValue = Decrypt(PlayerPrefs.GetString(key));
            T pref;
            if (!Enums<T>.TryParse(decryptedStringValue, out pref)) {
                D.Error("Unable to parse Preference {0} of Type {1}.", decryptedStringValue, typeof(T));
            }
            return pref;
        }

        private string Decrypt(string encryptedItem) {
            return encryptedItem;   // UNDONE combine key and value into a delimited string, then encrypt/decript
        }

        void OnDestroy() {
            Dispose();
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
                eventMgr.RemoveListener<OptionChangeEvent>(this, OnOptionChange);
                eventMgr.RemoveListener<BuildNewGameEvent>(this, OnBuildNewGame);
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


        public override string ToString() {
            return new ObjectAnalyzer().ToString(this);
        }

        #region Debug

        [Conditional("UNITY_EDITOR")]
        private void ValidateState() {
            // Grab the name of the calling method
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackTrace().GetFrame(1);
            string callerIdMessage = " Called by {0}.{1}().".Inject(stackFrame.GetFileName(), stackFrame.GetMethod().Name);

            D.Assert(SizeOfUniverse != UniverseSize.None, callerIdMessage + " SizeOfUniverse cannot be None.", true);
            D.Assert(GameSpeedOnLoad != GameClockSpeed.None, callerIdMessage + "GameSpeedOnLoad cannot be None.", true);
        }

        #endregion

    }
}


