using UnityEngine;
using System.Collections;

public class Firetrap : MonoBehaviour
{
    [SerializeField] private float damage = 1f; // Inicializado por seguridad

    [Header("Firetrap Timers")]
    [SerializeField] private float activationDelay = 0.5f;
    [SerializeField] private float activeTime = 1.5f;
    private Animator anim;
    private SpriteRenderer spriteRend;

    private bool triggered; // cuando la trampa es disparada (ej: el jugador entra)
    private bool active; // cuando la trampa está activa y puede dañar al jugador

    private void Awake()
    {
        anim = GetComponent<Animator>();
        spriteRend = GetComponent<SpriteRenderer>();
    }

    // --- En Firetrap.cs ---

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.tag == "Player")
        {
            if (!triggered)
                StartCoroutine(ActivateFiretrap());

            if (active)
            {
                // CORRECCIÓN: Apuntar al script que realmente maneja la vida
                collision.GetComponent<PlayerController>()?.TakeDamage((int)damage);
                // Usamos (int)damage porque PlayerController.TakeDamage espera un entero
            }
        }
    }

    private IEnumerator ActivateFiretrap()
    {
        //turn the sprite red to notify the player and trigger the trap
        triggered = true;
        spriteRend.color = Color.red;

        //Wait for delay, activate trap, turn on animation, return color back to normal
        yield return new WaitForSeconds(activationDelay);
        spriteRend.color = Color.white; //turn the sprite back to its initial color
        active = true;
        anim.SetBool("activated", true);

        //Wait until X seconds, deactivate trap and reset all variables and animator
        yield return new WaitForSeconds(activeTime);
        active = false;
        triggered = false;
        anim.SetBool("activated", false);
    }
}