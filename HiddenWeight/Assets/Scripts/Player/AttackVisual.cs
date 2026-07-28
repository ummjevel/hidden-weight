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
            _flash.transform.localScale = new Vector3(radius, radius * 1.2f, 1f);
            _flash.enabled = true;

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
