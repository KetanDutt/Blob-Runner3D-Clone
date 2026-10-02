using System.Collections.Generic;
using BlobRunner.Core;
using BlobRunner.Effects;
using UnityEngine;

namespace BlobRunner.Levels
{
    /// <summary>
    /// Prepares the track for a level.
    ///
    ///  * Level 1 keeps the hand authored obstacles and loot of the scene.
    ///  * Later levels replace them with a generated layout (<see cref="LevelGenerator"/>); the authored objects are
    ///    used as templates, so the look and the collider setup stay exactly the same.
    ///  * Every level gets a colour theme, a patterned floor (a plain white floor gives no feeling of speed),
    ///    striped hazard bars and a visible finish gate (the finish trigger itself is invisible).
    /// </summary>
    public sealed class LevelBuilder
    {
        private const string ObstacleTag = "Obstacle";
        private const string FinishTag = "AFinish";
        private const float FloorOverlayLift = 0.004f;

        private readonly Player _player;
        private readonly List<GameObject> _obstacles = new List<GameObject>();
        private readonly List<LootContainer> _loot = new List<LootContainer>();
        private readonly List<Object> _owned = new List<Object>(); // textures / materials / meshes created here

        private GameObject _obstacleTemplate;
        private LootContainer _lootTemplate;
        private GameObject _finishArea;
        private GameObject _floor;
        private Transform _root;
        private Material _barMaterial;
        private List<GameObject> _newObstacles;

        public LevelPlan Plan { get; private set; }

        public LevelTheme Theme { get; private set; }

        /// <summary>World position of the finish line.</summary>
        public Vector3 FinishPosition { get; private set; }

        public LevelBuilder(Player player)
        {
            _player = player;
        }

        public LevelPlan Build(int level)
        {
            Discover();

            bool useAuthored = level <= 1 && _obstacles.Count > 0 && _finishArea != null;
            Plan = useAuthored ? CreateAuthoredPlan() : LevelGenerator.Generate(level);
            Theme = LevelThemes.For(Plan.ThemeIndex);

            _root = new GameObject("[Level]").transform;

            if (!useAuthored)
            {
                SpawnObstacles(Plan);
                SpawnLoot(Plan);
                RetireAuthoredContent();
                MoveFinishArea(Plan.FinishZ);
            }

            ApplyBackground();
            ApplyBarVisuals();
            BuildFloorOverlay();
            BuildFinishGate();

            ReportProblems();
            return Plan;
        }

