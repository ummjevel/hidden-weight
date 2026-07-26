using System.Collections;
using UnityEngine;
using HiddenWeight.Core;
using HiddenWeight.Data;
using HiddenWeight.Player;

namespace HiddenWeight.World
{
    // 자각 해금 지점 (기획서 EMOTION_SYSTEM 2.4절 — 응시 지역 후반부, 가장 큰 "눈" 오브제 앞).
    // 대사 없이: 입력을 잠그고, 거대 눈이 플레이어를 향해 커졌다가 가라앉는 연출 뒤 자각을 부여한다.
    // "숨어 있던 캐릭터가 처음으로 정면을 응시한다"는 컷씬은 정면 스프라이트가 없는 MVP에서는
    // 눈과의 대치(멈춤) 연출로 대신한다.
    [RequireComponent(typeof(Collider2D))]
    public class AwarenessUnlockMoment : MonoBehaviour
    {
        [SerializeField] SpriteRenderer eyeVisual;   // 거대 눈 오브제
        [SerializeField] float buildUpSeconds = 1.5f;
        [SerializeField] float holdSeconds = 1f;
        [SerializeField, TextArea(2, 4)] string fragmentText; // 해금 순간 화면에 띄울 한 줄
        [SerializeField] string fragmentId = "gaze_awareness";

        bool _triggered;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered) return;
            if (!PlayerLayers.IsPlayer(other.gameObject)) return;
            if (GameManager.Instance.Progress.HasAwareness) { _triggered = true; return; }

            _triggered = true;
            StartCoroutine(UnlockRoutine());
        }

        IEnumerator UnlockRoutine()
        {
            PlayerInput.Enabled = false;

            if (eyeVisual != null)
            {
                var baseScale = eyeVisual.transform.localScale;
                var baseColor = eyeVisual.color;

                float t = 0f;
                while (t < buildUpSeconds)
                {
                    t += Time.deltaTime;
                    float k = t / buildUpSeconds;
                    eyeVisual.transform.localScale = Vector3.Lerp(baseScale, baseScale * 1.35f, k);
                    eyeVisual.color = Color.Lerp(baseColor, Color.white, k);
                    yield return null;
                }

                yield return new WaitForSeconds(holdSeconds);

                // 정면으로 마주본 뒤에야 눈이 가라앉는다 — 포용의 결과.
                t = 0f;
                while (t < buildUpSeconds)
                {
                    t += Time.deltaTime;
                    float k = t / buildUpSeconds;
                    eyeVisual.transform.localScale = Vector3.Lerp(baseScale * 1.35f, baseScale, k);
                    eyeVisual.color = Color.Lerp(Color.white, baseColor * 0.6f, k);
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(buildUpSeconds + holdSeconds);
            }

            var progress = GameManager.Instance.Progress;
            progress.GrantAwareness();
            progress.CollectFragment(fragmentId);
            if (!string.IsNullOrEmpty(fragmentText)) GameManager.FragmentPresenter?.Invoke(fragmentText);

            PlayerInput.Enabled = true;
        }

        void OnDestroy()
        {
            // 연출 도중 씬이 내려가도 입력이 잠긴 채 남지 않게 한다.
            if (_triggered) PlayerInput.Enabled = true;
        }
    }
}
