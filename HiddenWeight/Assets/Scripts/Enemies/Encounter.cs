using System.Collections;
using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.World;
using System.Collections.Generic;

namespace HiddenWeight.Enemies
{
    // 조우 관리자(RESIDUE_ROOM_IMPLEMENTATION.md 2.2절). 방에 들어오면 적 묶음을 단계별로
    // 활성화하고, 전투 중에는 출구를 잠그고, 전멸시키면 잠금을 풀고 보상·숏컷을 연다.
    //
    // 왜 적을 미리 놓고 비활성으로 두는가: 명세가 "전투 시작 전 플레이어가 전체 전장을
    // 내려다볼 수 있게"(S2), "새 적을 처음 보여주는 위치와 실제 조우 지점 사이에 2초 이상
    // 관찰 시간"(1.4절)을 요구한다. 스폰이 아니라 활성화로 하면 배치가 그대로 보인다.
    public class Encounter : MonoBehaviour
    {
        [System.Serializable]
        public class Wave
        {
            public GameObject[] members;

            [Tooltip("이전 단계가 시작되고 이 시간이 지나면 활성화한다. 0이면 시간 조건 없음.")]
            public float delaySeconds;

            [Tooltip("이전 단계의 생존 수가 이 값 이하가 되면 활성화한다. 음수면 수 조건 없음.")]
            public int advanceWhenRemaining = -1;
        }

        [SerializeField] string encounterId;
        [SerializeField] bool oneTime;              // 정예·중간 보스·지역 보스
        [SerializeField] float observeSeconds = 2f; // 입장 후 잠그기까지 관찰 시간
        [SerializeField] Wave[] waves;
        [SerializeField] GameObject[] lockObjects;  // 전투 중에만 켜지는 잠금 콜라이더
        [SerializeField] RewardChest victoryReward;
        [SerializeField] Shortcut victoryShortcut;
        [SerializeField] GameObject[] victoryObjects;
        [SerializeField] string displayName = "보스";

        int _activeWave = -1;
        float _waveStartTime;
        bool _running;
        bool _finished;
        readonly List<GameObject> _runtimeVictoryObjects = new List<GameObject>();

        public bool IsRunning => _running;
        public bool IsFinished => _finished;
        public string Id => encounterId;
        public string DisplayName => string.IsNullOrEmpty(displayName) ? "보스" : displayName;
        public Enemy BossEnemy { get; private set; }

        // 보스뿐 아니라 모든 조우가 알린다. HUD는 BossEnemy가 있는 조우만 골라 체력 바를
        // 띄우고, 카메라는 일반 전투에서도 전장 중심으로 구도를 잡아야 하기 때문이다.
        public static event System.Action<Encounter, bool> EncounterStateChanged;

        public void RegisterVictoryObject(GameObject target)
        {
            if (target == null || _runtimeVictoryObjects.Contains(target)) return;
            _runtimeVictoryObjects.Add(target);
            target.SetActive(_finished);
        }

        void Start()
        {
            SetLocks(false);
            if (victoryObjects != null)
                foreach (var target in victoryObjects)
                    if (target != null) target.SetActive(false);

            // 이 조우가 관리하는 적은 죽어도 파괴되지 않게 표시한다.
            if (waves != null)
                foreach (var wave in waves)
                    foreach (var member in wave.members)
                    {
                        if (member == null) continue;
                        var enemy = member.GetComponent<Enemy>();
                        if (enemy != null)
                        {
                            enemy.SetManagedByEncounter(true);
                            if (enemy.GetComponent<BossController>() != null) BossEnemy = enemy;
                        }
                    }

            // 플레이어가 죽어 체크포인트로 돌아가면 이 조우도 처음 상태로 되돌린다.
            if (GameManager.Instance != null) GameManager.Instance.RespawnRequested += HandlePlayerRespawn;

            // 이미 클리어한 일회성 조우는 적을 아예 두지 않는다. 숏컷은 열린 채로 시작한다
            // (Shortcut 자신도 저장 상태를 보지만, 보상 상자와 순서를 맞추기 위해 여기서도 연다).
            if (oneTime && GameManager.Instance != null
                && GameManager.Instance.Progress.IsEncounterCleared(encounterId))
            {
                DeactivateAll();
                _finished = true;
                if (victoryShortcut != null) victoryShortcut.Open();
                if (victoryObjects != null)
                    foreach (var target in victoryObjects)
                        if (target != null) target.SetActive(true);
                return;
            }

            DeactivateAll();
        }

