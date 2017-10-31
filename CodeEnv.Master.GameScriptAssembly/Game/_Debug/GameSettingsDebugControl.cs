// --------------------------------------------------------------------------------------------------------------------
// <copyright>
// Copyright © 2012 - 2016 
//
// Email: jim@strategicforge.com
// </copyright> 
// <summary> 
// File: GameSettingsDebugControl.cs
// Singleton. Editor Debug controls for GameSettings.
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
/// Singleton. Editor Debug controls for GameSettings.
/// </summary>
public class GameSettingsDebugControl : AMonoSingleton<GameSettingsDebugControl>, IGameSettingsDebugControl {

    // Notes: Has custom editor that uses NguiEditorTools and SerializedObject. 
    // Allows concurrent use of [Tooltip("")]. NguiEditorTools do not offer a separate tooltip option because this concurrent use is allowed.
    // [Header("")] can also be used concurrently, but this can also be done in the custom editor with greater location precision.

    #region Editor Fields

    [Tooltip("Set the size of the Universe")]
    [SerializeField]
    private DebugUniverseSize _universeSize = DebugUniverseSize.Normal;

    [Tooltip("The max number of players allowed for this size of universe")]
    [SerializeField]
    private int _maxPlayerCount = TempGameValues.MaxPlayers;

    [Tooltip("Set the number of players in the game")]
    [SerializeField]
    private int _playerCount = 5;


    [Tooltip("Check to only use the existing System and Unit DebugCreators")]
    [SerializeField]
    private bool _useDebugCreatorsOnly = false; // if true, the choices below will be disabled


    [Tooltip("Set the density of Systems in the Universe")]
    [SerializeField]
    private DebugSystemDensity _systemDensity = DebugSystemDensity.Normal;

    [Tooltip("Set the advancement level that all players will start at")]
    [SerializeField]
    private DebugEmpireStartLevel _startLevel = DebugEmpireStartLevel.Normal;

    [Tooltip("Set the desirability of all player's home system")]
    [SerializeField]
    private DebugSystemDesirability _homeSystemDesirability = DebugSystemDesirability.Normal;

    [Tooltip("Set the degree of separation of all AIPlayer's from the User")]
    [SerializeField]
    private DebugPlayerSeparation _aiPlayersSeparationFromUser = DebugPlayerSeparation.Normal;

    [Tooltip("Check to deploy additional User creators on random dates. Not valid when _useDebugCreatorsOnly is checked")]
    [SerializeField]
    private bool _deployAdditionalUserCreators = false;

    [Tooltip("Check to deploy additional AI creators on random dates. Not valid when _useDebugCreatorsOnly is checked")]
    [SerializeField]
    private bool _deployAdditionalAiCreators = false;

    [Tooltip("The number of additional fleet creators per player")]
    [Range(0, 9)]
    [SerializeField]
    private int _additionalFleetCreatorQty = 3;

    [Tooltip("The number of additional starbase creators per player")]
    [Range(0, 9)]
    [SerializeField]
    private int _additionalStarbaseCreatorQty = 3;

    [Tooltip("The number of additional settlement creators per player")]
    [Range(0, 9)]
    [SerializeField]
    private int _additionalSettlementCreatorQty = 3;

    [Tooltip("Check to zoom on a user unit when starting a new game. Not valid when _useDebugCreatorsOnly is checked")]
    [SerializeField]
    private bool _zoomOnUser = false;

    #endregion

    public override bool IsPersistentAcrossScenes { get { return true; } }  // GameScene -> GameScene retains values

    public string DebugName { get { return GetType().Name; } }

    private IGameManager _gameMgr;
    private PlayerPrefsManager _playerPrefsMgr;

    #region Initialization

    /// <summary>
    /// Called on the first Instance call or from Awake, whichever comes first, this method is limited to initializing 
    /// local references and values that don't rely on ANY other MonoBehaviour Awake methods having already run.
    /// </summary>
    protected override void InitializeOnInstance() {
        base.InitializeOnInstance();
        GameReferences.GameSettingsDebugControl = Instance;
    }

