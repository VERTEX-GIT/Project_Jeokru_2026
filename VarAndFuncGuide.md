## 사용하는 변수들 정리(요약)

### TileCoordinateManager.cs
`TempTilemap` 오브젝트에 부착됨. 좌표 변환 담당.
외부에서 사용할 변수 없음.

### TileOccupancyManager.cs
`Grid` 오브젝트에 부착됨. 타일별 게임 상태를 Dictionary로 관리.
- `Instance` - 싱글톤
- `CoordinateManager` - CoordinateManager.cs

### TileObjectPlacement.cs
`유닛`과 `공장` 오브젝트에 부착됨. 타일에 오브젝트 배치하는 것 관리.
- `AnchorCell` - 오브젝트 점유 영역의 좌측 하단 기준 좌표
- `OccupiedCells` - 오브젝트가 현재 점유하고 있는 모든 타일 좌표
- `IsPlaced` - 오브젝트가 타일에 배치되어 있는지 여부
- `ObjectType` - 오브젝트가 유닛인지 시설인지 나타냄
- `Size` - 오브젝트가 점유하는 가로·세로 타일 수

### FactoryWorkArea.cs
`공장` 오브젝트에 부착됨. 공장의 작업 타일 관리.
- `WorkCells` - 공장이 사용하는 작업 타일 좌표
- `IsRegistered` - 작업 영역이 타일 점유 관리자에 등록되어 있는지 나타냄

---

## 사용하는 함수들 정리

### TileCoordinateManager.cs
`TempTilemap` 오브젝트에 부착됨. 좌표 변환 담당.
- `WorldToCell(Vector3 worldPosition)` - 월드 좌표를 타일 좌표로 변환
- `CellToWorldCenter(Vector3Int cell)` - 타일 좌표를 타일 중앙의 월드 좌표로 변환
- `HasTile(Vector3Int cell)` - 해당 좌표에 실제 타일이 존재하는지 확인

### TileOccupancyManager.cs
`Grid` 오브젝트에 부착됨. 타일별 게임 상태를 Dictionary로 관리.
- `Dictionaries`
    - `HasOccupant(Vector3Int cell)` - 해당 타일에 유닛이나 시설이 있는지 확인
    - `IsReserved(Vector3Int cell)` - 해당 타일이 유닛의 이동 목적지로 예약됐는지 확인
    - `IsWorkArea(Vector3Int cell)` - 해당 타일이 공장의 작업 영역인지 확인
- `TryGetOccupant(Vector3Int cell, out TileObjectPlacement occupant)` - 타일을 점유한 오브젝트를 가져옴
- `TryGetWorkAreaOwner(Vector3Int cell, out FactoryWorkArea owner)` - 해당 작업 타일을 소유한 공장을 가져옴

### TileObjectPlacement.cs
`유닛`과 `공장` 오브젝트에 부착됨. 타일에 오브젝트 배치하는 것 관리.
- `TryPlace(Vector3Int anchorCell)` - 배치 가능 여부를 검사하고 성공하면 오브젝트를 배치
- `RemoveFromTiles()` - 오브젝트의 점유 정보와 공장 작업 영역을 제거

### FactoryWorkArea.cs
`공장` 오브젝트에 부착됨. 공장의 작업 타일 관리.
- `Contains(Vector3Int cell)` - 해당 좌표가 이 공장의 실제 작업 타일인지 확인