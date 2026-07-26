using UnityEngine;

namespace HiddenWeight.World
{
    // 회전형 "시선" (기획서 EMOTION_SYSTEM 2.3절 — 고정형/회전형 두 종류로 난이도 조절).
    // GazeHazard가 transform.right 방향을 시야로 쓰므로, Z축 회전만 시켜 주면
    // 감지 로직 수정 없이 시야각이 통로를 훑는다.
    public class GazeRotator : MonoBehaviour
    {
        [SerializeField] float degreesPerSecond = 60f;

        void Update()
        {
            transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
        }
    }
}
