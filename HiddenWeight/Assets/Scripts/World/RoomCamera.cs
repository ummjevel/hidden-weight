using System.Collections;
using UnityEngine;
using HiddenWeight.UI;
using HiddenWeight.Player;

namespace HiddenWeight.World
{
    // 룸 단위로 플레이어를 따라가는 카메라. 결과 위치를 현재 룸 경계 안으로 클램프한다.
    [RequireComponent(typeof(Camera))]
    public class RoomCamera : MonoBehaviour
    {
        // 카메라 추적 부드러움 계수. 밸런스 수치가 아니라 연출용 값이라 여기 인스펙터 필드로 둔다.
        [SerializeField] float followLerp = 8f;

        // 착지 등에서 쓰는 기본 흔들림 세기·지속시간. 연출용 값이라 PlayerData가 아닌 여기 둔다.
        [SerializeField] float defaultShakeDuration = 0.12f;
        [SerializeField] float defaultShakeMagnitude = 0.06f;

        Vector2 _shakeOffset;
        Coroutine _shakeRoutine;

        public static RoomCamera Instance { get; private set; }
        public Room CurrentRoom { get; private set; }
        public event System.Action<Room> RoomChanged;

        void Awake()
        {
            Instance = this;
        }

        void LateUpdate()
        {
            var player = PlayerController.Instance;
            if (player == null) return; // 씬에 플레이어가 아직 없을 수 있다

            Vector2 target = ComputeClampedTarget(player.transform.position);
            transform.position = Vector3.Lerp(transform.position, new Vector3(target.x, target.y, transform.position.z), followLerp * Time.deltaTime)
                + new Vector3(_shakeOffset.x, _shakeOffset.y, 0f);
        }

        // 기본 세기(착지처럼 약한 흔들림)로 흔든다.
        public void Shake() => Shake(defaultShakeDuration, defaultShakeMagnitude);

        public void Shake(float duration, float magnitude)
        {
            if (UISettings.ReduceMotion) return;
            if (_shakeRoutine != null) StopCoroutine(_shakeRoutine);
            _shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                _shakeOffset = Random.insideUnitCircle * magnitude;
                elapsed += Time.deltaTime;
                yield return null;
            }
            _shakeOffset = Vector2.zero;
            _shakeRoutine = null;
        }

        // CurrentRoom 경계 안으로 클램프된 카메라 목표 위치를 계산한다.
        // CurrentRoom이 없으면 원래 목표(플레이어 위치)를 그대로 돌려준다.
        Vector2 ComputeClampedTarget(Vector2 target)
        {
            if (CurrentRoom == null) return target;

            var b = CurrentRoom.WorldBounds;

            // 화면 전체를 룸 안에 가두면 방 사이의 짧은 통로에서 카메라만 멈춘다.
            // 플레이어는 화면 끝으로 사라지고 다음 룸 트리거에 닿은 뒤에야 카메라가 크게
            // 튀는 결과가 된다. 카메라의 '중심'만 룸 안에 두면 가장자리에서 이웃 공간을
            // 미리 보여 주면서도 현재 룸에서 완전히 벗어나지는 않아 전환이 연속적으로 읽힌다.
            float x = Mathf.Clamp(target.x, b.min.x, b.max.x);
            float y = Mathf.Clamp(target.y, b.min.y, b.max.y);
            return new Vector2(x, y);
        }

        // CurrentRoom을 바꾸기만 한다. 급전환은 LateUpdate의 Lerp가 자연히 흡수한다.
        public void SetRoom(Room room)
        {
            if (CurrentRoom == room) return;
            CurrentRoom = room;
            RoomChanged?.Invoke(room);
        }

        // Lerp 없이 즉시 플레이어 위치로 이동한다 (씬 진입·리스폰용).
        public void SnapToPlayer()
        {
            var player = PlayerController.Instance;
            if (player == null) return;

            Vector2 target = ComputeClampedTarget(player.transform.position);
            transform.position = new Vector3(target.x, target.y, transform.position.z);
        }
    }
}
