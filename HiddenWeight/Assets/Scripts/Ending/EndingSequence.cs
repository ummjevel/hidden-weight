using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using HiddenWeight.Core;
using HiddenWeight.Player;
using HiddenWeight.UI;

namespace HiddenWeight.Ending
{
    // Ending 씬의 감독. PlayerInput.Enabled = false로 이동을 막아두고 자각(L) 입력만 직접 읽는다.
    // AwarenessSystem은 이 씬에 두지 않는다 — 이동·볼륨·불안정 연출이 필요 없고, 이 시퀀스가
    // AnomalyObject들을 직접 제어하기 때문이다.
    //
    // 1단계: 거짓 깨어남. 페이드 인 → 정적 → 자각 홀드 누적(2.5초) → 암전 → 몽타주.
    // 2단계: 진짜 각성. 같은 침실이지만 모든 AnomalyObject가 반응하지 않는다.
    public class EndingSequence : MonoBehaviour
    {
        enum Phase { FadeIn, Stillness, FalseAwakeningInput, Blackout, Montage, TrueAwakening, FadeOut }

        [Header("대상")]
        [SerializeField] AnomalyObject[] anomalies;
        [SerializeField] Image montageImage; // 전체 화면 몽타주용. Ending 씬의 Canvas 하위에 배치된다.
        [SerializeField] Sprite[] montageFrames; // 잔재·응시·균열 각 1장, 총 3장
        [SerializeField] AudioClip endingBgm; // null이면 무음

        [Header("타이밍")]
        [SerializeField] float fadeInSeconds = 3f;
        [SerializeField] float stillnessSeconds = 4f;
        [SerializeField] float holdToAdvance = 2.5f;
        [SerializeField] float blackoutSeconds = 1.5f;
        [SerializeField] float montageFrameSeconds = 0.8f;
        [SerializeField] float trueAwakeningSeconds = 8f;
        [SerializeField] float fadeOutSeconds = 3f;

        Phase _phase;
        float _hold;

        void Awake()
        {
            if (montageImage != null) montageImage.gameObject.SetActive(false);
        }

        void Start()
        {
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Ending);

            PlayerInput.Enabled = false;
            StartCoroutine(RunSequence());
        }

        void OnDestroy()
        {
            // 시퀀스 도중 씬이 강제로 내려가는 등 예외 상황에서도 다음 씬의 입력이
            // 막힌 채로 남지 않도록 방어적으로 복구한다.
            PlayerInput.Enabled = true;
        }

        IEnumerator RunSequence()
        {
            _phase = Phase.FadeIn;
            if (ScreenFader.Instance != null)
            {
                ScreenFader.Instance.SetAlpha(1f);
                yield return ScreenFader.Instance.FadeTo(0f, fadeInSeconds);
            }

            _phase = Phase.Stillness;
            yield return new WaitForSeconds(stillnessSeconds);

            _hold = 0f;
            _phase = Phase.FalseAwakeningInput; // 이후 Update()가 자각 입력을 처리한다
        }

        void Update()
        {
            if (_phase != Phase.FalseAwakeningInput) return;

            if (PlayerInput.AwarenessHeld)
            {
                _hold += Time.deltaTime;
                SetAnomaliesRevealed(true);
                if (_hold >= holdToAdvance) StartCoroutine(TransitionToRealAwakening());
            }
            else
            {
                _hold = 0f;
                SetAnomaliesRevealed(false);
            }
        }

        IEnumerator TransitionToRealAwakening()
        {
            // 다음 프레임부터 Update()가 자각 홀드를 더 이상 처리하지 않도록 코루틴이
            // yield하기 전에 즉시 phase를 바꿔 중복 트리거를 막는다.
            _phase = Phase.Blackout;
            SetAnomaliesRevealed(false);

            if (ScreenFader.Instance != null) ScreenFader.Instance.SetAlpha(1f); // 딱 끊기는 암전
            yield return new WaitForSeconds(blackoutSeconds);

            _phase = Phase.Montage;
            yield return PlayMontage();

            yield return RunTrueAwakening();
        }

        IEnumerator PlayMontage()
        {
            if (montageImage != null)
            {
                montageImage.gameObject.SetActive(true);

                for (int i = 0; i < montageFrames.Length; i++)
                {
                    montageImage.sprite = montageFrames[i];
                    yield return new WaitForSeconds(montageFrameSeconds); // 페이드 없이 하드 컷
                }

                montageImage.gameObject.SetActive(false);
            }

            // 몽타주가 끝나면 곧장(페이드 없이) 같은 침실로 돌아온다.
            if (ScreenFader.Instance != null) ScreenFader.Instance.SetAlpha(0f);
        }

        IEnumerator RunTrueAwakening()
        {
            _phase = Phase.TrueAwakening;

            // 2단계: 모든 이상이 자각에 반응하지 않는다.
            foreach (var anomaly in anomalies)
            {
                if (anomaly != null) anomaly.Enabled = false;
            }

            if (AudioManager.Instance != null) AudioManager.Instance.PlayBgm(endingBgm, 2f);

            float t = 0f;
            while (t < trueAwakeningSeconds && PlayerInput.AwarenessHeld)
            {
                t += Time.deltaTime;
                yield return null;
            }

            _phase = Phase.FadeOut;
            if (ScreenFader.Instance != null) yield return ScreenFader.Instance.FadeTo(1f, fadeOutSeconds);

            PlayerInput.Enabled = true;
            CinematicVideoPlayer.Play("final_end_scene.mp4", ReturnToTitle);
        }

        void ReturnToTitle()
        {
            if (GameManager.Instance != null) GameManager.Instance.SetState(GameState.Title);
            SceneFlow.LoadWithFade(SceneFlow.Title);
        }

        void SetAnomaliesRevealed(bool revealed)
        {
            foreach (var anomaly in anomalies)
            {
                if (anomaly != null) anomaly.OnAwarenessChanged(revealed);
            }
        }
    }
}
