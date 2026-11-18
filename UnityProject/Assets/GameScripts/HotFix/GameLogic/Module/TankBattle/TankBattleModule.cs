using System;
using System.Collections.Generic;
using TEngine;
using UnityEngine;

namespace GameLogic
{
    /// <summary>单局坦克战斗。更新和退出释放复用热更层单例系统。</summary>
    public sealed class TankBattleModule : Singleton<TankBattleModule>, IUpdate
    {
        public const int PlayerMaxHp = 3;
        public const int EnemyTotal = 3;
        private const int MaxActiveEnemies = 2;
        private const float SpawnInterval = 2f;
        private readonly List<TankUnit> _enemies = new List<TankUnit>();
        private readonly List<TankProjectile> _projectiles = new List<TankProjectile>();
        private TankBattleWorld _world;
        private TankUnit _player;
        private int _spawned;
        private int _kills;
        private int _baseHp;
        private float _spawnTimer;
        private bool _running;
        private TankBattleState _state;
        private TankBattleFailure _failure;

        public bool IsRunning => _running;
        public TankBattleSnapshot Snapshot => new TankBattleSnapshot(
            _player != null ? _player.Hp : 0, _kills, _baseHp, _state, _failure);

        public void StartBattle()
        {
            StopBattle();
            try
            {
                _world = new TankBattleWorld();
                _world.Build();
                _player = new TankUnit(_world, true, new Vector3(-4, 0, -7), 0);
                _spawned = 0;
                _kills = 0;
                _baseHp = 1;
                _spawnTimer = 0;
                _state = TankBattleState.Playing;
                _failure = TankBattleFailure.None;
                _running = true;
                SpawnEnemy();
                Physics.SyncTransforms();
                Log.Info("TankBattle started: wave 1/1, enemies 3, player HP 3.");
            }
            catch
            {
                StopBattle();
                throw;
            }
        }

        /// <summary>供结算按钮和键盘共用；战斗进行中不重复开局。</summary>
        public void RestartBattle()
        {
            if (!_running || _state != TankBattleState.Playing) StartBattle();
        }

        public void StopBattle()
        {
            _running = false;
            foreach (var projectile in _projectiles) projectile.Dispose();
            _projectiles.Clear();
            _enemies.Clear();
            _player = null;
            if (_world == null) return;
            _world.Dispose();
            _world = null;
        }

        protected override void OnRelease()
        {
            StopBattle();
        }

        public void OnUpdate()
        {
            if (!_running) return;
            _world.UpdateCamera();
            if (_state != TankBattleState.Playing)
            {
                if (Input.GetKeyDown(KeyCode.R)) RestartBattle();
                return;
            }
            float deltaTime = Mathf.Min(Time.deltaTime, 0.05f);
            Vector3 direction = ReadMovement();
            Tick(deltaTime, direction, Input.GetKey(KeyCode.Space));
        }

        private static Vector3 ReadMovement()
        {
            if (Input.GetKey(KeyCode.W)) return Vector3.forward;
            if (Input.GetKey(KeyCode.S)) return Vector3.back;
            if (Input.GetKey(KeyCode.A)) return Vector3.left;
            if (Input.GetKey(KeyCode.D)) return Vector3.right;
            return Vector3.zero;
        }

        private void Tick(float deltaTime, Vector3 movement, bool fire)
        {
            _player.TickCooldown(deltaTime);
            _player.Move(movement, deltaTime);
            if (fire) Fire(_player);
            foreach (var enemy in _enemies)
            {
                enemy.TickCooldown(deltaTime);
                enemy.TickEnemy(deltaTime, TankBattleWorld.BasePosition, _world);
                Fire(enemy);
            }
            Physics.SyncTransforms();
            for (int i = _projectiles.Count - 1; i >= 0; i--)
            {
                if (_projectiles[i].Tick(deltaTime, this)) continue;
                _projectiles[i].Dispose();
                _projectiles.RemoveAt(i);
            }
            _enemies.RemoveAll(enemy => !enemy.Alive);
            EvaluateResult();
            if (_state != TankBattleState.Playing) return;
            _spawnTimer -= deltaTime;
            if (_spawnTimer <= 0) SpawnEnemy();
        }

        private void Fire(TankUnit unit)
        {
            if (unit.TryFire()) _projectiles.Add(new TankProjectile(_world, unit));
        }

        private void SpawnEnemy()
        {
            if (_spawned >= EnemyTotal || _enemies.Count >= MaxActiveEnemies) return;
            float x = _spawned % 2 == 0 ? -4 : 4;
            Vector3 position = new Vector3(x, 0, 7);
            if (Physics.CheckBox(position + Vector3.up * 0.5f, new Vector3(0.65f, 0.4f, 0.65f))) return;
            _spawned++;
            _enemies.Add(new TankUnit(_world, false, position, _spawned));
            Physics.SyncTransforms();
            _spawnTimer = SpawnInterval;
        }

        internal bool IsFriendly(Collider collider, bool playerShell)
        {
            if (!playerShell)
            {
                foreach (var enemy in _enemies)
                    if (enemy.Collider == collider) return true;
            }
            return playerShell && (_player.Collider == collider || _world.BaseCollider == collider);
        }

        internal void Hit(Collider collider, bool playerShell, Vector3 position)
        {
            _world.Flash(position, 0.5f);
            if (_world.BreakBrick(collider)) return;
            if (!playerShell && collider == _world.BaseCollider)
            {
                _baseHp = 0;
                _world.DestroyBase();
                return;
            }
            if (!playerShell && collider == _player.Collider)
            {
                _player.Hit();
                return;
            }
            if (!playerShell) return;
            foreach (var enemy in _enemies)
            {
                if (enemy.Collider != collider || !enemy.Alive) continue;
                enemy.Hit();
                _kills++;
                Log.Info("TankBattle enemy destroyed: " + _kills + "/" + EnemyTotal);
                return;
            }
        }

        private void EvaluateResult()
        {
            // 同一帧先完成全部命中，再优先判失败，避免最后一击覆盖基地被毁。
            if (!_player.Alive)
            {
                _state = TankBattleState.Defeat;
                _failure = TankBattleFailure.PlayerDestroyed;
            }
            else if (_baseHp <= 0)
            {
                _state = TankBattleState.Defeat;
                _failure = TankBattleFailure.BaseDestroyed;
            }
            else if (_kills >= EnemyTotal) _state = TankBattleState.Victory;
            else return;
            foreach (var projectile in _projectiles) projectile.Dispose();
            _projectiles.Clear();
            Log.Info("TankBattle result: " + _state + ", reason: " + _failure);
        }
    }
}
