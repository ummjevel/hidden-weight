using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace HiddenWeight.Tests
{
    public class RoomBackground4KTests
    {
        static IEnumerable<string> Paths()
        {
            for (int i = 1; i <= 12; i++)
            {
                yield return $"Assets/Art/Residue/Rooms4K/Room{i:00}.png";
                yield return $"Assets/Art/Gaze/Rooms4K/GazeRoom{i:00}.png";
                yield return $"Assets/Art/Fracture/Rooms4K/FractureRoom{i:00}.png";
            }

            for (int i = 1; i <= 3; i++)
            {
                yield return $"Assets/Art/Residue/Rooms4K/Secret{i:00}.png";
                yield return $"Assets/Art/Gaze/Rooms4K/GazeSecret{i:00}.png";
                yield return $"Assets/Art/Fracture/Rooms4K/FractureSecret{i:00}.png";
            }
        }

        [TestCaseSource(nameof(Paths))]
        public void EveryRoomHasAnExact4KBackground(string path)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Assert.That(texture, Is.Not.Null, path);
            Assert.That(texture.width, Is.EqualTo(3840), path);
            Assert.That(texture.height, Is.EqualTo(2160), path);
        }
    }
}
