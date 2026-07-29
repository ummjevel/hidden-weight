using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HiddenWeight.UI
{
    // HUD 한구석에서 상태 변화를 알리는 작은 문양(ResidueStatusUI_v1: 되감기 / 위험 / 진행).
    //
    // 시트의 한 행이 하나의 사건을 처음부터 끝까지 담는다. 예를 들어 되감기 행은 8프레임에
    // 걸쳐 충전 → 준비 → 발동 → 소진을 한 번에 보여준다. 그래서 상태를 잘게 나누어 관리하지
    // 않고 "사건이 일어나면 그 행을 한 번 재생한다"로 다룬다.
    //
    // 위험 행만 루프다. 체력이 위태로운 동안에는 계속 눈에 남아야 하기 때문이다.
    [RequireComponent(typeof(Image))]
    public class StatusEmblem : MonoBehaviour
    {
        [System.Serializable]
        public class Sequence
        {
            public string name;
            public Sprite[] frames;
            public float fps = 10f;
            public bool loop;
        }

        [SerializeField] Sequence[] sequences;

        readonly Dictionary<string, Sequence> _byName = new Dictionary<string, Sequence>();
        Image _image;
        Sequence _current;
        string _currentName;
        float _timer;
        int _frame;

        public string CurrentSequence => _current != null ? _currentName : null;

        void Awake()
        {
            _image = GetComponent<Image>();
            _image.enabled = false;
            Rebuild();
        }

        // HUD가 런타임에 캔버스를 짓기 때문에, 프레임은 프리팹이 아니라 HUD가 들고 있다가
        // 여기로 넘겨 준다.
        public void Configure(params Sequence[] values)
        {
            sequences = values;
            Rebuild();
        }

        void Rebuild()
        {
            _byName.Clear();
            if (sequences == null) return;

            foreach (var sequence in sequences)
                if (sequence != null && !string.IsNullOrEmpty(sequence.name)
                    && sequence.frames != null && sequence.frames.Length > 0)
                    _byName[sequence.name] = sequence;
        }

        public bool Has(string name) => _byName.ContainsKey(name);

        public void RegisterAlias(string alias, string sequenceName)
        {
            if (string.IsNullOrEmpty(alias) || !_byName.TryGetValue(sequenceName, out var sequence)) return;
            _byName[alias] = sequence;
        }

        public void Play(string name)
        {
            if (!_byName.TryGetValue(name, out var sequence)) return;

            _current = sequence;
            _currentName = name;
            _frame = 0;
            _timer = 0f;
            if (_image == null) _image = GetComponent<Image>();
            _image.sprite = sequence.frames[0];
            _image.color = UISettings.ReduceFlash ? new Color(1f, 1f, 1f, 0.82f) : Color.white;
            _image.enabled = true;

            // 동작 감소에서는 애니메이션을 없애되 상태 자체는 숨기지 않는다.
            // 반복 상태는 대표 프레임에 머물고, 일회성 상태는 마지막 프레임을 짧게 보여 준다.
            if (UISettings.ReduceMotion)
            {
                _frame = sequence.loop ? 0 : sequence.frames.Length - 1;
                _image.sprite = sequence.frames[_frame];
            }
        }

        // 루프 중인 것을 끈다. 한 번짜리는 스스로 끝나므로 부를 필요가 없다.
        public void Stop(string name = null)
        {
            if (_current == null) return;
            if (name != null && _currentName != name && _current.name != name) return;

            _current = null;
            _currentName = null;
            if (_image != null) _image.enabled = false;
        }

        void Update()
        {
            if (_current == null) return;
            if (UISettings.ReduceMotion) return;

            _timer += Time.unscaledDeltaTime; // 일시정지 중에도 멈추지 않는다(HUD는 UI다)
            float interval = _current.fps <= 0f ? 0.1f : 1f / _current.fps;
            if (_timer < interval) return;

            _timer -= interval;
            _frame++;

            if (_frame >= _current.frames.Length)
            {
                if (!_current.loop)
                {
                    _current = null;
                    _currentName = null;
                    _image.enabled = false;
                    return;
                }
                _frame = 0;
            }

            _image.sprite = _current.frames[_frame];
        }
    }
}
