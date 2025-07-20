using _Scripts.Authorings;
using _Scripts.Components;
using Unity.Entities;
using Unity.Rendering;

namespace _Scripts.Aspects
{
    public readonly partial struct CellPrototypeAspect : IAspect
    {
        public readonly Entity Self;
        private readonly RefRO<CellPrototypeTag> _cellPrototypeTag;
        private readonly RefRW<MaterialMeshInfo> _materialMeshInfo;
        private readonly RefRW<CellType> _cellType;

        public CellTypeEnum CellType
        {
            get => _cellType.ValueRO.Value;
            set => SetCellType(value);
        }

        private void SetCellType(CellTypeEnum targetCellType)
        {
        }
    }
}