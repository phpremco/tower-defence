using System;
using System.Collections.Generic;

namespace Game.Core
{
    /// <summary>
    /// The only allowed global: a registry for platform services (save, input, achievements, settings).
    /// Gameplay asks for an interface here and never knows which platform implements it.
    /// </summary>
    public static class Services
    {
        private static readonly Dictionary<Type, object> Instances = new Dictionary<Type, object>();

        public static void Register<T>(T instance) where T : class
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            Instances[typeof(T)] = instance;
        }

        public static T Get<T>() where T : class
        {
            if (Instances.TryGetValue(typeof(T), out object instance))
            {
                return (T)instance;
            }

            throw new InvalidOperationException($"Service {typeof(T).Name} is not registered.");
        }

        public static bool TryGet<T>(out T instance) where T : class
        {
            bool found = Instances.TryGetValue(typeof(T), out object value);
            instance = found ? (T)value : null;
            return found;
        }

        public static void Clear()
        {
            Instances.Clear();
        }
    }
}
