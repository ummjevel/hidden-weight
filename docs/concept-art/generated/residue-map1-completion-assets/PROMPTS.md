# 잔재 1맵 보완 애니메이션 생성 기록

잔재 지역의 기존 검은 고딕 철골, 회갈색 석재, 낡은 청동, 절제된 호박색과 남색 기억광을
기준으로 다음 7개 애니메이션 시트를 제작했다. 모든 최종본은 투명 RGBA PNG이며 각 행은
왼쪽에서 오른쪽으로 8프레임이다.

| 원본 | 최종 구성 |
|---|---|
| `CheckpointShrine_Animation_Source.png` | 체크포인트 활성화·대기·회복, 되감기 성소 해제 |
| `CollectibleIdle_Animation_Source.png` | 재화·회복·최대 체력·기억 파편·되감기 핵 대기 |
| `ShortcutOperations_Animation_Source.png` | 도르래·승강기·관문·사슬다리 작동 |
| `SecretEntrances_Animation_Source.png` | S1·S2·S3 비밀 경로 암시와 개방 |
| `BossArenaDevices_Animation_Source.png` | 감시탑 장치·안전 발판·갈고리 예고·봉인 파열 |
| `AmbientDetails_Animation_Source.png` | 사슬·매달린 형상·낙진·웅덩이 |
| `AmbientBackgroundTransitions_Animation_Source.png` | 거대 손가락·전환 안개·원경 불빛 |

공통 생성 조건은 균일한 크로마 그린 배경, 고정된 셀 중심과 크기, 행별 동일 오브젝트,
텍스트·격자·캐릭터·완성 배경 제외다. 생성 원본에서 그린 배경을 제거하고 8열로 정확히
분할 가능한 크기로 정규화했다.
