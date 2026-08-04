using UnityEngine;
using HiddenWeight.Player;

namespace HiddenWeight.World
{
    // 방에 처음 들어왔을 때 나아갈 방향으로 한 번 흐르는 빛.
    //
    // 방 문에는 이제 아치와 유도광이 있지만, 넓은 방에서는 그 문이 화면 밖이라 "지금 어느
    // 쪽으로 가야 하는가"가 여전히 보이지 않는다. 방에 들어선 순간 1.2초 동안 출구 쪽으로
    // 빛이 한 번 흐르면, 화면 밖의 목적지를 화면 안에서 가리킬 수 있다.
    //
    // HUD 화살표를 쓰지 않는 것은 의도적이다(설계 원칙 2 "세계가 먼저, UI가 보조한다").
    // 한 번만 보여주는 것도 의도적이다 — 반복되면 안내가 아니라 잔소리가 된다.
    [RequireComponent(typeof(Room))]
    public sealed class RoomEntryCue : MonoBehaviour
    {
        const float CueSeconds = 1.2f;
        const int Motes = 5;

        Room _room;
        bool _shown;
        float _timer;
        readonly Transform[] _motes = new Transform[Motes];
        Vector3 _from;
        Vector3 _to;
        static Sprite _dot;

        void Awake() => _room = GetComponent<Room>();

        void Update()
        {
            if (_timer > 0f)
            {
                Animate();
                return;
            }
            if (_shown) return;

            var player = PlayerController.Instance;
            if (player == null || !_room.Contains(player.transform.position)) return;

            Transform exit = FarthestDoor(player.transform.position);
            if (exit == null) { _shown = true; return; }

            _shown = true;
            _from = player.transform.position + Vector3.up * 1.2f;
            _to = exit.position + Vector3.up * 0.6f;
            _timer = CueSeconds;
            Spawn();
        }

        // 들어온 문이 아니라 나갈 문을 고른다. 지금 서 있는 자리에서 가장 먼 문이 그것이다.
        Transform FarthestDoor(Vector3 from)
        {
            Transform best = null;
            float bestDistance = 0f;
            foreach (var door in FindObjectsByType<RoomDoor>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                if (!_room.Contains(door.transform.position)) continue;
                float distance = Vector3.Distance(from, door.transform.position);
                if (distance <= bestDistance) continue;
                bestDistance = distance;
                best = door.transform;
            }
            return best;
        }

        void Spawn()
        {
            for (int i = 0; i < Motes; i++)
            {
                var go = new GameObject("EntryMote");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = Dot();
                sr.color = new Color(0.78f, 0.94f, 1f, 0f);
                sr.sortingOrder = 30;
                go.transform.localScale = Vector3.one * 0.22f;
                _motes[i] = go.transform;
            }
        }

        void Animate()
        {
            _timer -= Time.deltaTime;
            float t = 1f - Mathf.Clamp01(_timer / CueSeconds);

            for (int i = 0; i < Motes; i++)
            {
                var mote = _motes[i];
                if (mote == null) continue;

                // 알갱이마다 조금씩 뒤처져 출발해 "흐름"으로 보이게 한다.
                float local = Mathf.Clamp01((t - i * 0.08f) / 0.72f);
                mote.position = Vector3.Lerp(_from, _to, local);

                var sr = mote.GetComponent<SpriteRenderer>();
                // 나타났다 사라진다. 도착점에서 그냥 꺼지면 무엇을 가리켰는지 남지 않는다.
                float alpha = Mathf.Sin(local * Mathf.PI) * 0.8f;
                sr.color = new Color(sr.color.r, sr.color.g, sr.color.b,
                                     local <= 0f ? 0f : alpha);
            }

            if (_timer > 0f) return;
            foreach (var mote in _motes) if (mote != null) Destroy(mote.gameObject);
        }

        static Sprite Dot()
        {
            if (_dot != null) return _dot;

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f) / size * 2f - 1f;
                    float dy = (y + 0.5f) / size * 2f - 1f;
                    float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
                }
            texture.Apply();
            _dot = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), size);
            _dot.name = "EntryCueDot";
            return _dot;
        }
    }
}
