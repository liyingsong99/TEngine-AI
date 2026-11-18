namespace GameLogic
{
    public enum TankBattleState { Preparing, Playing, Victory, Defeat }
    public enum TankBattleFailure { None, PlayerDestroyed, BaseDestroyed }

    /// <summary>HUD 只读快照；剩余数量包含尚未出场的敌人。</summary>
    public readonly struct TankBattleSnapshot
    {
        public readonly int PlayerHp;
        public readonly int PlayerMaxHp;
        public readonly int WaveIndex;
        public readonly int WaveTotal;
        public readonly int RemainingEnemies;
        public readonly int KillCount;
        public readonly int TotalEnemies;
        public readonly int BaseHp;
        public readonly TankBattleState BattleState;
        public readonly TankBattleFailure FailureReason;

        public TankBattleSnapshot(int playerHp, int killCount, int baseHp,
            TankBattleState state, TankBattleFailure failure)
        {
            PlayerHp = playerHp;
            PlayerMaxHp = TankBattleModule.PlayerMaxHp;
            WaveIndex = 1;
            WaveTotal = 1;
            TotalEnemies = TankBattleModule.EnemyTotal;
            RemainingEnemies = TotalEnemies - killCount;
            KillCount = killCount;
            BaseHp = baseHp;
            BattleState = state;
            FailureReason = failure;
        }
    }
}