        public void Dispose()
        {
            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i] != null)
                    Object.Destroy(_owned[i]);
            }

            _owned.Clear();
        }

        // ---- Discovery ----------------------------------------------------------------------------------

        private void Discover()
        {
            _finishArea = GameObject.FindGameObjectWithTag(FinishTag);
            _floor = GameObject.Find("Floor");

            _obstacles.Clear();
            _obstacles.AddRange(GameObject.FindGameObjectsWithTag(ObstacleTag));
            _obstacles.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
            _obstacleTemplate = _obstacles.Count > 0 ? _obstacles[0] : null;

            _loot.Clear();
            _loot.AddRange(Object.FindObjectsOfType<LootContainer>());
            _loot.Sort((a, b) => a.transform.position.z.CompareTo(b.transform.position.z));
            _lootTemplate = _loot.Count > 0 ? _loot[0] : null;
        }

        private LevelPlan CreateAuthoredPlan()
        {
            var plan = new LevelPlan
            {
                Level = 1,
                Seed = 0,
                ThemeIndex = 0,
                StartZ = _player != null ? _player.transform.position.z : 0f,
                FinishZ = _finishArea.transform.position.z,
                PlayerSpeed = LevelGenerator.PlayerSpeedFor(1)
            };

            foreach (var bar in _obstacles)
            {
                var p = bar.transform.position;
                plan.Obstacles.Add(new ObstacleSpec { X = p.x, Y = p.y, Z = p.z, Width = bar.transform.localScale.x, Kind = ObstacleKind.Static });
            }

            foreach (var loot in _loot)
            {
                var p = loot.transform.position;
                plan.Loot.Add(new LootSpec { X = p.x, Y = p.y, Z = p.z });
            }

            return plan;
        }

        // ---- Generated content --------------------------------------------------------------------------

        private void SpawnObstacles(LevelPlan plan)
        {
            Vector3 templateScale = _obstacleTemplate != null ? _obstacleTemplate.transform.localScale : new Vector3(1f, 0.1556f, 0.1326f);
            var spawned = new List<GameObject>(plan.Obstacles.Count);

            foreach (var spec in plan.Obstacles)
            {
                GameObject bar = _obstacleTemplate != null ? Object.Instantiate(_obstacleTemplate, _root) : CreateFallbackBar();
                bar.name = "Bar z" + Mathf.RoundToInt(spec.Z);
                bar.tag = ObstacleTag;
                bar.SetActive(true);

                bar.transform.localScale = new Vector3(spec.Width, templateScale.y, templateScale.z);
                bar.transform.position = new Vector3(spec.X, spec.Y, spec.Z);

                if (spec.Kind == ObstacleKind.Sliding)
                    bar.AddComponent<SlidingObstacle>().Initialize(spec.X, spec.SlideRange, spec.SlideSpeed, spec.SlidePhase);

                spawned.Add(bar);
            }

            // from now on the generated bars are "the" obstacles (the authored ones are retired later)
            _newObstacles = spawned;
        }

        private GameObject CreateFallbackBar()
        {
            var bar = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var body = bar.AddComponent<Rigidbody>();
            body.isKinematic = true;
            body.useGravity = false;
            return bar;
        }

        private void SpawnLoot(LevelPlan plan)
        {
            if (_lootTemplate == null)
            {
                if (plan.Loot.Count > 0)
                    Debug.LogWarning("[Level] The scene has no LootContainer to use as a template; the level will have no loot.");
                return;
            }

            foreach (var spec in plan.Loot)
            {
                var position = new Vector3(spec.X, spec.Y, spec.Z);
                var loot = Object.Instantiate(_lootTemplate, position, _lootTemplate.transform.rotation, _root);
                loot.gameObject.name = "Loot z" + Mathf.RoundToInt(spec.Z);
                loot.SetColor(LootPalette.Get(spec.ColorIndex));
            }
        }

        private void RetireAuthoredContent()
        {
            foreach (var bar in _obstacles)
                Retire(bar);
            foreach (var loot in _loot)
                Retire(loot.gameObject);

            _obstacles.Clear();
            _loot.Clear();
            if (_newObstacles != null)
                _obstacles.AddRange(_newObstacles);
        }

        // Deactivating first keeps Start() / physics callbacks of the doomed objects from running this frame.
        private static void Retire(GameObject go)
        {
            go.SetActive(false);
            Object.Destroy(go);
        }

        private void MoveFinishArea(float z)
        {
            if (_finishArea == null)
                return;

            var p = _finishArea.transform.position;
            _finishArea.transform.position = new Vector3(p.x, p.y, z);
        }

        // ---- Visuals ------------------------------------------------------------------------------------

        private void ApplyBackground()
        {
            var camera = Camera.main;
            if (camera != null)
                camera.backgroundColor = Theme.Background;
        }

        private void ApplyBarVisuals()
        {
            if (_obstacles.Count == 0)
                return;

            var templateRenderer = _obstacles[0].GetComponent<Renderer>();
            if (templateRenderer == null || templateRenderer.sharedMaterial == null)
                return;

            var stripes = ProceduralTextures.Stripes(Theme.BarA, Theme.BarB);
            _owned.Add(stripes);

            _barMaterial = new Material(templateRenderer.sharedMaterial) { name = "HazardBar", color = Color.white, mainTexture = stripes };
            _owned.Add(_barMaterial);

            var block = new MaterialPropertyBlock();
            foreach (var bar in _obstacles)
            {
                var barRenderer = bar.GetComponent<Renderer>();
                if (barRenderer == null)
                    continue;

                barRenderer.sharedMaterial = _barMaterial;

                // keep the stripes the same size on bars of different length
                float repeat = Mathf.Max(1f, bar.transform.localScale.x / 0.9f);
                barRenderer.GetPropertyBlock(block);
                block.SetVector("_MainTex_ST", new Vector4(repeat, 1f, 0f, 0f));
                barRenderer.SetPropertyBlock(block);
            }
        }

        private void BuildFloorOverlay()
        {
            if (_floor == null)
                return;

            var shader = Shader.Find("Sprites/Default");
            var floorFilter = _floor.GetComponent<MeshFilter>();
            var floorRenderer = _floor.GetComponent<MeshRenderer>();
            if (shader == null || floorFilter == null || floorRenderer == null || floorFilter.sharedMesh == null)
                return;

            // The authored floor is a unit cube; the overlay is a flat strip lying on its top face.
            Vector3 scale = _floor.transform.lossyScale;
            Vector3 center = _floor.transform.position;
            float halfWidth = scale.x * 0.5f;
            float zMin = center.z - scale.z * 0.5f;
            float zMax = center.z + scale.z * 0.5f;
            float top = center.y + scale.y * 0.5f + FloorOverlayLift;

            var texture = ProceduralTextures.FloorTexture(Theme.FloorA, Theme.FloorB, Theme.Edge);
            _owned.Add(texture);

            var material = new Material(shader) { name = "FloorOverlay", mainTexture = texture };
            _owned.Add(material);

            var mesh = CreateStrip(halfWidth, zMin, zMax, halfWidth * 2f);
            _owned.Add(mesh);

            var go = new GameObject("FloorOverlay");
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(center.x, top, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var meshRenderer = go.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            // the original (lit) floor is completely covered now: skip shading it
            floorRenderer.enabled = false;
        }

        /// <summary>Flat quad on the XZ plane (y = 0). UV: u across the width, v along the track in square cells.</summary>
        private static Mesh CreateStrip(float halfWidth, float zMin, float zMax, float cellSize)
        {
            var mesh = new Mesh { name = "Strip" };
            mesh.vertices = new[]
            {
                new Vector3(-halfWidth, 0f, zMin), new Vector3(halfWidth, 0f, zMin),
                new Vector3(-halfWidth, 0f, zMax), new Vector3(halfWidth, 0f, zMax)
            };
            float v = (zMax - zMin) / Mathf.Max(0.01f, cellSize);
            mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, v), new Vector2(1f, v) };
            mesh.triangles = new[] { 0, 2, 1, 2, 3, 1 };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private void BuildFinishGate()
        {
            float z = Plan.FinishZ;
            var shader = Shader.Find("Sprites/Default");
            var gate = new GameObject("FinishGate").transform;
            gate.SetParent(_root, false);
            gate.position = new Vector3(0f, 0f, z);

            // posts + beam use the (lit) hazard material so they match the bars
            Material postMaterial = _barMaterial != null ? new Material(_barMaterial) { name = "GatePost", mainTexture = null, color = Theme.BarA } : null;
            if (postMaterial != null)
                _owned.Add(postMaterial);

            CreateBlock(gate, "PostL", new Vector3(-1.95f, 1.6f, 0f), new Vector3(0.36f, 3.2f, 0.36f), postMaterial);
            CreateBlock(gate, "PostR", new Vector3(1.95f, 1.6f, 0f), new Vector3(0.36f, 3.2f, 0.36f), postMaterial);
            CreateBlock(gate, "Beam", new Vector3(0f, 3.25f, 0f), new Vector3(4.26f, 0.36f, 0.36f), postMaterial);

            // chequered line on the floor
            if (shader != null)
            {
                var texture = ProceduralTextures.Checkerboard(Color.white, new Color(0.12f, 0.12f, 0.14f), 14, 3);
                _owned.Add(texture);

                var material = new Material(shader) { name = "FinishLine", mainTexture = texture };
                _owned.Add(material);

                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.name = "FinishLine";
                Object.Destroy(quad.GetComponent<Collider>());
                quad.transform.SetParent(gate, false);
                quad.transform.localPosition = new Vector3(0f, FloorTop() + 0.009f, 0f);
                quad.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                quad.transform.localScale = new Vector3(3.5f, 0.75f, 1f);

                var quadRenderer = quad.GetComponent<MeshRenderer>();
                quadRenderer.sharedMaterial = material;
                quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                quadRenderer.receiveShadows = false;
            }

            FinishPosition = gate.position;
        }

        private float FloorTop()
        {
            return _floor != null ? _floor.transform.position.y + _floor.transform.lossyScale.y * 0.5f : 0.106f;
        }

        private static void CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 scale, Material material)
        {
            var block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            Object.Destroy(block.GetComponent<Collider>());
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localScale = scale;

            var renderer = block.GetComponent<MeshRenderer>();
            if (material != null)
                renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
        }

        // ---- Diagnostics ---------------------------------------------------------------------------------

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void ReportProblems()
        {
            var problems = LevelGenerator.Validate(Plan);
            for (int i = 0; i < problems.Count; i++)
                Debug.LogWarning("[Level " + Plan.Level + "] " + problems[i]);
        }
    }
}
