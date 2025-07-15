using _Scripts.Components;
using Unity.Entities;
using UnityEngine;

namespace _Scripts.Authorings
{
    public class CellAuthoring : MonoBehaviour
    {
        private class CellAuthoringBaker : Baker<CellAuthoring>
        {
            public override void Bake(CellAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.Dynamic);

                AddComponent<CellTag>(entity);
            }
        }
    }
}