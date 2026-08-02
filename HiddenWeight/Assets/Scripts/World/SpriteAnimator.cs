using UnityEngine;

namespace HiddenWeight.World
{
    // 스프라이트 시트 프레임을 순서대로 넘기는 최소 애니메이터.
    //
    // Unity Animator/AnimationClip을 쓰지 않는 이유: 클립·컨트롤러 에셋을 상태 수만큼 만들어야
    // 하는데, 이 프로젝트는 씬과 프리팹을 전부 코드로 짓는다. 에디터에서 손으로 엮은 에셋이
    // 하나라도 끼면 빌더를 다시 돌릴 때마다 그 연결이 끊긴다. 프레임 배열만 들고 도는 편이
    // 빌더와 궁합이 맞고, 문서가 요구하는 것(행 단위 프레임, 행별 FPS, 루프 여부)에도 충분하다.
    public class SpriteAnimator : MonoBehaviour
    {
        [System.Serializable]
        public class Clip
        {
            public string name;
            public Sprite[] frames;
            public float fps = 10f;
            public bool loop = true;
        }

        [SerializeField] SpriteRenderer target;
        [SerializeField] Clip[] clips;

        // 프레임마다 원본 셀 크기가 다르다(플레이어: 정지 포즈 384x512, 이동 256x256,
        // 공격·벽 362x362). 스케일을 한 번만 정해 두면 동작에 따라 캐릭터 크기가 널뛴다.
        // 0보다 크면 매 프레임 이 높이에 맞춰 스케일을 다시 잡아 크기를 일정하게 유지한다.
        [SerializeField] float normalizedHeight = 0f;

        // 프레임별 재정규화의 함정: 웅크린 대시 프레임(트리밍 높이가 작다)은 뻥튀기되고,
        // 잔상·이펙트가 섞여 트리밍 박스가 커진 프레임은 몸이 줄어든다 — "공격·대시 때
        // 캐릭터가 작아진다"가 이것이다. 켜면 첫 클립의 첫 프레임(대기 포즈)을 기준으로
        // 배율을 한 번만 정하고 모든 프레임에 같은 배율을 쓴다. 같은 시트 계열(같은 PPU,
        // 같은 캐릭터 픽셀 크기)이라는 전제가 있는 플레이어 전용 옵션이다.
        [SerializeField] bool uniformScale = false;
        float _uniformScaleFactor = -1f;

        // 프레임마다 캔버스 안 여백(머리 위·발 아래 공백)이 달라, 피벗(캔버스 중앙)만 믿으면
        // 발 위치가 프레임/클립마다 들쭉날쭉해진다(플레이어 "공중에 뜬 것처럼 보인다" 버그).
        // 켜두면 매 프레임 스프라이트의 실제(트리밍된) 바닥이 항상 groundY(로컬 좌표)에 오도록
        // 위치를 다시 계산한다 — 피벗 대신 "그림 속 발"을 기준으로 맞춘다.
        [SerializeField] bool lockFeetToGround = false;
        [SerializeField] float groundY = 0f;

        // "지금 실제로 화면에 그려지는 렌더러"는 이것 하나다. 좌우 반전·피격 점멸처럼
        // 겉모습을 건드리는 쪽은 전부 여기를 봐야 한다 — 예전에는 프리팹 루트에 남아 있던
        // 꺼진 렌더러를 잡아, 안 보이는 그림만 뒤집히고 점멸이 엉뚱한 그림을 켜 놓았다.
        public SpriteRenderer Renderer => target;

        Clip _current;
        float _elapsed;
        int _frame;

        // 재생 속도 배율. 걷는 속도와 무관하게 고정 fps로 돌리면 발이 지면 위를 미끄러진다.
        // 이동을 소유한 쪽(적 행동 모듈)이 실제 수평 속도에 맞춰 매 프레임 넣어 준다.
        // 1이면 클립에 적힌 fps 그대로라 이 값을 건드리지 않는 쪽은 동작이 변하지 않는다.
        public float PlaybackSpeed { get; set; } = 1f;

