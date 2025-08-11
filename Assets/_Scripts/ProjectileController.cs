using _Scripts.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace _Scripts
{
    public class ProjectileController : MonoBehaviour
    {
        private Vector3 _direction;
        private float _speed;

        private NativeHashMap<int3, Entity> _cellMap;
        private World _world;
        private EntityManager _entityManager;

        public void Initialize(Vector3 direction, float speed, int[] projectileCount)
        {
            _direction = direction.normalized;
            _speed = speed;
        }

        private void Start()
        {
            _world = World.DefaultGameObjectInjectionWorld;
            _entityManager = _world.EntityManager;

            var globalDataSystem = _world.GetExistingSystemManaged<GlobalDataInitSystem>();
            _cellMap = globalDataSystem.CellMap;
        }

        private void Update()
        {
            transform.position += Time.deltaTime * _speed * _direction;

            if (CheckCoordinateHasCell()) ApplyProjectileEffect();
        }

        private bool CheckCoordinateHasCell()
        {
            return default;
        }

        private void ApplyProjectileEffect()
        {
        }
    }
}