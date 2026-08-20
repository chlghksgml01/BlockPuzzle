# Line Clear Sparkle

Ice/Grass/Gem 미션에서 클리어 예정 줄을 **ParticleSystem 프리팹**으로 감싸 표시한다.

## Prefab

- `Assets/2.Prefabs/VFX/LineClearSparkleParticle.prefab`
- 크기/색/방출량은 프리팹 Particle System Inspector에서 조절
- `BoardManager`의 `Line Clear Sparkle Particle Prefab`에 연결
- `LineClearSparkleSegment._sparkSprites`에 랜덤 스파클 스프라이트 할당
  - `Assets/10.Resources/LineClearSparkle/BlueCircleSpark.png`
  - `Assets/10.Resources/LineClearSparkle/BlueSparkle.png`
  - `Assets/10.Resources/LineClearSparkle/YellowCircleSpark.png`
  - `Assets/10.Resources/LineClearSparkle/YellowSparkle.png`

## Runtime

- `LineClearSparkleController`가 프리팹을 풀링
- `LineClearSparkleSegment.Show`는 위치 + BoxEdge 크기만 맞춤
- 서로 다른 텍스처 스프라이트는 런타임 아틀라스로 합친 뒤 Texture Sheet `Start Frame` 랜덤으로 파티클마다 1장 선택