        public string CurrentClip => _current != null ? _current.name : null;
        public bool IsFinished => _current != null && !_current.loop && _frame >= _current.frames.Length - 1;
        public event System.Action<string, int> FrameDisplayed;

        void Awake()
        {
            if (target == null) target = GetComponentInChildren<SpriteRenderer>();
        }

        // 상태 전환용 시트(발판의 균열·붕괴·복구처럼)는 시작하자마자 첫 클립을 틀면 안 된다.
        // 밟지도 않았는데 발판이 계속 금 가 있는 것처럼 보인다. 그런 경우만 이 값을 끈다.
        [SerializeField] bool autoPlay = true;

        void Start()
        {
            if (autoPlay && _current == null && clips != null && clips.Length > 0) Play(clips[0].name);
        }

        void Update()
        {
            if (_current == null || _current.frames == null || _current.frames.Length == 0) return;

            _elapsed += Time.deltaTime * Mathf.Max(0f, PlaybackSpeed);
            float frameTime = _current.fps <= 0f ? 0.1f : 1f / _current.fps;
            if (_elapsed < frameTime) return;

            _elapsed -= frameTime;

            if (_frame + 1 >= _current.frames.Length)
            {
                // 루프가 아니면 마지막 프레임에서 멈춘다 — 복원·붕괴처럼 "끝난 상태"를 남겨야
                // 하는 연출이 있기 때문이다(ANIMATION_README.md).
                if (!_current.loop) return;
                _frame = 0;
            }
            else
            {
                _frame++;
            }

            Apply();
        }

        // 같은 클립을 다시 넣으면 이어서 재생한다(걷다가 방향만 바뀌었을 때 튀지 않게).
        public void Play(string clipName, bool restart = false)
        {
            if (clips == null) return;
            if (!restart && _current != null && _current.name == clipName) return;

            foreach (var clip in clips)
            {
                if (clip.name != clipName) continue;

                _current = clip;
                _frame = 0;
                _elapsed = 0f;
                Apply();
                return;
            }
        }

        public bool Has(string clipName)
        {
            if (clips == null) return false;
            foreach (var clip in clips)
                if (clip.name == clipName) return true;
            return false;
        }

        void Apply()
        {
            if (target == null || _current.frames.Length == 0) return;

            var sprite = _current.frames[Mathf.Clamp(_frame, 0, _current.frames.Length - 1)];
            target.sprite = sprite;
            FrameDisplayed?.Invoke(_current.name, _frame);

            if (normalizedHeight <= 0f || sprite == null) return;

            float height = sprite.bounds.size.y;
            if (height <= 0f) return;

            // 가로세로 비율은 유지한 채 높이만 맞춘다.
            float scale = uniformScale ? UniformScaleFactor() : normalizedHeight / height;
            if (scale <= 0f) return;
            target.transform.localScale = new Vector3(scale, scale, 1f);

            if (!lockFeetToGround) return;

            // sprite.bounds.min.y는 피벗 기준 트리밍된 하단(발) 위치다(스케일 전 단위).
            // 스케일을 곱해 실제 렌더 크기로 바꾼 뒤, 그만큼 위/아래로 밀어 발을 groundY에 둔다.
            var pos = target.transform.localPosition;
            pos.y = groundY - sprite.bounds.min.y * scale;
            target.transform.localPosition = pos;
        }

        // 기준 배율은 첫 클립의 첫 프레임(플레이어는 PlayerIdle_00 = 서 있는 포즈)에서
        // 한 번만 계산해 캐시한다.
        float UniformScaleFactor()
        {
            if (_uniformScaleFactor > 0f) return _uniformScaleFactor;
            if (clips == null || clips.Length == 0) return -1f;

            var reference = clips[0].frames != null && clips[0].frames.Length > 0
                ? clips[0].frames[0] : null;
            if (reference == null) return -1f;

            float height = reference.bounds.size.y;
            if (height <= 0f) return -1f;

            _uniformScaleFactor = normalizedHeight / height;
            return _uniformScaleFactor;
        }
    }
}
