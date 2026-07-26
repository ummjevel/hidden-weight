using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using HiddenWeight.Core;
using HiddenWeight.Player;
using HiddenWeight.World;

namespace HiddenWeight.Emotions
{
    // 자각(L 홀드) 시스템. 채도를 잃은 볼륨을 띄우고, 이동속도를 늦추고,
    // AwarenessRegistry에 등록된 반응 오브젝트들에 On/Off를 방송한다.
    // 균열 지역(awarenessStable == false)에서는 켜져 있는 동안 주기적으로 깜빡인다(기획서 5.3절).
    public class AwarenessSystem : MonoBehaviour
    {
        // 기획서 5.3절 리터럴: 자각 중 이동 가능하되 속도 0.6배.
        // EmotionData에는 없는 값이라 여기 직렬화 필드로 둔다.
        [SerializeField] float slowMultiplier = 0.6f;
        [SerializeField] float volumeRampTime = 0.25f;

        public static AwarenessSystem Instance { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsStable => GameManager.Instance.CurrentZoneData?.awarenessStable ?? true;

        public event System.Action<bool> AwarenessChanged;

        Volume _volume;
        Coroutine _weightRoutine;
        Coroutine _flickerRoutine;

        void Awake()
        {
            Instance = this;

            _volume = gameObject.AddComponent<Volume>();
            _volume.profile = GameManager.Instance.Balance.awarenessProfile;
            _volume.weight = 0f;
            _volume.priority = 10f;
        }

        void OnEnable() => AwarenessRegistry.Added += HandleAdded;
        void OnDisable() => AwarenessRegistry.Added -= HandleAdded;

        // 자각이 켜진 상태에서 새로 등록된 오브젝트는 즉시 동기화한다
        // (씬 로드 순서상 HiddenFragment.OnEnable이 이 시점 이후에 돌 수 있다).
        void HandleAdded(IAwarenessReactive r)
        {
            if (IsActive) r.OnAwarenessChanged(true);
        }

        void Update()
        {
            // AwarenessHeld는 PausePressed와 마찬가지로 PlayerInput.Enabled와 무관하게 동작하므로
            // (Ending 시퀀스가 필요로 함), 일시정지 중에는 여기서 직접 막아 이전과 같은 동작을 유지한다.
            if (GameManager.Instance.State != GameState.Playing)
            {
                if (IsActive) Deactivate();
                IsActive = false;
                return;
            }

            bool wanted = GameManager.Instance.Progress.HasAwareness && PlayerInput.AwarenessHeld;
            if (wanted != IsActive)
            {
                IsActive = wanted;
                if (IsActive) Activate();
                else Deactivate();
                return;
            }

            // 자각을 유지한 채로 안정 지역 -> 균열 지역으로 넘어간 경우.
            // 반대 방향(균열 -> 안정)은 UnstableFlicker의 while 조건이 스스로 끝내준다.
            if (IsActive && !IsStable && _flickerRoutine == null)
            {
                _flickerRoutine = StartCoroutine(UnstableFlicker());
            }
        }

        void Activate()
        {
            PlayerController.Instance.ExternalSpeedMultiplier = slowMultiplier;
            StartWeightRamp(1f);
            Broadcast(true);

            if (!IsStable) _flickerRoutine = StartCoroutine(UnstableFlicker());
        }

        void Deactivate()
        {
            PlayerController.Instance.ExternalSpeedMultiplier = 1f;
            StartWeightRamp(0f);
            Broadcast(false);

            if (_flickerRoutine != null)
            {
                StopCoroutine(_flickerRoutine);
                _flickerRoutine = null;
            }
        }

        void Broadcast(bool active)
        {
            foreach (var r in AwarenessRegistry.Items) r?.OnAwarenessChanged(active);
            AwarenessChanged?.Invoke(active);
        }

        void StartWeightRamp(float target)
        {
            if (_weightRoutine != null) StopCoroutine(_weightRoutine);
            _weightRoutine = StartCoroutine(RampWeight(target));
        }

        IEnumerator RampWeight(float target)
        {
            float start = _volume.weight;
            float t = 0f;
            while (t < volumeRampTime)
            {
                t += Time.deltaTime;
                _volume.weight = Mathf.Lerp(start, target, t / volumeRampTime);
                yield return null;
            }
            _volume.weight = target;
        }

        IEnumerator UnstableFlicker()
        {
            while (IsActive && !IsStable)
            {
                yield return new WaitForSeconds(Random.Range(0.3f, 0.8f));

                var items = AwarenessRegistry.Items;
                if (items.Count == 0) continue;

                int n = Mathf.Max(1, items.Count / 2);
                for (int i = 0; i < n; i++)
                {
                    var pick = items[Random.Range(0, items.Count)];
                    pick?.OnAwarenessChanged(false);
                }

                yield return new WaitForSeconds(0.15f);
                foreach (var r in items) r?.OnAwarenessChanged(true);
            }

            _flickerRoutine = null;
        }
    }
}
