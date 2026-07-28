using System.Collections;
using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.World;

namespace HiddenWeight.Enemies
{
    // 지원·원거리형 — 응시의 "밀고하는 입"(GAZE_LEVEL_DESIGN.md 6.1절). 직접 때리지 않고
    // 비명을 예고한 뒤 주변 시선 장치를 켠다. 플레이어를 찾지 못하면 비명을 취소한다.
    //
    // 이 적을 두는 자리(G06·G09)는 "엄폐물 뒤에서 먼저 처치할지, 숨죽여 지나갈지 선택"하게
    // 하는 곳이다(4.6절). 그래서 숨죽인 플레이어는 아예 감지하지 못하고, 예고 도중에
    // 숨어도 비명이 취소된다 — 예고를 보고 나서 대응할 시간이 실제로 있어야 한다.
    public class ScreamerBehavior : EnemyBehavior
    {
        [SerializeField] GazeHazard[] linkedDevices; // 비명이 켜는 휴면 시선들
        [SerializeField] float cooldownSeconds = 4f;

        bool _busy;
        float _cooldown;

        public bool IsScreaming { get; private set; }

        protected override void Awake()
        {
            base.Awake();

            // 자기 자리를 지키는 적이다. 순찰이 붙어 있으면 끈다.
            var patrol = GetComponent<EnemyPatrol>();
            if (patrol != null) patrol.enabled = false;
        }

        void Update()
        {
            if (_cooldown > 0f) _cooldown -= Time.deltaTime;
            if (_busy || _cooldown > 0f) return;
            if (!CanSeePlayer()) return;

            StartCoroutine(ScreamRoutine());
        }

        // 숨죽인 플레이어는 찾지 못한다. 시선 장치와 같은 규칙이라 플레이어가 배운 것이
        // 그대로 통한다.
        bool CanSeePlayer()
        {
            if (Player == null) return false;
            if (PlayerLayers.IsHushed(Player.gameObject)) return false;
            return DistanceToPlayer <= Data.detectRange;
        }

        IEnumerator ScreamRoutine()
        {
            _busy = true;
            IsScreaming = true;
            FaceTowards(DirectionToPlayer);

            // 예고. 이 사이에 숨으면 취소된다.
            ShowTelegraph(true);
            float elapsed = 0f;
            while (elapsed < Data.telegraphSeconds)
            {
                if (!CanSeePlayer())
                {
                    ShowTelegraph(false);
                    IsScreaming = false;
                    _busy = false;
                    _cooldown = cooldownSeconds * 0.5f; // 취소는 벌이 가볍다
                    yield break;
                }
                elapsed += Time.deltaTime;
                yield return null;
            }
            ShowTelegraph(false);

            if (linkedDevices != null)
                foreach (var device in linkedDevices)
                    if (device != null) device.Activate(Data.alertSeconds);

            Self.PlayClip("Attack");
            IsScreaming = false;

            yield return new WaitForSeconds(Data.recoverSeconds);
            _cooldown = cooldownSeconds;
            _busy = false;
        }
    }
}
