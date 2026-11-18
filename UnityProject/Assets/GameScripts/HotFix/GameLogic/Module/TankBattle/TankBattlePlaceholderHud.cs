using UnityEngine;

namespace GameLogic
{
    /// <summary>管线 HUD 接入前的临时数据读数，不依赖 Prefab 或节点。</summary>
    public sealed class TankBattlePlaceholderHud : MonoBehaviour
    {
        private GUIStyle _label;

        private void OnGUI()
        {
            if (!TankBattleModule.IsValid || !GameModule.TankBattle.IsRunning) return;
            if (_label == null)
            {
                _label = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 22,
                    fontStyle = FontStyle.Bold
                };
                _label.normal.textColor = Color.white;
            }
            var data = GameModule.TankBattle.Snapshot;
            var previous = GUI.matrix;
            float scale = Mathf.Max(0.35f, Screen.width / 1920f);
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1));
            GUI.Label(new Rect(220, 22, 1600, 42),
                "TANK BATTLE   HP " + data.PlayerHp + "/" + data.PlayerMaxHp +
                "   WAVE " + data.WaveIndex + "/" + data.WaveTotal +
                "   DESTROYED " + data.KillCount + "/" + data.TotalEnemies +
                "   REMAINING " + data.RemainingEnemies + "   BASE " + data.BaseHp, _label);
            GUI.Label(new Rect(220, 64, 1400, 40), "WASD MOVE   |   SPACE FIRE   |   R RESTART AFTER RESULT", _label);
            if (data.BattleState != TankBattleState.Playing)
            {
                GUI.Box(new Rect(610, 180, 700, 110), GUIContent.none);
                GUI.Label(new Rect(640, 192, 640, 42), data.BattleState + "  " + data.FailureReason, _label);
                GUI.Label(new Rect(640, 235, 640, 42), "PRESS R TO RESTART", _label);
            }
            GUI.matrix = previous;
        }
    }
}