        void OnDestroy()
        {
            if (_running) EncounterStateChanged?.Invoke(this, false);
            if (GameManager.Instance != null) GameManager.Instance.RespawnRequested -= HandlePlayerRespawn;
        }

        void HandlePlayerRespawn(Vector3 _)
        {
            if (_finished) return; // 이미 클리어한 일회성 조우는 되돌리지 않는다
            ResetEncounter();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_running || _finished) return;
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;

            StartCoroutine(BeginRoutine());
        }

        void OnTriggerExit2D(Collider2D other)
        {
            // 명세(S2): "방에서 나가면 조우는 초기화". 클리어한 조우는 되돌리지 않는다.
            if (_finished || !_running) return;
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;

            ResetEncounter();
        }

        IEnumerator BeginRoutine()
        {
            _running = true;

            // 전장을 먼저 보여준다. 이 사이에 나가면 잠기지 않는다.
            yield return new WaitForSeconds(observeSeconds);
            if (!_running) yield break;

            SetLocks(true);
            ActivateWave(0);
            EncounterStateChanged?.Invoke(this, true);
        }

        void Update()
        {
            if (!_running || _finished || _activeWave < 0) return;

            int remaining = AliveInWave(_activeWave);

            // 다음 단계 조건: 시간 경과 또는 잔존 수 이하.
            if (_activeWave + 1 < waves.Length)
            {
                var next = waves[_activeWave + 1];
                bool byTime = next.delaySeconds > 0f && Time.time - _waveStartTime >= next.delaySeconds;
                bool byCount = next.advanceWhenRemaining >= 0 && remaining <= next.advanceWhenRemaining;
                if (byTime || byCount)
                {
                    ActivateWave(_activeWave + 1);
                    return;
                }
            }

            if (AliveTotal() == 0) Victory();
        }

        void ActivateWave(int index)
        {
            _activeWave = index;
            _waveStartTime = Time.time;

            foreach (var member in waves[index].members)
                if (member != null) member.SetActive(true);
        }

        void Victory()
        {
            _finished = true;
            _running = false;
            EncounterStateChanged?.Invoke(this, false);
            if (BossEnemy != null) AudioManager.Instance?.PlaySfx(SfxCue.BossVictory, 0.8f);
            SetLocks(false);

            if (oneTime && GameManager.Instance != null)
                GameManager.Instance.Progress.MarkEncounterCleared(encounterId);

            if (victoryReward != null) victoryReward.Give();
            if (victoryShortcut != null) victoryShortcut.Open();
            if (victoryObjects != null)
                foreach (var target in victoryObjects)
                    if (target != null) target.SetActive(true);
            foreach (var target in _runtimeVictoryObjects)
                if (target != null) target.SetActive(true);
        }

        void ResetEncounter()
        {
            bool wasRunning = _running;
            StopAllCoroutines();
            _running = false;
            _activeWave = -1;
            SetLocks(false);
            DeactivateAll();
            if (wasRunning) EncounterStateChanged?.Invoke(this, false);
        }

        // 조우를 처음 상태로 되돌린다. 쓰러진 적도 체력을 채워 다시 세운다 — Enemy가
        // 조우 소속임을 알고 파괴 대신 비활성화되기 때문에 가능하다.
        void DeactivateAll()
        {
            if (waves == null) return;
            foreach (var wave in waves)
                foreach (var member in wave.members)
                {
                    if (member == null) continue;

                    var enemy = member.GetComponent<Enemy>();
                    if (enemy != null) enemy.ResetForEncounter();
                    member.SetActive(false);
                }
        }

        void SetLocks(bool locked)
        {
            if (lockObjects == null) return;
            foreach (var lockObject in lockObjects)
                if (lockObject != null) lockObject.SetActive(locked);
        }

        int AliveInWave(int index)
        {
            int count = 0;
            foreach (var member in waves[index].members)
            {
                if (member == null || !member.activeSelf) continue;

                // 사망 애니메이션이 재생되는 동안 오브젝트는 잠시 활성 상태로 남는다.
                // 조우 완료 판정은 그림이 사라지는 시점이 아니라 체력이 0이 된 시점에 한다.
                var enemy = member.GetComponent<Enemy>();
                if (enemy == null || enemy.IsAlive) count++;
            }
            return count;
        }

        int AliveTotal()
        {
            int count = 0;
            for (int i = 0; i <= _activeWave; i++) count += AliveInWave(i);
            return count;
        }
    }
}
