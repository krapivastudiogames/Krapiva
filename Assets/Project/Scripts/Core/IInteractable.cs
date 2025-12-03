// IInteractable.cs
// Базовый интерфейс для всех интерактивных объектов.

using UnityEngine;

namespace Project.Scripts.Core
{
    public interface IInteractable
    {
        void Interact(GameObject interactor);
    }
}