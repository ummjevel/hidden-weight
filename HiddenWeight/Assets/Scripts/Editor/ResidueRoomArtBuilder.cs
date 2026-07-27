using System;
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
            string folder = GetFolderName(room.name);
            Transform artRoot = GetOrCreate(room.transform, "Art");

            BuildLayer(
                artRoot,
                "Far",
                $"{ArtRoot}/{folder}/{folder}_BG_Far.png",
                -30,
                0.15f);
            BuildLayer(
                artRoot,
                "Mid",
                $"{ArtRoot}/{folder}/{folder}_BG_Mid.png",
                -20,
                0.35f);
            BuildLayer(
                artRoot,
                "Foreground",
                $"{ArtRoot}/{folder}/{folder}_FG_Overlay.png",
                20,
                null);
        }

        static void BuildLayer(
            Transform parent,
            string name,
            string spritePath,
            int sortingOrder,
            float? parallaxMultiplier)
        {
            Transform layer = GetOrCreate(parent, name);
            var renderer =
                GetOrAddComponent<SpriteRenderer>(layer.gameObject);
            renderer.sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            renderer.sortingOrder = sortingOrder;

            if (renderer.sprite != null)
            {
                float roomWidth =
                    parent.parent.GetComponent<Room>()
                        .WorldBounds.size.x;
                float scale =
                    roomWidth / renderer.sprite.bounds.size.x;
                layer.localScale = Vector3.one * scale;
            }

            var parallax = layer.GetComponent<ParallaxLayer>();
            if (parallaxMultiplier.HasValue)
            {
                if (parallax == null)
                    parallax =
                        layer.gameObject.AddComponent<ParallaxLayer>();

                var serialized = new SerializedObject(parallax);
                serialized.FindProperty("multiplier").floatValue =
                    parallaxMultiplier.Value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
            else if (parallax != null)
            {
                UnityEngine.Object.DestroyImmediate(parallax);
            }
        }

        static string GetFolderName(string roomName)
        {
            if (TryReadSuffix(roomName, "Room", out int roomNumber))
                return $"Room{roomNumber:00}";
            if (TryReadSuffix(roomName, "Secret", out int secretNumber))
                return $"Secret{secretNumber:00}";

            throw new ArgumentException(
                $"Unsupported residue room name: {roomName}",
                nameof(roomName));
        }

        static bool TryReadSuffix(
            string value,
            string prefix,
            out int number)
        {
            number = 0;
            return value.StartsWith(prefix, StringComparison.Ordinal) &&
                int.TryParse(value.Substring(prefix.Length), out number);
        }

        static Transform GetOrCreate(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child != null)
                return child;

            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            return gameObject.transform;
        }

        static T GetOrAddComponent<T>(GameObject gameObject)
            where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null
                ? component
                : gameObject.AddComponent<T>();
        }
    }
}
