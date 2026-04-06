using UnityEngine;

public class GameUI : MonoBehaviour
{
    [Header("Targets")]
    public FighterController player;
    public FighterController opponent;

    public Health playerHealth;
    public Health opponentHealth;

    [Header("Layout")]
    public Vector2 playerUIPos = new Vector2(10, 10);
    public Vector2 opponentUIPos = new Vector2(300, 10);
    public float lineHeight = 25f;

    [Header("Combo UI")]
    public Vector2 playerComboPos = new Vector2(10, 180);
    public Vector2 opponentComboPos = new Vector2(300, 180);

    void OnGUI()
    {
        if (player != null)
        {
            DrawCharacterUI(player, playerHealth, playerUIPos, "PLAYER");
            DrawComboUI(player, playerComboPos);
        }

        if (opponent != null)
        {
            DrawCharacterUI(opponent, opponentHealth, opponentUIPos, "OPPONENT");
            DrawComboUI(opponent, opponentComboPos);
        }
    }

    void DrawCharacterUI(FighterController c, Health h, Vector2 pos, string label)
    {
        float x = pos.x;
        float y = pos.y;
        int line = 0;

        GUI.Label(new Rect(x, y + line++ * lineHeight, 300, 25), label);

        if (h != null)
        {
            GUI.Label(
                new Rect(x, y + line++ * lineHeight, 300, 25),
                "HP: " + h.currentHealth + "/" + h.maxHealth
            );
        }

        GUI.Label(
            new Rect(x, y + line++ * lineHeight, 300, 25),
            "State: " + c.CurrentState
        );

        GUI.Label(
            new Rect(x, y + line++ * lineHeight, 300, 25),
            "Blocking: " + c.IsBlocking
        );

        GUI.Label(
            new Rect(x, y + line++ * lineHeight, 300, 25),
            "Blockstun: " + c.IsInBlockstun
        );

        GUI.Label(
            new Rect(x, y + line++ * lineHeight, 300, 25),
            "Attack Phase: " + c.CurrentAttackPhase
        );

        string attackName = c.CurrentAttackData != null
            ? c.CurrentAttackData.attackName
            : "None";

        GUI.Label(
            new Rect(x, y + line++ * lineHeight, 300, 25),
            "Current Attack: " + attackName
        );

        Rigidbody2D rb = c.GetRigidbody();
        if (rb != null)
        {
            GUI.Label(
                new Rect(x, y + line++ * lineHeight, 300, 25),
                "Velocity: " + rb.linearVelocity
            );
        }
    }

    void DrawComboUI(FighterController c, Vector2 pos)
    {
        if (c.comboCount <= 1) return;

        string comboText = c.comboCount == 2
            ? "2 HIT"
            : c.comboCount + " HIT COMBO";

        GUI.Label(new Rect(pos.x, pos.y, 250, 30), comboText);
    }
}