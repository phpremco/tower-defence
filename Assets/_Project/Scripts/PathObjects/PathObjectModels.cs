using Game.Core;
using UnityEngine;

namespace Game.PathObjects
{
    /// <summary>The procedural mesh behind each path object look, shared by the field view and the HUD cards.</summary>
    public static class PathObjectModels
    {
        public static Mesh MeshFor(PathObjectLook look)
        {
            switch (look)
            {
                case PathObjectLook.Mine: return StructureModelFactory.Mine();
                case PathObjectLook.FieldEmitter: return StructureModelFactory.FieldEmitter();
                case PathObjectLook.Chrono: return StructureModelFactory.FieldEmitter();
                case PathObjectLook.LavaBall: return StructureModelFactory.LavaBall();
                case PathObjectLook.GroundDrone: return StructureModelFactory.GroundDrone();
                case PathObjectLook.Barrel: return StructureModelFactory.Barrel();
                default: return StructureModelFactory.AirNet();
            }
        }

        /// <summary>A display copy of the path object, standing on y = 0 under <paramref name="parent"/>: the imported model when it has one.</summary>
        public static GameObject Build(Transform parent, PathObjectDefinition definition)
        {
            var go = new GameObject(definition.DisplayName);
            go.transform.SetParent(parent, false);
            if (definition.Art != null)
            {
                ArtModel.CreateStill(go.transform, definition.Art);
                return go;
            }

            go.AddComponent<MeshFilter>().sharedMesh = MeshFor(definition.Look);
            go.AddComponent<MeshRenderer>().sharedMaterials = ModelPalette.Create(definition.Color);
            return go;
        }
    }
}
