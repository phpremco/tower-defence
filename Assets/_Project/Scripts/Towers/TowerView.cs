using Game.Combat;
using Game.Core;
using UnityEngine;

namespace Game.Towers
{
    /// <summary>
    /// The look of a tower: an armoured pedestal with a procedural weapon head that turns towards its target and
    /// kicks back (or spins) when it fires, one star per level, a health bar once damaged and a dark husk when
    /// destroyed. A gate has no pedestal: its gun sits on the crossbeam that <see cref="GateView"/> builds.
    /// Light top and dark bottom make the tower stand out from the plane.
    /// </summary>
    [RequireComponent(typeof(Tower))]
    public sealed class TowerView : MonoBehaviour
    {
        private const float StarSize = 0.17f;
        private const float StarSpacing = 0.26f;
        private const float StarHeight = 1.4f;
        private const float StarEmission = 3f;
        private const float HeadEmission = 0.8f;
        private const float BarrelEmission = 1.5f;
        private const float BarWidth = 1f;
        private const float BarHeight = 0.1f;
        private const float BarY = 1.7f;
        private const float BarEmission = 1.5f;
        private const float WallOverhang = 0.05f;
        private const float GateHeadHeight = 1.17f;
        private const float LevelGrowth = 0.07f;
        private const float RecoilDistance = 0.14f;
        private const float RecoilRecovery = 6f;
        private const float SpinKick = 1080f;
        private const float SpinDecay = 2.2f;
        private const float MuzzleSeconds = 0.07f;
        private const float MuzzleSize = 0.3f;
        private const float MuzzleForward = 0.6f;
        private const float MuzzleEmission = 6f;
        private const float MuzzleWhiteness = 0.6f;
        private const float MuzzleTintWhiteness = 0.2f;
        private static readonly Color StarColor = new Color(1f, 0.9f, 0.3f);
        private static readonly Color HealthyColor = new Color(0.4f, 1f, 0.4f);
        private static readonly Color DamagedColor = new Color(1f, 0.55f, 0.2f);
        private static readonly Color WreckColor = new Color(0.12f, 0.12f, 0.13f);
        // A hurt tower smokes and throws sparks in proportion to the damage; a wreck only smoulders.
        private static readonly Color SmokeColor = new Color(0.2f, 0.19f, 0.18f, 0.35f);
        private static readonly Color SparkColor = new Color(1f, 0.8f, 0.4f) * 2f;
        private const float SmokeBelow = 0.75f;
        private const float SmokeRate = 5f;
        private const float SparkRate = 2.5f;
        private const float WreckSmokeRate = 2.5f;
        private const float ParticleHeight = 0.55f;
        private const float BarClearance = 0.35f;
        // The flash sits just past the mouth of a modelled barrel, along the muzzle bone's +Y.
        private const float ArtFlashForward = 0.06f;
        private const float ArtFlashSize = 0.22f;
        private const float StarModelScale = 1f;

        private readonly GameObject[] _stars = new GameObject[TowerDefinition.LevelCount];

        private Tower _tower;
        private Transform _head;
        private WorldBar _worldBar;
        private Transform _bar;
        private Renderer _barRenderer;
        private Renderer[] _modelRenderers;
        private Material[][] _modelMaterials;
        private bool _built;
        private Transform _barrel;
        private MeshFilter _headMesh;
        private MeshFilter _barrelMesh;
        private Material[] _shapeMaterials;
        private Renderer _pedestal;
        private int _shapeLevel = -1;
        private TowerModelFactory.Parts _parts;
        private float _recoil;
        private float _sinceFire = float.MaxValue / 2f;
        private float _restYaw;
        private float _idlePhase;
        private float _spin;
        private float _spinAngle;
        private Transform _muzzle;
        private float _muzzleTime = float.PositiveInfinity;
        private ParticleSystem _smoke;
        private ParticleSystem _sparks;
        private TowerArtView _art;
        private MuzzleFlashView _paintedMuzzle;

