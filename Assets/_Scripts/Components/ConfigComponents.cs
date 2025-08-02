using _Scripts.Utilities;
using Unity.Entities;

namespace _Scripts.Components
{
    public struct CellConfigTag : IComponentData
    {
    }

    public struct CellConfigBuffer : IBufferElementData
    {
        public CellConfig Data;
    }
}