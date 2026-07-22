using System;
using System.Collections;
using UnityEngine;
using HanGame.Common;
using HanGame.Data;

namespace HanGame.Day
{
    /// <summary>
    /// 상사의 눈치. 전투 캐릭터가 아니라 맵 기믹. 기획서 10장.
    /// 30초 경고 → 붉은 시선 영역이 화면을 가로질러 이동 → 걸리면 2초간 '일하는 척'(자동 공격 정지).
    /// </summary>
    public class BossGaze : MonoBehaviour
    {
        [SerializeField] private Transform gazeVisual;   // 붉은 시선 영역 스프라이트
        [SerializeField] private float pretendDuration = 2f;

        public bool PlayerCaught { get; private set; } // AutoAttackSystem이 읽어 공격 정지
        public event Action WarningRaised; // "상사가 보고 있습니다" UI

        private FloorConfig _floor;
        private float _width;
        private bool _scheduled;

        public void Configure(FloorConfig floor)
        {
            _floor = floor;
            _width = floor != null ? floor.bossGazeWidth : 2f;
            _scheduled = false;
            if (gazeVisual != null) gazeVisual.gameObject.SetActive(false);
        }

        /// <summary>웨이브 경과 시간을 매니저가 매 프레임 전달.</summary>
        public void Tick(float waveElapsed)
        {
            if (_floor == null || !_floor.bossGazeEnabled || _scheduled) return;
            if (waveElapsed >= _floor.bossGazeFirstAt)
            {
                _scheduled = true;
                StartCoroutine(RunSweeps());
            }
        }

        private IEnumerator RunSweeps()
        {
            for (int s = 0; s < _floor.bossGazeSweeps; s++)
            {
                WarningRaised?.Invoke();
                if (AudioManager.Instance != null) AudioManager.Instance.PlaySfx(Sfx.BossGazeWarn);
                yield return new WaitForSeconds(2f); // 2초 후 시선 이동

                yield return Sweep();
                yield return new WaitForSeconds(3f); // 다음 시선까지 간격
            }
        }

        private IEnumerator Sweep()
        {
            if (gazeVisual == null) yield break;
            gazeVisual.gameObject.SetActive(true);

            float half = 12f; // 맵 반폭. 필요 시 노출
            float x = -half;
            float speed = _floor.bossGazeSpeed;
            gazeVisual.position = new Vector3(x, 0f, gazeVisual.position.z);

            while (x < half)
            {
                x += speed * Time.deltaTime;
                gazeVisual.position = new Vector3(x, gazeVisual.position.y, gazeVisual.position.z);

                if (Player.Local != null && Mathf.Abs(Player.Local.Position.x - x) <= _width * 0.5f)
                    StartCoroutine(CatchPlayer());

                yield return null;
            }
            gazeVisual.gameObject.SetActive(false);
        }

        private IEnumerator CatchPlayer()
        {
            if (PlayerCaught) yield break;
            PlayerCaught = true; // 자동 공격 정지. 이동은 가능(기획서 10.2)
            yield return new WaitForSeconds(pretendDuration);
            PlayerCaught = false;
        }
    }
}