        private void Awake()
        {
            _tower = GetComponent<Tower>();
            _idlePhase = Random.Range(0f, Mathf.PI * 2f);
            _tower.LevelChanged += OnLevelChanged;
            _tower.StateChanged += OnStateChanged;
            _tower.Fired += OnFired;
            // A boss can fling the tower or weigh down the gate; that look lives in its own view.
            gameObject.AddComponent<TowerDisplacementView>();
            _smoke = ParticleKit.Create(transform, "Smoke", Vector3.up * ParticleHeight, SmokeColor, 1.2f, 2.2f, 0.2f, 0.45f, 0.3f, 0.7f, -0.12f, 30, 0.25f);
            _sparks = ParticleKit.Create(transform, "Sparks", Vector3.up * ParticleHeight, SparkColor, 0.3f, 0.7f, 0.03f, 0.06f, 2f, 5f, 1.5f, 60, 0.2f);
            SetRate(_smoke, 0f);
            SetRate(_sparks, 0f);
        }

        private static void SetRate(ParticleSystem system, float rate)
        {
            ParticleSystem.EmissionModule emission = system.emission;
            emission.enabled = rate > 0f;
            emission.rateOverTime = rate;
        }

        private Vector3 MuzzlePosition(Vector3 target) => _muzzle.position;

        private void OnDestroy()
        {
            _tower.LevelChanged -= OnLevelChanged;
            _tower.StateChanged -= OnStateChanged;
            _tower.Fired -= OnFired;
        }

        private void OnLevelChanged(Tower tower)
        {
            if (!_built)
            {
                Build(tower.Definition);
                _built = true;
            }

            if (_art != null)
            {
                ShowArtLevel(tower);
            }
            else
            {
                SwapModelForLevel(tower);
            }

            // A blockade only shows its gun once the level has one.
            if (tower.Definition.BlocksGroundEnemies)
            {
                _head.gameObject.SetActive(tower.Definition.GetLevel(tower.LevelIndex).Damage > 0f);
            }

            ShowStars(tower);
        }

        // The imported look swaps its tier; the bar rides above the new top and the smoke rises from its Smoke socket
        // (a wreck keeps the old height). The flash leaves the old tier's barrel before that tier goes.
        private void ShowArtLevel(Tower tower)
        {
            _muzzle.SetParent(_head, false);
            _art.ShowLevel(tower.LevelIndex);
            _bar.localPosition = new Vector3(0f, _art.Top + BarClearance, 0f);
            if (!tower.IsWrecked)
            {
                _smoke.transform.localPosition = _art.SmokeLocal;
                _sparks.transform.localPosition = _art.SmokeLocal;
            }
        }

        private void ShowStars(Tower tower)
        {
            for (int i = 0; i < _stars.Length; i++)
            {
                if (_stars[i] != null)
                {
                    _stars[i].SetActive(i <= tower.LevelIndex && !tower.IsWrecked);
                }
            }
        }

        // Visual upgrades of the procedural shape: its head is rebuilt for the new level.
        private void SwapModelForLevel(Tower tower)
        {
            if (_headMesh != null)
            {
                RebuildShape(tower.Definition, tower.LevelIndex);
            }
        }

        private void RememberMaterials(Renderer[] renderers)
        {
            _modelRenderers = renderers;
            _modelMaterials = new Material[renderers.Length][];
            for (int i = 0; i < renderers.Length; i++)
            {
                _modelMaterials[i] = renderers[i].sharedMaterials;
            }
        }

        private void OnStateChanged(Tower tower)
        {
            // A gate always shows its bar: it is the line that holds the road. Towers only show it once hurt.
            bool gate = tower.Definition.BlocksGroundEnemies;
            bool damaged = gate ? !tower.IsWrecked : tower.Health.Fraction < 1f && !tower.IsWrecked;
            _bar.gameObject.SetActive(damaged);
            if (damaged)
            {
                float fraction = tower.Health.Fraction;
                if (_worldBar != null)
                {
                    _worldBar.Set(fraction, 0f, true);
                    _worldBar.SetColour(Color.Lerp(DamagedColor, HealthyColor, fraction));
                }
                else
                {
                    _bar.localScale = new Vector3(BarWidth * Mathf.Max(0.001f, fraction), BarHeight, BarHeight);
                    _barRenderer.sharedMaterial = PlaceholderVisuals.MaterialFor(Color.Lerp(DamagedColor, HealthyColor, fraction), BarEmission);
                }
            }

            float hurt = Mathf.Clamp01(1f - tower.Health.Fraction / SmokeBelow);
            SetRate(_smoke, tower.IsWrecked ? WreckSmokeRate : hurt * SmokeRate);
            SetRate(_sparks, tower.IsWrecked ? 0f : hurt * SparkRate);
            if (_art != null)
            {
                _art.SetWrecked(tower.IsWrecked);
            }

            ApplyMaterials();

            ShowStars(tower);
        }

