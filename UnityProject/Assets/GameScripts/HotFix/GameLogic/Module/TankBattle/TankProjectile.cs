using UnityEngine;

namespace GameLogic
{
    internal sealed class TankProjectile
    {
        private const float Speed = 14f;
        private const float Radius = 0.11f;
        private const float Lifetime = 3f;
        private readonly TankUnit _owner;
        private readonly GameObject _view;
        private readonly Vector3 _direction;
        private float _age;

        public TankProjectile(TankBattleWorld world, TankUnit owner)
        {
            _owner = owner;
            _direction = owner.Direction;
            // 从车体内扫出，过滤自身；避免炮口越过紧邻砖墙后才开始检测。
            _view = world.CreateProjectile(owner.Position + Vector3.up * 0.6f, owner.IsPlayer);
            world.Flash(owner.Position + _direction * 0.85f + Vector3.up * 0.6f, 0.22f);
        }

        public bool Tick(float deltaTime, TankBattleModule battle)
        {
            _age += deltaTime;
            if (_age >= Lifetime) return false;
            float distance = Speed * deltaTime;
            Vector3 position = _view.transform.position;
            var hits = Physics.SphereCastAll(position, Radius, _direction, distance,
                ~0, QueryTriggerInteraction.Ignore);
            RaycastHit nearest = default;
            float nearestDistance = float.MaxValue;
            foreach (var hit in hits)
            {
                if (hit.collider == _owner.Collider || battle.IsFriendly(hit.collider, _owner.IsPlayer)) continue;
                if (hit.distance >= nearestDistance) continue;
                nearest = hit;
                nearestDistance = hit.distance;
            }
            if (nearestDistance < float.MaxValue)
            {
                battle.Hit(nearest.collider, _owner.IsPlayer, nearest.point);
                return false;
            }
            _view.transform.position = position + _direction * distance;
            return Mathf.Abs(_view.transform.position.x) < 11 && Mathf.Abs(_view.transform.position.z) < 11;
        }

        public void Dispose()
        {
            if (_view != null) Object.Destroy(_view);
        }
    }
}
