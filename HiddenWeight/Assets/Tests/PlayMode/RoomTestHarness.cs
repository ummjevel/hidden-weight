using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using HiddenWeight.World;

namespace HiddenWeight.Tests
{
    // 셸을 띄우고 지정한 방을 로드한다. RoomLoader.LoadRoom과 이름이 겹치지 않게 둔다.
    public static class RoomTestHarness
    {
        public static IEnumerator EnterRoom(string zone, string room)
        {
            yield return SceneManager.LoadSceneAsync("Zone_" + zone, LoadSceneMode.Single);
            yield return null;

            var loader = RoomLoader.Instance;
            Assert(loader != null, "셸에 RoomLoader가 없다.");

            // 진입점이 이미 첫 방을 로드했을 수 있으므로 끝날 때까지 기다린다.
            while (loader.IsTransitioning) yield return null;

            if (loader.CurrentRoom != room)
            {
                yield return loader.LoadRoom(room, null);
                while (loader.IsTransitioning) yield return null;
            }

            yield return null;
        }

        static void Assert(bool condition, string message)
        {
            if (!condition) throw new System.Exception(message);
        }
    }
}
