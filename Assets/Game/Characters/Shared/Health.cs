using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 10;
    public int currentHealth;

    [Header("Debug HUD")]
    public string displayName = "Character";
    public bool drawHealthOnScreen = true;

    public bool IsDead => currentHealth <= 0;

    public event Action<int> OnDamaged;
    public event Action OnDeath;

    void Start()
    {
        ResetHealth();
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(currentHealth, 0);

        Debug.Log(gameObject.name + " HP: " + currentHealth);

        OnDamaged?.Invoke(damage);

        if (currentHealth <= 0)
        {
            Debug.Log(gameObject.name + " is defeated!");
            OnDeath?.Invoke();
        }
    }
}