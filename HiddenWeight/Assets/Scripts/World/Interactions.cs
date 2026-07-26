using System.Collections.Generic;
using UnityEngine;

namespace HiddenWeight.World
{
    // 되감기(잔재) 대상. 전체 시간이 아니라 오브젝트 단위로 되돌린다 (기획서 4.2절).
    public interface IRewindable
    {
        Transform Transform { get; }
        bool CanRewind { get; }        // 이미 초기 상태면 false
        void CaptureInitial();         // Start에서 1회
        void Rewind();
    }

    // 자각(L 홀드) 중에만 반응하는 오브젝트.
    public interface IAwarenessReactive
    {
        void OnAwarenessChanged(bool active);
    }

    // 예지(균열) 대상. leadSeconds 뒤의 상태를 예측해 돌려준다.
    public interface IForeseeable
    {
        Transform Transform { get; }
        Vector3 PredictPosition(float leadSeconds);
        bool PredictActive(float leadSeconds);   // false면 그때는 사라져 있다
        Sprite CurrentSprite { get; }            // 고스트에 그대로 쓴다
    }

    // 자각 반응 오브젝트의 등록소. Task 8의 AwarenessSystem이 이 목록을 순회하며
    // OnAwarenessChanged를 호출한다. World가 Emotions를 참조하지 않고도 Emotions가
    // World를 관찰할 수 있게 하는 역방향 훅이다.
    public static class AwarenessRegistry
    {
        static readonly List<IAwarenessReactive> _items = new List<IAwarenessReactive>();
        public static IReadOnlyList<IAwarenessReactive> Items => _items;
        public static event System.Action<IAwarenessReactive> Added;

        public static void Register(IAwarenessReactive r)
        {
            if (r != null && !_items.Contains(r)) { _items.Add(r); Added?.Invoke(r); }
        }

        public static void Unregister(IAwarenessReactive r) => _items.Remove(r);
    }
}
