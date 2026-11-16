using Game.Boot;
using Game.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Editor
{
    /// <summary>
    /// Round 72: the credits screen's data (sections of string-table keys plus the font licences that must ship with the
    /// fonts) and its place on the Boot scene's bootstrapper. The full generator runs the data step before the scenes;
    /// the menu entry also wires the existing Boot scene, so it runs on its own without regenerating anything else.
    /// The sources and licences behind each line are listed in Docs/LICENCES.md.
    /// </summary>
    public static partial class ProjectGenerator
    {
        private const string CreditsAssetPath = DataRoot + "/Menu/Credits.asset";
        private const string ChakraPetchLicencePath = "Assets/_Project/Art/UI/License_Fonts.txt";
        private const string LiberationSansLicencePath = "Assets/TextMesh Pro/Fonts/LiberationSans - OFL.txt";

        [MenuItem("Tools/Generate/Credits")]
        public static void GenerateCreditsAndWireBoot()
        {
            CreditsDefinition credits = CreateCredits();
            AssetDatabase.SaveAssets();
            int sections = credits.Sections.Length;
            int licences = credits.LicenceTexts.Length;
            WireCreditsIntoBootScene();
            Debug.Log($"Generated the credits ({sections} sections, {licences} licence texts) and wired them into the Boot scene.");
        }

        private static CreditsDefinition CreateCredits()
        {
            EnsureFolder(DataRoot + "/Menu");
            var credits = LoadOrCreate<CreditsDefinition>(CreditsAssetPath);
            credits.Set(
                new[]
                {
                    new CreditsSection("credits.section.game", "credits.game.creator"),
                    new CreditsSection("credits.section.tools", "credits.tools.unity", "credits.tools.blender", "credits.tools.ai", "credits.tools.music"),
                    new CreditsSection("credits.section.fonts", "credits.fonts.chakra", "credits.fonts.liberation"),
                    new CreditsSection("credits.section.sound", "credits.sound.kenney", "credits.sound.lentikula", "credits.sound.pixabay"),
                    new CreditsSection("credits.section.characters", "credits.characters.mpfb", "credits.characters.hair"),
                },
                new[] { LoadLicence(ChakraPetchLicencePath), LoadLicence(LiberationSansLicencePath) });
            EditorUtility.SetDirty(credits);
            return credits;
        }

        private static TextAsset LoadLicence(string path)
        {
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (text == null)
            {
                throw new System.InvalidOperationException($"Licence text {path} is missing; the credits must ship it with its font.");
            }

            return text;
        }

        // Opens the Boot scene on its own, sets the reference and saves it; the scenes that were open come back afterwards.
        private static void WireCreditsIntoBootScene()
        {
            string bootPath = $"{ScenesFolder}/{Core.SceneNames.Boot}.unity";
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            Scene boot = EditorSceneManager.OpenScene(bootPath, OpenSceneMode.Single);
            Bootstrapper bootstrapper = Object.FindAnyObjectByType<Bootstrapper>();
            if (bootstrapper == null)
            {
                throw new System.InvalidOperationException($"No Bootstrapper in {bootPath}: run the full generator first.");
            }

            // Opening a scene unloads assets nobody references yet, so the credits are loaded again by path.
            SetCredits(bootstrapper, AssetDatabase.LoadAssetAtPath<CreditsDefinition>(CreditsAssetPath));
            EditorSceneManager.SaveScene(boot);
            if (previous.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            }
        }

        private static void SetCredits(Bootstrapper bootstrapper, CreditsDefinition credits)
        {
            var so = new SerializedObject(bootstrapper);
            so.FindProperty("_credits").objectReferenceValue = credits;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// Round 72: the shader keepers on their own. The IL2CPP smoke run found the Game/ExhaustPlume shader missing from
        /// the player (the generator's keeper list had grown, the Boot scene still held the old list), so every level threw
        /// while building the base shuttle. Writes the keepers and puts the whole list on the Boot scene's bootstrapper.
        /// </summary>
        [MenuItem("Tools/Generate/Shader Keepers")]
        public static void GenerateShaderKeepersAndWireBoot()
        {
            string[] paths = CreateShaderKeepers();
            AssetDatabase.SaveAssets();
            string bootPath = $"{ScenesFolder}/{Core.SceneNames.Boot}.unity";
            SceneSetup[] previous = EditorSceneManager.GetSceneManagerSetup();
            Scene boot = EditorSceneManager.OpenScene(bootPath, OpenSceneMode.Single);
            var so = new SerializedObject(Object.FindAnyObjectByType<Bootstrapper>());
            SerializedProperty keepers = so.FindProperty("_shaderKeepers");
            keepers.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++)
            {
                keepers.GetArrayElementAtIndex(i).objectReferenceValue = AssetDatabase.LoadAssetAtPath<Material>(paths[i]);
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(boot);
            if (previous.Length > 0)
            {
                EditorSceneManager.RestoreSceneManagerSetup(previous);
            }

            Debug.Log($"Wrote {paths.Length} shader keepers onto the Boot scene.");
        }
    }
}
