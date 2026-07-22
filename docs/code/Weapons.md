# Weapons 모듈

플레이어의 자동 공격, 발사체, 그리고 두 종의 스킬(액티브 '업무 떠넘기기', 궁극기 '퇴사 통보')을 구현하는 스크립트 모음이다. 무기 수치는 `WeaponData`(HanGame.Data), 보유 여부는 `RunState`(HanGame.Common)로 관리하며, 대상 탐색은 `Day` 모듈의 `EnemyRegistry`를 사용한다. 모든 스크립트는 `HanGame.Weapons` 네임스페이스에 속한다.

---

## AutoAttackSystem.cs

**역할**: 보유한 자동 무기(키보드 샷건·스테이플러)를 각자 쿨타임으로 발사. 발사 시마다 가장 가까운 적을 재탐색하고, 상사의 시선 '일하는 척' 중에는 정지한다.

**상속/의존**: `MonoBehaviour`. `WeaponData`(HanGame.Data), `Player`/`PlayerStats`/`GameManager`/`AudioManager`/`WeaponIds`/`Sfx`(HanGame.Common), `BossGaze`/`EnemyRegistry`(HanGame.Day), `Projectile`에 의존.

**주요 멤버**
- `[SerializeField] WeaponData keyboardShotgun`, `WeaponData staplerRapid`, `BossGaze bossGaze`(없으면 항상 발사)
- 내부: `PlayerStats _stats`, `Dictionary<string,float> _cooldownTimers`(무기 id별 쿨타임)

**동작**
- `Update`: `Time.timeScale==0`(레벨업 정지) 또는 `bossGaze.PlayerCaught`(일하는 척)면 발사 중단. 아니면 두 무기 각각 `TickWeapon`.
- `TickWeapon`: `Run.HasWeapon(id)`로 보유 확인, `PlayerStats`의 공속·사거리·공격력 배수 적용. 쿨타임 소진 시 `Fire` 호출 → 성공하면 `attackInterval / 공속배수`로 재설정, 적이 없으면 0.1초 후 재시도.
- `Fire`: `EnemyRegistry.Nearest(플레이어, range)`로 조준.
  - 키보드 샷건(`WeaponIds.KeyboardShotgun`): `pellets` 개수를 `spreadAngle` 범위에 균등 분포시켜 부채꼴 다발 발사, `KeyboardHit` SFX.
  - 그 외(스테이플러): 단일 직선 발사, `StaplerFire` SFX.
- `SpawnProjectile`: `projectilePrefab` 인스턴스화, `Projectile.Launch(dir, projectileSpeed, damage, pierces)`. `Rotate`는 벡터를 지정 각도만큼 회전하는 헬퍼.

---

## Projectile.cs

**역할**: 플레이어 무기 발사체. 가구를 통과하므로 적 레이어만 검사하며, 관통 여부에 따라 첫 적중 시 소멸하거나 관통한다.

**상속/의존**: `MonoBehaviour`, `[RequireComponent(typeof(Collider2D))]`. `Enemy`(HanGame.Day)에 의존.

**주요 멤버**
- 내부: `_dir`, `_speed`, `_damage`, `_pierce`, `_life`
- `void Launch(Vector2 dir, float speed, float damage, bool pierce, float life = 3f)`

**동작**
- `Launch`: 방향 정규화 후 속도·피해·관통·수명 설정.
- `Update`: `_dir × _speed`로 직선 이동, `_life` 감소 후 0 이하면 파괴(화면 밖·미명중 정리).
- `OnTriggerEnter2D`: `Enemy` 접촉(살아있는 경우)이면 `TakeDamage(_damage)`. `_pierce`가 false면 파괴(스테이플러), true면 관통 유지.

---

## ResignationUltimate.cs

**역할**: 궁극기 '퇴사 통보'. R 키로 사용. 일반 적 공포(도주), 정예 감속, CEO 웨이브 3초 정지를 건다. 게이지가 가득 차야 사용 가능.

**상속/의존**: `MonoBehaviour`. `WeaponData`/`EnemyType`(HanGame.Data), `GameManager`/`AudioManager`/`WeaponIds`/`Sfx`(HanGame.Common), `Enemy`/`EnemyRegistry`/`EnemySpawner`(HanGame.Day)에 의존.

**주요 멤버**
- `[SerializeField] WeaponData data`, `KeyCode key = KeyCode.R`, `EnemySpawner spawner`(게이지 충전용, 선택)
- `float Gauge { get; }`[0~1], `bool Ready`(≥1), `event Action<float> GaugeChanged`, `event Action Used`

**동작**
- `OnEnable`/`OnDisable`: `spawner.EnemyKilled` 구독/해제.
- `OnKill`: 적 처리마다 `Gauge`를 `data.gaugePerKill`만큼 충전(최대 1), `GaugeChanged` 발행.
- `Update`: `Run.HasWeapon(ResignationNotice)` 미보유·`timeScale==0`이면 무시. R 입력 + `Ready`면 `Activate`.
- `Activate`: 살아있는 모든 적을 순회 — `CeoDirective`는 `ApplyStun(ceoStunDuration)`, 그 외는 `ApplyFear(fearDuration, eliteSlow)`. 게이지 0으로 리셋, `Resignation` SFX, `Used` 발행.

---

## TaskDelegateSkill.cs

**역할**: 액티브 스킬 '업무 떠넘기기'. Space 키로 사용. 주변 적을 바깥으로 밀어내 포위를 탈출한다. 쿨타임은 '짬' 강화로 감소한다.

**상속/의존**: `MonoBehaviour`. `WeaponData`(HanGame.Data), `Player`/`PlayerStats`/`GameManager`/`WeaponIds`(HanGame.Common), `Enemy`/`EnemyRegistry`(HanGame.Day)에 의존.

**주요 멤버**
- `[SerializeField] WeaponData data`, `KeyCode key = KeyCode.Space`
- `float Cooldown { get; }`, `float CooldownRemaining { get; }`, `event Action Used`
- 내부: `List<Enemy> _buffer`(재사용), `PlayerStats _stats`

**동작**
- `Start`: `Player.Local`의 `PlayerStats` 확보.
- `Update`: 쿨타임 감소. `Run.HasWeapon(TaskDelegate)` 미보유·`timeScale==0`이면 무시. Space 입력 + 쿨타임 종료면 `Activate`.
- `Activate`: `EnemyRegistry.InRadius(origin, pushRadius, _buffer)`로 반경 내 적을 모아 각각 `Push(origin, pushForce)`로 밀어냄. 쿨타임을 `data.cooldown × ActiveCooldownMul`로 설정, `Used` 발행.
