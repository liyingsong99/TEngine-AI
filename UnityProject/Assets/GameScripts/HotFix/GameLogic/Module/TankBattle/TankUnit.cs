using UnityEngine;

namespace GameLogic
{
    internal sealed class TankUnit
    {
        private const float HalfSize = 0.48f;
        private const float PlayerSpeed = 4.5f;
        private const float EnemySpeed = 1.5f;
        private const float PlayerFireInterval = 0.3f;
        private const float EnemyFireInterval = 1.8f;
        private float _fireCooldown;
        private float _turnTimer;
        private Vector3 _detour;
        private Collider _obstacle;

        public GameObject View { get; }
        public BoxCollider Collider { get; }
        public bool IsPlayer { get; }
        public int Hp { get; private set; }
        public bool Alive => Hp > 0;
        public Vector3 Position => View.transform.position;
        public Vector3 Direction { get; private set; }

        public TankUnit(TankBattleWorld world, bool isPlayer, Vector3 position, int ordinal)
        {
            IsPlayer = isPlayer;
            Hp = isPlayer ? TankBattleModule.PlayerMaxHp : 1;
            Direction = isPlayer ? Vector3.forward : Vector3.back;
            View = world.CreateTank(isPlayer, position, ordinal);
            Collider = View.AddComponent<BoxCollider>();
            Collider.center = new Vector3(0, 0.5f, 0);
            Collider.size = new Vector3(HalfSize * 2, 1, HalfSize * 2);
            _fireCooldown = isPlayer ? 0 : EnemyFireInterval;
            Face(Direction);
        }

        public void TickCooldown(float deltaTime)
        {
            _fireCooldown = Mathf.Max(0, _fireCooldown - deltaTime);
        }

        public bool TryFire()
        {
            if (!Alive || _fireCooldown > 0) return false;
            _fireCooldown = IsPlayer ? PlayerFireInterval : EnemyFireInterval;
            return true;
        }

        public bool Move(Vector3 direction, float deltaTime)
        {
            if (!Alive || direction == Vector3.zero) return false;
            Face(direction);
            float distance = (IsPlayer ? PlayerSpeed : EnemySpeed) * deltaTime;
            Vector3 origin = Position + Vector3.up * 0.5f;
            var hits = Physics.BoxCastAll(origin, new Vector3(HalfSize, 0.35f, HalfSize),
                Direction, Quaternion.identity, distance + 0.04f, ~0, QueryTriggerInteraction.Ignore);
            _obstacle = null;
            foreach (var hit in hits)
            {
                if (hit.collider == Collider) continue;
                _obstacle = hit.collider;
                return false;
            }
            View.transform.position += Direction * distance;
            Physics.SyncTransforms();
            return true;
        }

        public void TickEnemy(float deltaTime, Vector3 basePosition, TankBattleWorld world)
        {
            _turnTimer -= deltaTime;
            Vector3 direction;
            if (_turnTimer > 0) direction = _detour;
            else if (Position.z > -5.5f) direction = Vector3.back;
            else if (Mathf.Abs(Position.x - basePosition.x) > 0.12f)
                direction = Position.x > basePosition.x ? Vector3.left : Vector3.right;
            else direction = Vector3.back;
            if (Move(direction, deltaTime)) return;
            // 护墙前保持炮口朝向，等待开火，不反复横移。
            if (world.IsAttackable(_obstacle)) return;
            _detour = Direction.z != 0
                ? (Position.x >= 0 ? Vector3.left : Vector3.right)
                : Vector3.back;
            _turnTimer = 0.8f;
        }

        public void Hit()
        {
            if (!Alive) return;
            Hp--;
            if (Alive) return;
            Collider.enabled = false;
            View.SetActive(false);
            Object.Destroy(View);
        }

        private void Face(Vector3 direction)
        {
            Direction = direction.normalized;
            View.transform.rotation = Quaternion.LookRotation(Direction);
        }
    }
}
