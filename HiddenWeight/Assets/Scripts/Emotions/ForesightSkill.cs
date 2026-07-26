using System.Collections.Generic;
using UnityEngine;
using HiddenWeight.Data;
using HiddenWeight.World;

namespace HiddenWeight.Emotions
{
    // 예지. 탭 입력. 반경 안 IForeseeable들의 previewLeadTime 뒤 상태를 반투명 고스트로
    // effectDuration 동안 보여준다.
    public class ForesightSkill : EmotionSkill
    {
        public override EmotionId Id => EmotionId.Foresight;

        readonly List<GameObject> _ghosts = new List<GameObject>();
        float _timer;

        protected override void OnBegin()
        {
            _timer = Data.effectDuration;
            var hits = Physics2D.OverlapCircleAll(Player.transform.position, Data.range);
            foreach (var h in hits)
            {
                var f = h.GetComponentInParent<IForeseeable>();
                if (f == null) continue;
                if (!f.PredictActive(Data.previewLeadTime)) continue;   // 그때는 사라져 있다 → 고스트 없음
                SpawnGhost(f);
            }
        }

        void SpawnGhost(IForeseeable f)
        {
            var go = new GameObject("ForesightGhost");
            go.transform.position = f.PredictPosition(Data.previewLeadTime);
            go.transform.localScale = f.Transform.localScale;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = f.CurrentSprite;
            sr.color = new Color(1f, 1f, 1f, 0.35f);
            sr.sortingOrder = 50;
            _ghosts.Add(go);
        }

        protected override void OnTick(float dt)
        {
            _timer -= dt;
            if (_timer <= 0f) End();
        }

        protected override void OnEnd()
        {
            foreach (var g in _ghosts) if (g != null) Destroy(g);
            _ghosts.Clear();
        }

        // 무너질 발판이 "무너진 뒤의 형태"를 보여줘야 하는데, PredictActive가 false면 고스트를
        // 띄우지 않는 것으로 대신한다 — 발판이 있어야 할 자리에 아무것도 안 보이는 것이 곧 경고다.
    }
}
