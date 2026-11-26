using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Dodge_Skill : Skill
{
    [Header("…¡±‹")]
    [Range(0,100)]
    [SerializeField] int evasionAmount=10;
    [SerializeField] private UI_SkillTreeSlot unlockDodgeButton;
    [SerializeField] public bool dodgeUnlocked;

    [Header("ª√”∞…¡±‹")]
    [SerializeField] private UI_SkillTreeSlot unlockMirageDodgeButton;
    [SerializeField] public bool mirageDogeUnlocked;

    protected override void Start()
    {
        base.Start();
        unlockDodgeButton.GetComponent<Button>().onClick.AddListener(UnlockDodge);
        unlockMirageDodgeButton.GetComponent<Button>().onClick.AddListener(UnlockMirageDodge);
    }

    protected override void CheckUnlock()
    {
        UnlockDodge();
        UnlockMirageDodge();
    }

    private void UnlockDodge()
    {
        if (unlockDodgeButton.unLocked)
        {
            player.stats.evasion.AddModifier(evasionAmount);
            Inventory.instance.UpdateStatsUI();
            dodgeUnlocked = true;
        }
    }

    private void UnlockMirageDodge()
    {
        if (unlockMirageDodgeButton.unLocked)
        {
            mirageDogeUnlocked = true;
        }
    }

    public void CreateMirageOnDodge()
    {
        if (dodgeUnlocked)
        {
            SkillManager.instance.clone.CreateClone(FindClosesEnemy(player.transform),new Vector3(2*player.facingDir,0));
        }

    }

}