    /// <summary>
    /// Called from Awake after InitializeOnInstance, this method should be limited to initializing local references and values. 
    /// Note: Other MonoBehaviour Awake methods may or may not have yet been called depending on ScriptExecutionOrder.
    /// </summary>
    protected override void InitializeOnAwake() {
        base.InitializeOnAwake();
        InitializeValuesAndReferences();
    }

    private void InitializeValuesAndReferences() {
        _gameMgr = GameReferences.GameManager;
        _playerPrefsMgr = PlayerPrefsManager.Instance;
    }

    #endregion

    #region Value Change Checking

    void OnValidate() {
        CheckValuesForChange();
    }

    private void CheckValuesForChange() {
        CheckPlayerCount();
    }

    private void CheckPlayerCount() {
        _maxPlayerCount = _universeSize.Convert().MaxPlayerCount();
        if (_playerCount > _maxPlayerCount) {
            _playerCount = _maxPlayerCount;
        }
        else if (_playerCount < 2) {
            _playerCount = 2;
        }
    }

    #endregion

    /// <summary>
    /// Called by the button on this control to launch a new game 
    /// using the values specified in the editor fields and preferences.
    /// </summary>
    public void LaunchNewGame() {
        var gameSettings = CreateNewGameSettings();
        _gameMgr.InitiateNewGame(gameSettings);
    }

