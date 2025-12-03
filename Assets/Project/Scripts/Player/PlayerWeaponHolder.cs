// PlayerWeaponHolder.cs
// Крепит 3D-модель оружия к кости руки и
// передаёт PlayerCombatController ссылку на MeleeWeaponDefinition.

using Project.Scripts.Combat;
using UnityEngine;

namespace Project.Scripts.Player
{
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponHolder : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private PlayerCombatController combat;

        [Header("Кость руки для крепления")]
        [SerializeField, Tooltip("Сокет/кость в иерархии рук (например, hand_r).")]
        private Transform handSocket;

        [Header("Оружие (данные)")]
        [SerializeField, Tooltip("ScriptableObject с параметрами палки.")]
        private MeleeWeaponDefinition weaponDefinition;

        private GameObject _weaponInstance;

        private void Awake()
        {
            if (weaponDefinition == null)
                return;

            combat.SetWeapon(weaponDefinition);

            if (!handSocket || weaponDefinition.WeaponPrefab == null)
                return;

            SpawnWeaponModel();
        }

        private void SpawnWeaponModel()
        {
            _weaponInstance = Instantiate(
                weaponDefinition.WeaponPrefab,
                handSocket
            );

            _weaponInstance.transform.localPosition = weaponDefinition.LocalPositionOffset;
            _weaponInstance.transform.localRotation = Quaternion.Euler(weaponDefinition.LocalRotationOffset);
            _weaponInstance.transform.localScale    = weaponDefinition.LocalScale;
        }
    }
}