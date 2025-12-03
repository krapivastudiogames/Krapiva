// MeleeWeaponDefinition.cs
// Описание параметров палки/оружия для ближнего боя.

using UnityEngine;

namespace Project.Scripts.Combat
{
    [CreateAssetMenu(
        fileName = "MeleeWeaponDefinition",
        menuName = "Project/Combat/Melee Weapon",
        order    = 0)]
    public sealed class MeleeWeaponDefinition : ScriptableObject
    {
        [Header("Общее")]
        [SerializeField, Tooltip("Отображаемое имя оружия (для дебага/UI).")]
        private string _displayName = "Stick";

        [SerializeField, Tooltip("Слои, по которым может быть нанесён урон.")]
        private LayerMask _hitMask;

        public string    DisplayName => _displayName;
        public LayerMask HitMask     => _hitMask;

        [Header("Быстрая атака (ЛКМ клик)")]
        [SerializeField, Min(0f)]
        private float _lightDamage = 10f;

        [SerializeField, Min(0.1f)]
        private float _lightRange = 2f;

        [SerializeField, Tooltip("Затраты стамины на одну быструю атаку.")]
        [Min(0f)]
        private float _lightStaminaCost = 10f;

        [SerializeField, Tooltip("Длительность анимации быстрой атаки, сек.")]
        [Min(0.05f)]
        private float _lightDuration = 0.4f;

        [SerializeField, Tooltip("Момент нанесения удара в долях длительности (0..1).")]
        [Range(0f, 1f)]
        private float _lightHitTimeNormalized = 0.3f;

        [Header("Заряженная атака (удержание ЛКМ)")]
        [SerializeField, Min(0f)]
        private float _heavyDamage = 25f;

        [SerializeField, Min(0.1f)]
        private float _heavyRange = 2.5f;

        [SerializeField, Tooltip("Затраты стамины на одну заряженную атаку.")]
        [Min(0f)]
        private float _heavyStaminaCost = 25f;

        [SerializeField, Tooltip("Сколько секунд нужно удерживать ЛКМ, чтобы сработала тяжёлая атака.")]
        [Min(0.1f)]
        private float _heavyChargeTime = 0.6f;

        [SerializeField, Tooltip("Длительность анимации тяжёлой атаки, сек.")]
        [Min(0.1f)]
        private float _heavyDuration = 0.7f;

        [SerializeField, Tooltip("Момент нанесения удара в долях длительности (0..1).")]
        [Range(0f, 1f)]
        private float _heavyHitTimeNormalized = 0.35f;

        public float LightDamage            => _lightDamage;
        public float LightRange             => _lightRange;
        public float LightStaminaCost       => _lightStaminaCost;
        public float LightDuration          => _lightDuration;
        public float LightHitTimeNormalized => _lightHitTimeNormalized;

        public float HeavyDamage            => _heavyDamage;
        public float HeavyRange             => _heavyRange;
        public float HeavyStaminaCost       => _heavyStaminaCost;
        public float HeavyChargeTime        => _heavyChargeTime;
        public float HeavyDuration          => _heavyDuration;
        public float HeavyHitTimeNormalized => _heavyHitTimeNormalized;

        [Header("Визуал (модель в руке)")]
        [SerializeField, Tooltip("Префаб 3D-модели оружия (палка и т.п.).")]
        private GameObject _weaponPrefab;

        [SerializeField, Tooltip("Локальная позиция относительно сокета руки.")]
        private Vector3 _localPositionOffset = Vector3.zero;

        [SerializeField, Tooltip("Локальный вращательный оффсет (Эйлер).")]
        private Vector3 _localRotationOffsetEuler = Vector3.zero;

        [SerializeField, Tooltip("Локальный масштаб модели.")]
        private Vector3 _localScale = Vector3.one;

        public GameObject WeaponPrefab          => _weaponPrefab;
        public Vector3    LocalPositionOffset   => _localPositionOffset;
        public Vector3    LocalRotationOffset   => _localRotationOffsetEuler;
        public Vector3    LocalScale            => _localScale;
    }
}
