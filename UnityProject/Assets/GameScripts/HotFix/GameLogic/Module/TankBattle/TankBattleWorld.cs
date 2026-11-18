using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GameLogic
{
    internal sealed class TankBattleWorld : IDisposable
    {
        public static readonly Vector3 BasePosition = new Vector3(0, 0, -7);
        private readonly List<Material> _materials = new List<Material>();
        private readonly HashSet<Collider> _bricks = new HashSet<Collider>();
        private readonly Camera _camera;
        private readonly Vector3 _cameraPosition;
        private readonly Quaternion _cameraRotation;
        private readonly bool _orthographic;
        private readonly float _orthographicSize;
        private readonly Color _background;
        private readonly CameraClearFlags _clearFlags;
        private readonly Material _ground;
        private readonly Material _track;
        private readonly Material _player;
        private readonly Material _enemy;
        private readonly Material _brick;
        private readonly Material _steel;
        private readonly Material _gold;
        private readonly Material _flash;
        private float _aspect;

        public GameObject Root { get; }
        public BoxCollider BaseCollider { get; private set; }
        public int BrickCount => _bricks.Count;

        public TankBattleWorld()
        {
            _camera = Camera.main;
            if (_camera == null) throw new InvalidOperationException("Tank demo requires Main Camera.");
            var shader = Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("Tank demo requires the built-in Standard shader.");
            _cameraPosition = _camera.transform.position;
            _cameraRotation = _camera.transform.rotation;
            _orthographic = _camera.orthographic;
            _orthographicSize = _camera.orthographicSize;
            _background = _camera.backgroundColor;
            _clearFlags = _camera.clearFlags;
            Root = new GameObject("TankBattleRoot");
            _ground = Material(shader, new Color(0.17f, 0.22f, 0.23f));
            _track = Material(shader, new Color(0.065f, 0.085f, 0.1f));
            _player = Material(shader, new Color(0.3f, 0.72f, 0.42f));
            _enemy = Material(shader, new Color(0.8f, 0.24f, 0.17f));
            _brick = Material(shader, new Color(0.65f, 0.37f, 0.21f));
            _steel = Material(shader, new Color(0.31f, 0.4f, 0.46f));
            _gold = Material(shader, new Color(0.95f, 0.7f, 0.2f));
            _flash = Material(shader, new Color(1f, 0.83f, 0.35f));
            _flash.EnableKeyword("_EMISSION");
            _flash.SetColor("_EmissionColor", new Color(1.5f, 0.7f, 0.1f));
        }

        public void Build()
        {
            Box("Ground", new Vector3(0, -0.3f, 0), new Vector3(19, 0.5f, 19), _ground);
            Wall("WestBoundary", new Vector3(-9, 0.5f, 0), new Vector3(0.5f, 1, 18.5f));
            Wall("EastBoundary", new Vector3(9, 0.5f, 0), new Vector3(0.5f, 1, 18.5f));
            Wall("NorthBoundary", new Vector3(0, 0.5f, 9), new Vector3(18.5f, 1, 0.5f));
            Wall("SouthBoundary", new Vector3(0, 0.5f, -9), new Vector3(18.5f, 1, 0.5f));
            foreach (float x in new[] { -6f, -2f, 2f, 6f })
                foreach (float z in new[] { -3f, 1f, 4f })
                    AddBrick(new Vector3(x, 0.45f, z));
            foreach (float x in new[] { -1f, 0f, 1f }) AddBrick(new Vector3(x, 0.45f, -5.5f));
            var baseView = Box("Base", BasePosition + Vector3.up * 0.45f, new Vector3(1.3f, 0.9f, 1.3f), _gold);
            BaseCollider = baseView.AddComponent<BoxCollider>();
            Box("BaseBeacon", BasePosition + Vector3.up * 1.1f, new Vector3(0.25f, 0.6f, 0.25f), _gold);
            for (int i = -8; i <= 8; i += 2)
            {
                Box("Grid", new Vector3(i, -0.035f, 0), new Vector3(0.025f, 0.015f, 18), _steel);
                Box("Grid", new Vector3(0, -0.035f, i), new Vector3(18, 0.015f, 0.025f), _steel);
            }
            var lightObject = new GameObject("BattleLight");
            lightObject.transform.SetParent(Root.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(50, -35, 0);
            var light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows = LightShadows.Soft;
            _camera.transform.SetPositionAndRotation(new Vector3(0, 20, -13), Quaternion.Euler(57, 0, 0));
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0.04f, 0.06f, 0.085f);
            UpdateCamera();
            Physics.SyncTransforms();
        }

        public GameObject CreateTank(bool player, Vector3 position, int ordinal)
        {
            var root = new GameObject(player ? "PlayerTank" : "EnemyTank_" + ordinal);
            root.transform.SetParent(Root.transform, false);
            root.transform.position = position;
            var paint = player ? _player : _enemy;
            Part(root, "Hull", new Vector3(0, 0.38f, 0), new Vector3(0.72f, 0.38f, 1.03f), paint);
            Part(root, "LeftTrack", new Vector3(-0.4f, 0.23f, 0), new Vector3(0.23f, 0.35f, 1.16f), _track);
            Part(root, "RightTrack", new Vector3(0.4f, 0.23f, 0), new Vector3(0.23f, 0.35f, 1.16f), _track);
            Part(root, "Turret", new Vector3(0, 0.68f, -0.08f), new Vector3(0.52f, 0.28f, 0.55f), paint);
            Part(root, "Barrel", new Vector3(0, 0.68f, 0.48f), new Vector3(0.14f, 0.14f, 0.8f), _steel);
            Part(root, "Hatch", new Vector3(0, 0.85f, -0.08f), new Vector3(0.24f, 0.06f, 0.26f), _gold);
            return root;
        }

        public GameObject CreateProjectile(Vector3 position, bool player)
        {
            return Box(player ? "PlayerShell" : "EnemyShell", position, new Vector3(0.16f, 0.16f, 0.3f), _flash);
        }

        public void Flash(Vector3 position, float size)
        {
            var view = Box("Impact", position, Vector3.one * size, _flash);
            Object.Destroy(view, 0.15f);
        }

        public bool IsAttackable(Collider collider)
        {
            return collider != null && (collider == BaseCollider || _bricks.Contains(collider));
        }

        public bool BreakBrick(Collider collider)
        {
            if (!_bricks.Remove(collider)) return false;
            collider.enabled = false;
            collider.gameObject.SetActive(false);
            Object.Destroy(collider.gameObject);
            return true;
        }

        public void DestroyBase()
        {
            BaseCollider.enabled = false;
            BaseCollider.gameObject.SetActive(false);
        }

        public void UpdateCamera()
        {
            if (_camera == null) return;
            float aspect = _camera.aspect;
            if (Mathf.Approximately(_aspect, aspect)) return;
            _aspect = aspect;
            _camera.orthographicSize = Mathf.Max(10.8f, 10.4f / Mathf.Max(0.1f, aspect));
        }

        public void Dispose()
        {
            if (Root != null)
            {
                Root.SetActive(false);
                Object.Destroy(Root);
            }
            foreach (var material in _materials) if (material != null) Object.Destroy(material);
            _materials.Clear();
            if (_camera == null) return;
            _camera.transform.SetPositionAndRotation(_cameraPosition, _cameraRotation);
            _camera.orthographic = _orthographic;
            _camera.orthographicSize = _orthographicSize;
            _camera.backgroundColor = _background;
            _camera.clearFlags = _clearFlags;
        }

        private Material Material(Shader shader, Color color)
        {
            var material = new Material(shader) { color = color, name = "TankDemoMaterial" };
            material.SetFloat("_Glossiness", 0.15f);
            _materials.Add(material);
            return material;
        }

        private GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
        {
            var view = GameObject.CreatePrimitive(PrimitiveType.Cube);
            view.name = name;
            view.transform.SetParent(Root.transform, false);
            view.transform.localPosition = position;
            view.transform.localScale = scale;
            var collider = view.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            view.GetComponent<Renderer>().sharedMaterial = material;
            return view;
        }

        private void Part(GameObject parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var part = Box(name, Vector3.zero, scale, material);
            part.transform.SetParent(parent.transform, false);
            part.transform.localPosition = position;
        }

        private void Wall(string name, Vector3 position, Vector3 scale)
        {
            Box(name, position, scale, _steel).AddComponent<BoxCollider>();
        }

        private void AddBrick(Vector3 position)
        {
            var brick = Box("Brick", position, new Vector3(0.94f, 0.9f, 0.94f), _brick);
            _bricks.Add(brick.AddComponent<BoxCollider>());
        }
    }
}
