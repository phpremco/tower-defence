using Game.Core;
using Game.Drones;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>
    /// The base's drone bays: each of the first drones launched lives in one (its home is the bay's dock). When the drone
    /// settles at home its bay's lid swings open, the drone sinks onto the dock and the lid closes over it; when it sets
    /// off the lid opens, the drone rises out and the lid closes again. Follows the drones' home changes; moves only the
    /// lids and the drones' models (through <see cref="DroneView.SetDock"/>). Pure view code.
    /// </summary>
    public sealed class BaseDockView : MonoBehaviour
    {
        // A drone that only touches home (a harvester handing in its load) does not dock: it must stay this long.
        private const float SettleSeconds = 0.6f;

        // A lid opens faster for a drone that is already leaving.
        private const float LeavingDoorSpeed = 2f;

        // A home point this close to a dock (flat) is that dock's drone.
        private const float DockMatch = 0.25f;

        // A drone folds in (its rotors tuck) to fit the well: its model shrinks to this share as it settles.
        private const float DockedScale = 0.6f;

        private BaseArtDefinition _art;
        private Transform[] _docks;
        private Transform[][] _lids;
        private Quaternion[][] _openRotations;
        private Quaternion[][] _closedRotations;
        private Drone[] _drones;
        private DroneView[] _views;
        private float[] _open;
        private float[] _depth;
        private float[] _homeSince;
        private GameEvent<Drone> _launched;

        /// <summary>The bays found on the model: dock sockets and lids (near and far mesh each), numbered alike.</summary>
        public void Initialize(BaseArtDefinition art, ArtModel model)
        {
            _art = art;
            _docks = model.Numbered(art.DockSocketPrefix);
            int count = _docks.Length;
            _lids = new Transform[count][];
            _openRotations = new Quaternion[count][];
            _closedRotations = new Quaternion[count][];
            _drones = new Drone[count];
            _views = new DroneView[count];
            _open = new float[count];
            _depth = new float[count];
            _homeSince = new float[count];
            for (int i = 0; i < count; i++)
            {
                Renderer[] lid = model.RenderersEndingWith($"{art.BayDoorPrefix}_{i + 1}");
                _lids[i] = new Transform[lid.Length];
                _openRotations[i] = new Quaternion[lid.Length];
                _closedRotations[i] = new Quaternion[lid.Length];
                for (int k = 0; k < lid.Length; k++)
                {
                    Transform part = lid[k].transform;
                    _lids[i][k] = part;
                    _openRotations[i][k] = part.localRotation;
                    _closedRotations[i][k] = Closed(art, part, _docks[i]);
                }

                _homeSince[i] = -1f;
                PoseLid(i, 0f);
            }
        }

        /// <summary>Hears about every drone launched from now on; the ones whose home is a bay move in.</summary>
        public void Bind(GameEvent<Drone> launched)
        {
            _launched = launched;
            _launched.Subscribe(OnLaunched);
        }

        /// <summary>Closes every lid on a still copy of the base (a menu or a cutscene), as it stands with no drone about.</summary>
        public static void CloseLids(BaseArtDefinition art, ArtModel model)
        {
            Transform[] docks = model.Numbered(art.DockSocketPrefix);
            for (int i = 0; i < docks.Length; i++)
            {
                foreach (Renderer lid in model.RenderersEndingWith($"{art.BayDoorPrefix}_{i + 1}"))
                {
                    lid.transform.localRotation = Closed(art, lid.transform, docks[i]);
                }
            }
        }

        private void OnDestroy()
        {
            _launched?.Unsubscribe(OnLaunched);
            for (int i = 0; i < _drones.Length; i++)
            {
                if (_drones[i] != null)
                {
                    _drones[i].HomeChanged -= OnHomeChanged;
                }
            }
        }

        // The export stands open; the lid closes by turning about its hinge (local Z) so its free edge falls over the well.
        private static Quaternion Closed(BaseArtDefinition art, Transform lid, Transform dock)
        {
            float across = dock != null ? lid.parent.InverseTransformPoint(dock.position).x - lid.localPosition.x : 0f;
            float sign = across > 0f ? -1f : 1f;
            return lid.localRotation * Quaternion.AngleAxis(sign * art.DoorClosedDegrees, Vector3.forward);
        }

        // A new drone starts in its bay, lid shut, and leaves from there as soon as it has work.
        private void OnLaunched(Drone drone)
        {
            for (int i = 0; i < _docks.Length; i++)
            {
                Vector3 offset = _docks[i].position - drone.Home;
                if (_drones[i] != null || offset.x * offset.x + offset.z * offset.z > DockMatch * DockMatch)
                {
                    continue;
                }

                _drones[i] = drone;
                _views[i] = drone.GetComponent<DroneView>();
                _depth[i] = 1f;
                // Launched at home it is already settled; one that sets off at once says so on its first frame.
                _homeSince[i] = drone.IsHome ? Time.time - SettleSeconds : -1f;
                drone.HomeChanged += OnHomeChanged;
                return;
            }
        }

        private void OnHomeChanged(Drone drone, bool home)
        {
            for (int i = 0; i < _drones.Length; i++)
            {
                if (_drones[i] == drone)
                {
                    _homeSince[i] = home ? Time.time : -1f;
                }
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < _drones.Length; i++)
            {
                if (_drones[i] == null)
                {
                    continue;
                }

                bool wanted = _homeSince[i] >= 0f && Time.time - _homeSince[i] >= SettleSeconds;
                float target = wanted ? 1f : 0f;
                if (!Mathf.Approximately(_depth[i], target))
                {
                    // The lid opens first; only an open lid lets the drone through.
                    _open[i] = Mathf.MoveTowards(_open[i], 1f, dt * (wanted ? 1f : LeavingDoorSpeed) / _art.DoorSeconds);
                    if (_open[i] >= 1f)
                    {
                        _depth[i] = Mathf.MoveTowards(_depth[i], target, dt / _art.DockSeconds);
                    }
                }
                else
                {
                    _open[i] = Mathf.MoveTowards(_open[i], 0f, dt / _art.DoorSeconds);
                }

                PoseLid(i, _open[i]);
                PoseDrone(i);
            }
        }

        private void PoseLid(int bay, float open)
        {
            float eased = open * open * (3f - 2f * open);
            Transform[] lid = _lids[bay];
            for (int k = 0; k < lid.Length; k++)
            {
                lid[k].localRotation = Quaternion.Slerp(_closedRotations[bay][k], _openRotations[bay][k], eased);
            }
        }

        private void PoseDrone(int bay)
        {
            if (_views[bay] == null)
            {
                return;
            }

            float depth = _depth[bay];
            float eased = depth * depth * (3f - 2f * depth);
            Vector3 down = _docks[bay].position - _drones[bay].transform.position;
            _views[bay].SetDock(down * eased, eased, Mathf.Lerp(1f, DockedScale, eased));
        }
    }
}
