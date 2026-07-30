using System.Collections;
using UnityEngine;
using HiddenWeight.UI;
using HiddenWeight.Core;

namespace HiddenWeight.Player
{
    // 공격 시각 피드백. 플레이스홀더 단계라 Animator가 없어서, J를 눌러도 화면에는 아무 변화가
    // 없었다 — 적 체력은 실제로 깎이고 있는데도 "공격 키가 없는 것 같다"고 느껴지는 이유였다.
    //
    // 애니메이션 대신 판정 범위와 같은 크기·방향의 반원형 섬광을 짧게 띄운다. 연출이자
    // 동시에 디버그 정보다 — 실제로 닿는 범위가 눈에 보이므로 헛스윙 여부를 바로 안다.
    public class AttackVisual : MonoBehaviour
    {
        [SerializeField] Sprite sprite;
        [SerializeField] Color color = new Color(1f, 0.95f, 0.8f, 0.55f);

        // 참격 프레임(CombatVFX_v1의 SwingSlash 행). 채워져 있으면 사각 섬광 대신 이것을
        // 공격 시간에 맞춰 한 번 재생한다 — 비어 있으면 예전 방식으로 떨어져, 아트가 아직
        // 없는 지역에서도 공격 피드백 자체는 사라지지 않는다.
        [SerializeField] Sprite[] slashFrames;

        PlayerAttack _attack;
        PlayerController _controller;
        SpriteRenderer _flash;
        Coroutine _routine;

        void Awake()
        {
            _attack = GetComponent<PlayerAttack>();
            _controller = GetComponent<PlayerController>();

            var go = new GameObject("AttackFlash");
            go.transform.SetParent(transform, false);
            _flash = go.AddComponent<SpriteRenderer>();
            _flash.sprite = sprite;
            _flash.color = color;
            _flash.sortingOrder = 11; // 플레이어(10) 바로 위
            _flash.enabled = false;
        }

        void OnEnable()
        {
            if (_attack != null) _attack.Attacked += HandleAttacked;
        }

        void OnDisable()
        {
            if (_attack != null) _attack.Attacked -= HandleAttacked;
        }

        void HandleAttacked()
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            if (UISettings.ReduceFlash)
            {
                _flash.enabled = false;
                _routine = null;
                yield break;
            }
            float radius = GameManager.Instance.Balance.player.attackRadius;
            float duration = Mathf.Max(0.08f, GameManager.Instance.Balance.player.attackActiveTime);

            // 바라보는 쪽으로 판정 반경만큼 내민다.
            _flash.transform.localPosition = new Vector3(_controller.Facing * radius * 0.5f, 0f, 0f);
            _flash.enabled = true;

            if (slashFrames != null && slashFrames.Length > 0)
            {
                // 참격 프레임을 공격 시간에 정확히 맞춰 한 번 재생한다. 시트는 오른쪽 방향
                // 기준이라 왼쪽을 볼 때는 뒤집는다.
                _flash.flipX = _controller.Facing < 0;
                _flash.color = Color.white;

                float frameTime = duration / slashFrames.Length;
                for (int i = 0; i < slashFrames.Length; i++)
                {
                    var frame = slashFrames[i];
                    _flash.sprite = frame;

                    // 판정 반경과 그림 크기를 맞춘다(참격 궤적이 실제 닿는 범위를 보여주는
                    // 디버그 겸 연출이라는 원래 목적 그대로).
                    float spriteWidth = frame != null ? frame.bounds.size.x : 0f;
                    float scale = spriteWidth > 0f ? radius * 1.6f / spriteWidth : 1f;
                    _flash.transform.localScale = new Vector3(scale, scale, 1f);

                    float elapsedInFrame = 0f;
                    while (elapsedInFrame < frameTime)
                    {
                        elapsedInFrame += Time.deltaTime;
                        yield return null;
                    }
                }

                _flash.enabled = false;
                _flash.sprite = sprite;
                _flash.flipX = false;
                _flash.color = color;
                _routine = null;
                yield break;
            }

            _flash.transform.localScale = new Vector3(radius, radius * 1.2f, 1f);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                var c = color;
                c.a = color.a * (1f - elapsed / duration); // 빠르게 사라진다
                _flash.color = c;
                elapsed += Time.deltaTime;
                yield return null;
            }

            _flash.enabled = false;
            _flash.color = color;
            _routine = null;
        }
    }
}
