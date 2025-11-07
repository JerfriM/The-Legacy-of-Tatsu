// IEnemy.cs
public interface IEnemy
{
    // Esta propiedad DEBE existir en todas las clases de Orco.
    bool IsDead { get; }

    // (Opcional: puedes agregar otros métodos como TakeDamage, etc.)
    void Die();
}