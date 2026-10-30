using System.Collections;
using Game.Core;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// What the base does when it falls: a flash, a fireball, sparks, debris thrown out of the structure, the
    /// lights going out and a column of smoke while the buildings sink and tilt. Sits on the base's root object;
    /// the sound and the camera kick come from their own listeners on the same event.
    /// </summary>
    public sealed class BaseView : MonoBehaviour
    {
        private const float FlashIntensity = 14f;
        private const float FlashRange = 10f;
        private const float FlashDecay = 3f;
        private const int FireballCount = 90;
        private const int SparkCount = 70;
        private const float SmokeRate = 30f;
        private const float SmokeSeconds = 9f;
        private const int DebrisCount = 18;
        private const float DebrisSpeed = 4.5f;
        private const float DebrisUp = 7f;
        private const float DebrisSpin = 540f;
        private const float Gravity = 16f;
        private const float ShakeSeconds = 1.1f;
        private const float ShakeAmount = 0.12f;
        private const float SinkDepth = 0.4f;
        private const float TiltDegrees = 9f;
        private const float SettleSeconds = 1.6f;
        private const float DarkenFactor = 0.3f;
        private static readonly Color FlashColor = new Color(1f, 0.75f, 0.45f);
        private static readonly Color FireColor = new Color(1f, 0.55f, 0.15f);
        private static readonly Color SparkColor = new Color(1f, 0.9f, 0.5f);
        private static readonly Color SmokeColor = new Color(0.12f, 0.1f, 0.1f);
        private static readonly Color DebrisColor = new Color(0.16f, 0.17f, 0.2f);

        private LevelContext _context;
        private Material[] _materials;

        public void Bind(LevelContext context)
        {
            _context = context;
            _materials = GetComponentInChildren<MeshRenderer>().sharedMaterials;
            _context.BaseHealth.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_context != null)
            {
                _context.BaseHealth.Died -= OnDied;
            }
        }

        private void OnDied() => StartCoroutine(Explode());

        private IEnumerator Explode()
        {
            foreach (Spinner spinner in GetComponentsInChildren<Spinner>())
            {
                spinner.enabled = false;
            }

            foreach (EmissionPulse pulse in GetComponentsInChildren<EmissionPulse>())
            {
                pulse.enabled = false;
            }

            foreach (Light light in GetComponentsInChildren<Light>())
            {
                light.enabled = false;
            }

            Light flash = new GameObject("Flash").AddComponent<Light>();
            flash.transform.SetParent(transform, false);
            flash.transform.localPosition = Vector3.up * 1.2f;
            flash.type = LightType.Point;
            flash.color = FlashColor;
            flash.range = FlashRange;
            flash.intensity = FlashIntensity;

            ParticleSystem fire = ParticleKit.Create(transform, "Fireball", Vector3.up * 0.8f, FireColor, 0.5f, 1.1f, 0.7f, 1.8f, 1.5f, 5f, -0.6f, FireballCount, 0.7f);
            ParticleSystem sparks = ParticleKit.Create(transform, "Sparks", Vector3.up * 0.9f, SparkColor, 0.5f, 1.4f, 0.06f, 0.14f, 5f, 11f, 9f, SparkCount, 0.3f);
            ParticleSystem smoke = ParticleKit.Create(transform, "Smoke", Vector3.up * 1f, SmokeColor, 2.5f, 4.5f, 0.8f, 1.8f, 0.6f, 1.4f, -0.25f, 160, 0.8f);
            fire.Emit(FireballCount);
            sparks.Emit(SparkCount);
            ParticleKit.SetRate(smoke, SmokeRate);
            Darken();

            var debris = new Transform[DebrisCount];
            var velocities = new Vector3[DebrisCount];
            var spins = new Vector3[DebrisCount];
            for (int i = 0; i < DebrisCount; i++)
            {
                Vector3 size = new Vector3(Random.Range(0.12f, 0.3f), Random.Range(0.08f, 0.22f), Random.Range(0.12f, 0.3f));
                GameObject chunk = PlaceholderVisuals.CreatePrimitive(PrimitiveType.Cube, DebrisColor, transform.parent, transform.position + Vector3.up * Random.Range(0.4f, 1.6f), size);
                debris[i] = chunk.transform;
                Vector2 flat = Random.insideUnitCircle.normalized * Random.Range(0.4f, 1f) * DebrisSpeed;
                velocities[i] = new Vector3(flat.x, Random.Range(0.5f, 1f) * DebrisUp, flat.y);
                spins[i] = Random.onUnitSphere * DebrisSpin;
            }

            float ground = transform.position.y;
            Vector3 rest = transform.localPosition;
            Quaternion upright = transform.localRotation;
            Quaternion tilted = upright * Quaternion.AngleAxis(TiltDegrees, Random.onUnitSphere);
            float age = 0f;
            while (age < SmokeSeconds)
            {
                float dt = Time.deltaTime;
                age += dt;
                flash.intensity = FlashIntensity * Mathf.Exp(-FlashDecay * age);

                // Shake hard first, then sink and tilt as the structure gives way.
                float shake = Mathf.Max(0f, 1f - age / ShakeSeconds);
                float settle = Mathf.Clamp01((age - ShakeSeconds * 0.5f) / SettleSeconds);
                Vector3 jitter = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * (ShakeAmount * shake);
                transform.localPosition = rest + jitter + Vector3.down * (SinkDepth * settle);
                transform.localRotation = Quaternion.Slerp(upright, tilted, settle);

                for (int i = 0; i < DebrisCount; i++)
                {
                    if (debris[i].position.y <= ground && velocities[i].y < 0f)
                    {
                        continue;
                    }

                    velocities[i] += Vector3.down * (Gravity * dt);
                    debris[i].position += velocities[i] * dt;
                    debris[i].Rotate(spins[i] * dt, Space.World);
                    if (debris[i].position.y < ground)
                    {
                        debris[i].position = new Vector3(debris[i].position.x, ground, debris[i].position.z);
                    }
                }

                yield return null;
            }

            ParticleKit.SetRate(smoke, 0f);
            flash.enabled = false;
        }

        // The lights go out: every material of the base loses its glow and most of its colour.
        private void Darken()
        {
            foreach (Material material in _materials)
            {
                material.SetColor("_BaseColor", material.GetColor("_BaseColor") * DarkenFactor);
                material.SetColor("_EmissionColor", Color.black);
            }
        }
    }
}
