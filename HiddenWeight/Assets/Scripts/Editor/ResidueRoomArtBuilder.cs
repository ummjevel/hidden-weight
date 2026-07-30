using HiddenWeight.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HiddenWeight.EditorTools
{
    public static class ResidueRoomArtBuilder
    {
        const string ArtRoot = "Assets/Art/Residue";
        const string ScenePath = "Assets/Scenes/Zone_Residue.unity";

        [MenuItem("Hidden Weight/Art/Build Residue Room Layers")]
        public static void BuildSceneLayers()
        {
            ResidueArtImporter.ConfigureAll();

            Scene scene = EditorSceneManager.OpenScene(
                ScenePath,
                OpenSceneMode.Single);
            foreach (Room room in
                UnityEngine.Object.FindObjectsByType<Room>(
                    FindObjectsSortMode.None))
            {
                BuildRoomArt(room);
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        public static void BuildRoomArt(Room room)
        {
            SingleRoomBackgroundBuilder.Build(room, ArtRoot);
        }
    }
}
