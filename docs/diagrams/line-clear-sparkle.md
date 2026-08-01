# Line Clear Sparkle

Ice/Grass/Gem 미션에서 클리어 예정 줄을 **ParticleSystem 프리팹**으로 감싸 표시한다.

## Prefab

- `Assets/2.Prefabs/VFX/LineClearSparkleParticle.prefab`
- 크기/색/방출량은 프리팹 Particle System Inspector에서 조절
- `BoardManager`의 `Line Clear Sparkle Particle Prefab`에 연결

## Runtime

- `LineClearSparkleController`가 프리팹을 풀링
- `LineClearSparkleSegment.Show`는 위치 + BoxEdge 크기만 맞춤