        // The current look of the model: wrecked or whole.
        private void ApplyMaterials()
        {
            if (_modelRenderers == null)
            {
                return;
            }

            Material wreck = PlaceholderVisuals.MaterialFor(WreckColor, 0f);
            for (int i = 0; i < _modelRenderers.Length; i++)
            {
                _modelRenderers[i].sharedMaterials = _tower.IsWrecked ? Wrecked(wreck, _modelMaterials[i].Length) : _modelMaterials[i];
            }
        }

        private static Material[] Wrecked(Material wreck, int count)
        {
            var result = new Material[count];
            for (int m = 0; m < count; m++)
            {
                result[m] = wreck;
            }

            return result;
        }

        private void Build(TowerDefinition definition)
        {
            if (definition.Art != null)
            {
                BuildArt(definition, definition.Art);
                return;
            }

            bool wall = definition.BlocksGroundEnemies;
            bool shaped = definition.Shape != TowerShape.None;
            Renderer pedestal = null;
            if (wall)
            {
                // A gate across the road, with its own look and wreck animation. Its gun sits on the crossbeam and
                // appears with the first gun upgrade.
                gameObject.AddComponent<GateView>().Initialize(_tower, _tower.Span + WallOverhang, definition.Color, definition.GateArt);
            }
            else if (shaped)
            {
                pedestal = Part("Pedestal", transform, TowerModelFactory.Pedestal(), TowerModelFactory.CreateMaterials(definition.Color));
                pedestal.transform.localScale = Vector3.one * TowerModelFactory.PedestalScale;
            }
            else
            {
                PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, WreckColor, transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.85f, 0.2f, 0.85f));
            }

            var head = new GameObject("Head").transform;
            head.SetParent(transform, false);
            head.localPosition = new Vector3(0f, wall ? GateHeadHeight : shaped ? TowerModelFactory.PedestalHeight : 0.8f, 0f);
            _head = head;

