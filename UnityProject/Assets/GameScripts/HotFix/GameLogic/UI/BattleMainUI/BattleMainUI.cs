using System;
using UnityEngine;
using UnityEngine.UI;
using TEngine;

namespace GameLogic
{
    [Window(UILayer.UI, location: "BattleMainUI", hideTimeToClose: 0)]
    class BattleMainUI : UIWindow
    {
        private Text _playerHp;
        private Text _wave;
        private Text _remaining;
        private Text _kills;
        private Text _baseHp;
        private GameObject _result;
        private Text _resultTitle;
        private Text _resultReason;
        private Text _resultStats;
        private Button _restart;
        private CanvasScaler _scaler;
        private Vector2 _previousResolution;
        private float _previousMatch;
        private bool _layoutChanged;
        private bool _initialized;
        private TankBattleSnapshot _displayed;
        private bool _hasDisplayed;

        protected override void ScriptGenerator()
        {
            _playerHp = Require<Text>("m_goHud/m_textPlayerHp");
            _wave = Require<Text>("m_goHud/m_textWave");
            _remaining = Require<Text>("m_goHud/m_textRemainingEnemies");
            _kills = Require<Text>("m_goHud/m_textKillCount");
            _baseHp = Require<Text>("m_goHud/m_goBasePanel/m_textBaseHp");
            _result = Require<RectTransform>("m_goResult").gameObject;
            const string panel = "m_goResult/m_goResultPanel/";
            _resultTitle = Require<Text>(panel + "m_textResultTitle");
            _resultReason = Require<Text>(panel + "m_textResultReason");
            _resultStats = Require<Text>(panel + "m_textResultStats");
            _restart = Require<Button>(panel + "m_btnRestart");
        }

        protected override void RegisterEvent()
        {
            // AddUIEvent 管理 GameEvent；Button 使用 UnityEvent，并在窗口销毁时配对移除。
            _restart.onClick.AddListener(Restart);
        }

        protected override void OnCreate()
        {
            _scaler = UIModule.UIRoot.GetComponent<CanvasScaler>();
            if (_scaler == null) throw new InvalidOperationException("BattleMainUI requires the host CanvasScaler.");
            _previousResolution = _scaler.referenceResolution;
            _previousMatch = _scaler.matchWidthOrHeight;
            _layoutChanged = true;
            _scaler.referenceResolution = new Vector2(1920, 1080);
            _scaler.matchWidthOrHeight = 0.5f;
            _result.SetActive(false);
            try
            {
                // 绑定和布局成功后才开战，避免 UI 异步加载期间敌人先行动。
                GameModule.TankBattle.StartBattle();
                _initialized = true;
                RefreshData(true);
            }
            catch
            {
                RestoreLayout();
                throw;
            }
        }

        protected override void OnRefresh()
        {
            if (_initialized) RefreshData(true);
        }

        protected override void OnUpdate()
        {
            if (_initialized) RefreshData(false);
        }

        private void Restart()
        {
            if (!_initialized) return;
            GameModule.TankBattle.RestartBattle();
            RefreshData(true);
        }

        private void RefreshData(bool force)
        {
            if (!TankBattleModule.IsValid || !GameModule.TankBattle.IsRunning) return;
            var data = GameModule.TankBattle.Snapshot;
            if (!force && _hasDisplayed && data.PlayerHp == _displayed.PlayerHp &&
                data.KillCount == _displayed.KillCount && data.BaseHp == _displayed.BaseHp &&
                data.BattleState == _displayed.BattleState && data.FailureReason == _displayed.FailureReason) return;
            _displayed = data;
            _hasDisplayed = true;
            _playerHp.text = data.PlayerHp + " / " + data.PlayerMaxHp;
            _wave.text = data.WaveIndex.ToString("00") + " / " + data.WaveTotal.ToString("00");
            _remaining.text = data.RemainingEnemies.ToString("00");
            _kills.text = data.KillCount.ToString("00") + " / " + data.TotalEnemies.ToString("00");
            _baseHp.text = data.BaseHp > 0 ? "安全" : "已摧毁";
            bool finished = data.BattleState == TankBattleState.Victory || data.BattleState == TankBattleState.Defeat;
            _result.SetActive(finished);
            if (!finished) return;
            bool victory = data.BattleState == TankBattleState.Victory;
            _resultTitle.text = victory ? "守卫成功" : "任务失败";
            if (victory) _resultReason.text = "敌军已全部击退，基地安全。";
            else if (data.FailureReason == TankBattleFailure.BaseDestroyed) _resultReason.text = "基地已被摧毁。";
            else _resultReason.text = "你的坦克已被击毁。";
            _resultStats.text = "击毁 " + data.KillCount + " / " + data.TotalEnemies + "    波次 " + data.WaveIndex + " / " + data.WaveTotal;
        }

        protected override void OnDestroy()
        {
            _initialized = false;
            if (_restart != null) _restart.onClick.RemoveListener(Restart);
            if (TankBattleModule.IsValid) GameModule.TankBattle.StopBattle();
            RestoreLayout();
        }

        private void RestoreLayout()
        {
            if (!_layoutChanged || _scaler == null) return;
            _scaler.referenceResolution = _previousResolution;
            _scaler.matchWidthOrHeight = _previousMatch;
            _layoutChanged = false;
        }

        private T Require<T>(string path) where T : Component
        {
            var component = FindChildComponent<T>(path);
            if (component == null) throw new InvalidOperationException("BattleMainUI missing " + typeof(T).Name + ": " + path);
            return component;
        }
    }
}