    /// <summary>
    /// Creates new game settings using the values specified in the editor fields and preferences.
    /// </summary>
    /// <param name="isStartup">if set to <c>true</c> [is startup].</param>
    /// <returns></returns>
    /// <exception cref="System.NotImplementedException"></exception>
    public GameSettings CreateNewGameSettings(bool isStartup = false) {
        int aiPlayerCount = _playerCount - 1;
        Player[] aiPlayers = new Player[aiPlayerCount];
        EmpireStartLevel[] aiPlayerStartLevels = new EmpireStartLevel[aiPlayerCount];
        SystemDesirability[] aiPlayerHomeSystemDesirabilties = new SystemDesirability[aiPlayerCount];
        PlayerSeparation[] aiPlayerSeparationFromUser = new PlayerSeparation[aiPlayerCount];

        Species aiSpecies;
        GameColor aiColor;
        IQ aiIQ;
        TeamID aiTeam;

        for (int i = 0; i < aiPlayerCount; i++) {
            int aiPlayerNumber = i + 1;
            switch (aiPlayerNumber) {
                case 1:
                    aiSpecies = _playerPrefsMgr.AIPlayer1SpeciesSelection.Convert();
                    aiColor = _playerPrefsMgr.AIPlayer1Color;
                    aiIQ = _playerPrefsMgr.AIPlayer1IQ;
                    aiTeam = _playerPrefsMgr.AIPlayer1Team;
                    break;
                case 2:
                    aiSpecies = _playerPrefsMgr.AIPlayer2SpeciesSelection.Convert();
                    aiColor = _playerPrefsMgr.AIPlayer2Color;
                    aiIQ = _playerPrefsMgr.AIPlayer2IQ;
                    aiTeam = _playerPrefsMgr.AIPlayer2Team;
                    break;
                case 3:
                    aiSpecies = _playerPrefsMgr.AIPlayer3SpeciesSelection.Convert();
                    aiColor = _playerPrefsMgr.AIPlayer3Color;
                    aiIQ = _playerPrefsMgr.AIPlayer3IQ;
                    aiTeam = _playerPrefsMgr.AIPlayer3Team;
                    break;
                case 4:
                    aiSpecies = _playerPrefsMgr.AIPlayer4SpeciesSelection.Convert();
                    aiColor = _playerPrefsMgr.AIPlayer4Color;
                    aiIQ = _playerPrefsMgr.AIPlayer4IQ;
                    aiTeam = _playerPrefsMgr.AIPlayer4Team;
                    break;
                case 5:
                    aiSpecies = _playerPrefsMgr.AIPlayer5SpeciesSelection.Convert();
                    aiColor = _playerPrefsMgr.AIPlayer5Color;
                    aiIQ = _playerPrefsMgr.AIPlayer5IQ;
                    aiTeam = _playerPrefsMgr.AIPlayer5Team;
                    break;
                case 6:
                    aiSpecies = _playerPrefsMgr.AIPlayer6SpeciesSelection.Convert();
                    aiColor = _playerPrefsMgr.AIPlayer6Color;
                    aiIQ = _playerPrefsMgr.AIPlayer6IQ;
                    aiTeam = _playerPrefsMgr.AIPlayer6Team;
                    break;
                case 7:
                    aiSpecies = _playerPrefsMgr.AIPlayer7SpeciesSelection.Convert();
                    aiColor = _playerPrefsMgr.AIPlayer7Color;
                    aiIQ = _playerPrefsMgr.AIPlayer7IQ;
                    aiTeam = _playerPrefsMgr.AIPlayer7Team;
                    break;
                default:
                    throw new NotImplementedException(ErrorMessages.UnanticipatedSwitchValue.Inject(aiPlayerNumber));
            }
            SpeciesStat aiSpeciesStat = SpeciesFactory.Instance.MakeInstance(aiSpecies);
            LeaderStat aiLeaderStat = LeaderFactory.Instance.MakeInstance(aiSpecies);
            aiPlayers[i] = new Player(aiSpeciesStat, aiLeaderStat, aiIQ, aiTeam, aiColor);
            aiPlayerStartLevels[i] = _startLevel.Convert();
            aiPlayerHomeSystemDesirabilties[i] = _homeSystemDesirability.Convert();
            aiPlayerSeparationFromUser[i] = _aiPlayersSeparationFromUser.Convert();
        }

        var userPlayerSpecies = _playerPrefsMgr.UserPlayerSpeciesSelection.Convert();
        var userPlayerSpeciesStat = SpeciesFactory.Instance.MakeInstance(userPlayerSpecies);
        var userPlayerLeaderStat = LeaderFactory.Instance.MakeInstance(userPlayerSpecies);
        var userPlayerColor = _playerPrefsMgr.UserPlayerColor;
        var userPlayerTeamID = _playerPrefsMgr.UserPlayerTeam;
        bool toDeployAdditionalUserCreators = !_useDebugCreatorsOnly && _deployAdditionalUserCreators;
        bool toDeployAdditionalAiCreators = !_useDebugCreatorsOnly && _deployAdditionalAiCreators;
        Player userPlayer = new Player(userPlayerSpeciesStat, userPlayerLeaderStat, IQ.None, userPlayerTeamID, userPlayerColor, isUser: true);
        var gameSettings = new GameSettings {
            __IsStartup = isStartup,
            __UseDebugCreatorsOnly = _useDebugCreatorsOnly,
            __DeployAdditionalUserCreators = toDeployAdditionalUserCreators,
            __DeployAdditionalAICreators = toDeployAdditionalAiCreators,
            __AdditionalFleetCreatorQty = toDeployAdditionalAiCreators || toDeployAdditionalUserCreators ? _additionalFleetCreatorQty : Constants.Zero,
            __AdditionalStarbaseCreatorQty = toDeployAdditionalAiCreators || toDeployAdditionalUserCreators ? _additionalStarbaseCreatorQty : Constants.Zero,
            __AdditionalSettlementCreatorQty = toDeployAdditionalAiCreators || toDeployAdditionalUserCreators ? _additionalSettlementCreatorQty : Constants.Zero,
            __ZoomOnUser = !_useDebugCreatorsOnly && _zoomOnUser,
            UniverseSize = _universeSize.Convert(),
            SystemDensity = _systemDensity.Convert(),
            PlayerCount = _playerCount,
            UserPlayer = userPlayer,
            AIPlayers = aiPlayers,
            UserStartLevel = _startLevel.Convert(),
            AIPlayersStartLevels = aiPlayerStartLevels,
            UserHomeSystemDesirability = _homeSystemDesirability.Convert(),
            AIPlayersHomeSystemDesirability = aiPlayerHomeSystemDesirabilties,
            AIPlayersUserSeparations = aiPlayerSeparationFromUser
        };
        return gameSettings;
    }

    #region Event and Prop Change Handlers

    #endregion

    #region Cleanup

    protected override void Cleanup() {
        GameReferences.GameSettingsDebugControl = null;
    }

    #endregion

    public override string ToString() {
        return DebugName;
    }


}