            if (shaped)
            {
                BuildShape(definition, pedestal);
            }
            else
            {
                GameObject block = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, definition.Color, head, Vector3.zero, new Vector3(0.6f, 0.5f, 0.6f), HeadEmission);
                GameObject barrel = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cylinder, definition.Color, head, new Vector3(0f, 0f, 0.45f), new Vector3(0.18f, 0.35f, 0.18f), BarrelEmission);
                barrel.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                RememberMaterials(new[] { block.GetComponent<Renderer>(), barrel.GetComponent<Renderer>() });
            }

            // A flash at the mouth of the barrel on every shot; it rides the barrel so recoil carries it along.
            Transform muzzleParent = _barrel != null ? _barrel : head;
            Vector3 muzzleAt = _barrel != null ? _parts.Muzzle : new Vector3(0f, 0f, MuzzleForward);
            Color flashColor = Color.Lerp(definition.Color, Color.white, MuzzleWhiteness);
            _muzzle = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, flashColor, muzzleParent, muzzleAt, Vector3.one * MuzzleSize, MuzzleEmission).transform;
            _muzzle.gameObject.SetActive(false);
            _paintedMuzzle = PaintedMuzzle(definition);
            _tower.MuzzleProvider = MuzzlePosition;

            BuildStars(null);
            BuildBar();
        }

        // The imported look: the model brings its own pad, head and rank stars; a gate keeps its pylons and beam and
        // carries the gun on the beam, with its stars on the beam beside it.
        private void BuildArt(TowerDefinition definition, TowerArtDefinition art)
        {
            bool wall = definition.BlocksGroundEnemies;
            if (wall)
            {
                gameObject.AddComponent<GateView>().Initialize(_tower, _tower.Span + WallOverhang, definition.Color, definition.GateArt);
            }

            _head = new GameObject("Head").transform;
            _head.SetParent(transform, false);
            _head.localPosition = new Vector3(0f, wall ? GateHeadHeight : 0f, 0f);
            _art = gameObject.AddComponent<TowerArtView>();
            _art.Initialize(_tower, art, _head);

            Color flashColor = Color.Lerp(definition.Color, Color.white, MuzzleWhiteness);
            _muzzle = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, flashColor, _head, Vector3.zero, Vector3.one * ArtFlashSize, MuzzleEmission).transform;
            _muzzle.gameObject.SetActive(false);
            _paintedMuzzle = PaintedMuzzle(definition);
            _tower.MuzzleProvider = _art.MuzzleFor;

            if (wall)
            {
                BuildStars(art.Star);
            }

            BuildBar();
        }

        // One star per level above the tower; the rank star model when there is one, else a glowing bead.
        private void BuildStars(GameObject starModel)
        {
            float startX = -StarSpacing * (_stars.Length - 1) * 0.5f;
            for (int i = 0; i < _stars.Length; i++)
            {
                var at = new Vector3(startX + i * StarSpacing, StarHeight, 0f);
                if (starModel == null)
                {
                    _stars[i] = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Sphere, StarColor, transform, at, Vector3.one * StarSize, StarEmission);
                    continue;
                }

                GameObject star = Instantiate(starModel, transform, false);
                star.transform.localPosition = at;
                star.transform.localScale = Vector3.one * StarModelScale;
                foreach (Renderer renderer in star.GetComponentsInChildren<Renderer>())
                {
                    renderer.sharedMaterial = PlaceholderVisuals.MaterialFor(StarColor, StarEmission);
                }

                _stars[i] = star;
            }
        }

        private void BuildBar()
        {
            _worldBar = WorldBar.Create(MarkerArt.Active, transform, new Vector3(0f, BarY, 0f), BarWidth, BarHeight, HealthyColor, HealthyColor, false);
            if (_worldBar != null)
            {
                _bar = _worldBar.Root;
                return;
            }

            GameObject bar = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, HealthyColor, transform, new Vector3(0f, BarY, 0f), new Vector3(BarWidth, BarHeight, BarHeight), BarEmission);
            _bar = bar.transform;
            _barRenderer = bar.GetComponent<Renderer>();
            _bar.gameObject.SetActive(false);
        }

        private static Renderer Part(string name, Transform parent, Mesh mesh, Material[] materials)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            if (materials != null)
            {
                renderer.sharedMaterials = materials;
            }

            return renderer;
        }

        // The procedural model: a head that turns and a barrel group that kicks back. Rebuilt on every level so
        // upgrades show. The pedestal shares the materials so a wreck darkens as one.
        private void BuildShape(TowerDefinition definition, Renderer pedestal)
        {
            _pedestal = pedestal;
            Renderer headRenderer = Part("ShapeHead", _head, null, null);
            _headMesh = headRenderer.GetComponent<MeshFilter>();
            Renderer barrelRenderer = Part("ShapeBarrel", _head, null, null);
            _barrel = barrelRenderer.transform;
            _barrelMesh = barrelRenderer.GetComponent<MeshFilter>();
            _modelRenderers = pedestal != null ? new[] { pedestal, headRenderer, barrelRenderer } : new[] { headRenderer, barrelRenderer };
            RebuildShape(definition, 0);
        }

        private void RebuildShape(TowerDefinition definition, int level)
        {
            if (level == _shapeLevel)
            {
                return;
            }

            _shapeLevel = level;
            _parts = TowerModelFactory.Create(definition.Shape, level);

            // Each level has its own hotter palette and a pedestal with one more rank ring.
            _shapeMaterials = TowerModelFactory.CreateMaterials(definition.Color, level);
            _modelMaterials = new Material[_modelRenderers.Length][];
            for (int i = 0; i < _modelRenderers.Length; i++)
            {
                _modelMaterials[i] = _shapeMaterials;
            }

            ApplyMaterials();

            if (_pedestal != null)
            {
                _pedestal.GetComponent<MeshFilter>().sharedMesh = TowerModelFactory.Pedestal(level);
            }

            _head.localScale = Vector3.one * (_parts.Scale * (1f + level * LevelGrowth));
            _headMesh.sharedMesh = _parts.Head;
            _barrelMesh.sharedMesh = _parts.Barrel;
            _barrel.localPosition = _parts.BarrelPivot;
            if (_muzzle != null)
            {
                _muzzle.localPosition = _parts.Muzzle;
            }
        }

        private void Update()
        {
            if (_art == null)
            {
                UpdateIdleSweep();
            }

            if (_barrel != null && (_recoil > 0f || _spin > 0f))
            {
                _recoil = Mathf.Max(0f, _recoil - RecoilRecovery * Time.deltaTime);
                _spin = Mathf.Max(0f, _spin - SpinDecay * Time.deltaTime);
                _spinAngle += _spin * SpinKick * Time.deltaTime;
                _barrel.localPosition = _parts.BarrelPivot - _parts.RecoilAxis * (_recoil * RecoilDistance);
                _barrel.localRotation = _parts.Rotary ? Quaternion.AngleAxis(_spinAngle, Vector3.forward) : Quaternion.identity;
            }

            if (_muzzle != null && _paintedMuzzle == null && _muzzleTime <= MuzzleSeconds)
            {
                _muzzleTime += FeedbackClock.DeltaTime;
                bool lit = _muzzleTime <= MuzzleSeconds;
                _muzzle.gameObject.SetActive(lit);
                _muzzle.localScale = Vector3.one * ((_art != null ? ArtFlashSize : MuzzleSize) * (1.4f - _muzzleTime / MuzzleSeconds));
            }
        }

        private void UpdateIdleSweep()
        {
            if (_head == null || _tower.IsWrecked)
            {
                // A wreck has no crew left to look around.
                return;
            }

            _sinceFire += Time.deltaTime;
            if (!TowerIdleSweep.IsIdle(_sinceFire))
            {
                return;
            }

            float sweep = TowerIdleSweep.Degrees(_sinceFire, _idlePhase);
            _head.rotation = Quaternion.Euler(0f, _restYaw + sweep, 0f);
        }

        // The painted flash of the weapon, when the effect library has one; the glowing sphere stays as the stand-in.
        private MuzzleFlashView PaintedMuzzle(TowerDefinition definition)
        {
            if (!Services.TryGet(out VfxLibrary library))
            {
                return null;
            }

            var view = gameObject.AddComponent<MuzzleFlashView>();
            if (view.Initialize(library.WeaponFor(definition.ProjectileStyle), Color.Lerp(definition.Color, Color.white, MuzzleTintWhiteness), ModelQuality.IsLow, Camera.main))
            {
                return view;
            }

            Destroy(view);
            return null;
        }

        private void OnFired(Vector3 targetPosition)
        {
            if (_art != null)
            {
                _art.OnFired(targetPosition);
                _muzzleTime = 0f;
                _muzzle.SetParent(_art.LastMuzzle, false);
                _muzzle.localPosition = Vector3.up * ArtFlashForward;
                // The muzzle bone points its +Y out of the barrel.
                if (_paintedMuzzle != null)
                {
                    _paintedMuzzle.Flash(_muzzle, Vector3.up);
                }

                return;
            }

            _sinceFire = 0f;
            _recoil = 1f;
            _spin = 1f;
            _muzzleTime = 0f;
            if (_paintedMuzzle != null)
            {
                _paintedMuzzle.Flash(_muzzle, Vector3.forward);
            }

            Vector3 flat = targetPosition - transform.position;
            flat.y = 0f;
            if (flat.sqrMagnitude > 0.0001f)
            {
                _head.rotation = Quaternion.LookRotation(flat);
                _restYaw = _head.eulerAngles.y;
            }
        }
    }
}
