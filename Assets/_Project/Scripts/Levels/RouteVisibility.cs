using System.Collections.Generic;
using UnityEngine;

namespace Game.Levels
{
    /// <summary>Shows a flight route only while the current or the upcoming wave actually sends enemies along it.</summary>
    public sealed class RouteVisibility : MonoBehaviour
    {
        private LevelContext _context;
        private Dictionary<int, GameObject> _routes;

        public void Initialize(LevelContext context, Dictionary<int, GameObject> routes)
        {
            _context = context;
            _routes = routes;
            _context.RoutesChanged.Subscribe(Apply);
            Apply();
        }

        private void OnDestroy()
        {
            _context?.RoutesChanged.Unsubscribe(Apply);
        }

        private void Apply()
        {
            foreach (KeyValuePair<int, GameObject> route in _routes)
            {
                route.Value.SetActive(_context.ActiveRoutes.Contains(route.Key));
            }
        }
    }
}
