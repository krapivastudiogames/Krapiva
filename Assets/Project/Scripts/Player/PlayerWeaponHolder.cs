// PlayerWeaponHolder.cs
// Крепит 3D-модель оружия к кости руки и
// передаёт PlayerCombatController ссылку на MeleeWeaponDefinition.
//
// Папка: Project/Scripts/Player
// Namespace: Project.Scripts.Player

using UnityEngine;
using Project.Scripts.Combat;

namespace Project.Scripts.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerCombatController))]
    public sealed class PlayerWeaponHolder : MonoBehaviour
    {
        [Header("Кость руки для крепления")]
        [SerializeField, Tooltip("Сокет/кость в иерархии рук (например, hand_r).")]
        private Transform _handSocket;

        [Header("Оружие (данные)")]
        [SerializeField, Tooltip("ScriptableObject с параметрами палки.")]
        private MeleeWeaponDefinition _weaponDefinition;

        private PlayerCombatController _combat;
        private GameObject             _weaponInstance;

        private void Awake()
        {
            _combat = GetComponent<PlayerCombatController>();

            if (_combat == null)
            {
                Debug.LogError("PlayerWeaponHolder: не найден PlayerCombatController на объекте.");
                enabled = false;
                return;
            }

            if (_weaponDefinition == null)
            {
                Debug.LogWarning("PlayerWeaponHolder: не назначен WeaponDefinition.");
                return;
            }

            // Передаём оружие в боевой контроллер
            _combat.SetWeapon(_weaponDefinition);

            // Спавним визуал
            if (_handSocket != null && _weaponDefinition.WeaponPrefab != null)
            {
                SpawnWeaponModel();
            }
            else
            {
                Debug.LogWarning("PlayerWeaponHolder: не назначен HandSocket или WeaponPrefab в WeaponDefinition.");
            }
        }

        private void SpawnWeaponModel()
        {
            _weaponInstance = Instantiate(
                _weaponDefinition.WeaponPrefab,
                _handSocket
            );

            _weaponInstance.transform.localPosition = _weaponDefinition.LocalPositionOffset;
            _weaponInstance.transform.localRotation = Quaternion.Euler(_weaponDefinition.LocalRotationOffset);
            _weaponInstance.transform.localScale    = _weaponDefinition.LocalScale;
        }
    }
}
