// IDamageable.cs
// Минимальный интерфейс для получения урона.

namespace Project.Scripts.Combat
{
    public interface IDamageable
    {
        void TakeDamage(float amount);
    }
}