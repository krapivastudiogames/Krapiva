// Workbench.cs
// Верстак в хабе: игрок входит в триггер рядом с верстаком,
// нажимает E -> открывается панель верстака (через WorkbenchPanelUI).
// Работает ТОЛЬКО по триггеру, без Raycast.

using Project.Scripts.Player;
using Project.Scripts.UI;
using UnityEngine;

namespace Project.Scripts.Hub
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public sealed class Workbench : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private WorkbenchPanelUI panel;

        [Header("Управление")]
        [SerializeField] private KeyCode interactKey = KeyCode.E;

        private FirstPersonController currentPlayer;
        private bool                  playerInside;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (playerInside)
                return;

            var player = other.GetComponentInParent<FirstPersonController>();
            if (player == null)
                return;

            currentPlayer = player;
            playerInside  = true;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!playerInside)
                return;

            var player = other.GetComponentInParent<FirstPersonController>();
            if (player == null || player != currentPlayer)
                return;

            currentPlayer = null;
            playerInside  = false;
        }

        private void Update()
        {
            if (!playerInside)
                return;

            if (!Input.GetKeyDown(interactKey))
                return;

            if (panel.IsOpen)
                return;

            panel.Open();
        }
    }
}