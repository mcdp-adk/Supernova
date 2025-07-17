using _Scripts.Components;
using Unity.Entities;

namespace _Scripts.Aspects
{
    public readonly partial struct CellAspect : IAspect
    {
        public readonly Entity Self;
        private readonly RefRO<CellTag> _tag;
    }
}