using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Parry_Skill : Skill
{
    [Header("反击")]
    [SerializeField] private UI_SkillTreeSlot parryUnLockButton;
    public bool parryUnLocked { get; private set ;}

    [Header("反击恢复")]
    [SerializeField] private UI_SkillTreeSlot restoreUnLockedButton;
    [Range(0f, 1f)]
    [SerializeField] private float restoreHealthPerentage;

    public bool restoreUnLocked { get; private set ;}

    [Header("幻影反击")]
    [SerializeField] private UI_SkillTreeSlot parryWithMirageUnLockedButton;
    public bool  parryWithMirageUnLocked { get; private set ;}



    public override void UseSkill()
    {
        base.UseSkill();
        if (restoreUnLocked)
        {
            int restoreAmount=Mathf.RoundToInt(player.stats.GetMaxHealthValue()*restoreHealthPerentage);
            player.stats.IncreaseHealthBy(restoreAmount);

        }

    }

    protected override void Start()
    {
        base.Start();
        parryUnLockButton.GetComponent<Button>().onClick.AddListener(UnlockParry);
        restoreUnLockedButton.GetComponent<Button>().onClick.AddListener(UnlockParryRestore);
        parryWithMirageUnLockedButton.GetComponent<Button>().onClick.AddListener(UnlockParryWithMirage);

    }
    protected override void CheckUnlock()
    {
        UnlockParry();
        UnlockParryRestore();
        UnlockParryWithMirage();

    }

    private void UnlockParry()
    {
        if (parryUnLockButton.unLocked)
        {
            parryUnLocked = true;
        }

    }

    private void UnlockParryRestore()
    {
        if (restoreUnLockedButton.unLocked)
        {
            restoreUnLocked = true;
        }
    }

    private void UnlockParryWithMirage()
    {
        if (parryWithMirageUnLockedButton.unLocked)
        {
            parryWithMirageUnLocked = true;
        }
    }

    public void MakeMirageOnParry(Transform _responseTransform)
    {
        if (parryWithMirageUnLocked)
        {
            SkillManager.instance.clone.CreateCloneWithDelay(_responseTransform);
        }

    }


}
