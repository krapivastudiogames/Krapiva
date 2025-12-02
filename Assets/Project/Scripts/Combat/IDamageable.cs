// IDamageable.cs
// Минимальный интерфейс для получения урона.
//
// Папка: Project/Scripts/Combat
// Namespace: Project.Scripts.Combat

namespace Project.Scripts.Combat
{
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }
}