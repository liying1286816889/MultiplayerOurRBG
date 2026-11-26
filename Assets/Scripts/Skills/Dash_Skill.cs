using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Dash_Skill : Skill
{



    [Header("Dash")]
    [SerializeField] private UI_SkillTreeSlot dashUnLockButton;
    public bool dashUnlocked { get; private set; }

    [Header("Clone On Dash")]
    [SerializeField] private UI_SkillTreeSlot cloneOnDashUnlockedButton;
    public bool cloneOnDashUnlocked { get; private set; }

    [Header("Clone On Arrival")]
    [SerializeField] private UI_SkillTreeSlot cloneOnArrivalUnlockedButton;
    public bool cloneOnArrivalUnlocked { get; private set; }



    public override void UseSkill()
    {
        base.UseSkill();
    }

    protected override void Start()
    {
        base.Start();

        dashUnLockButton.GetComponent<Button>().onClick.AddListener(UnlockDash);
        
        cloneOnDashUnlockedButton.GetComponent<Button>().onClick.AddListener(UnlockCloneOnDash);

        cloneOnArrivalUnlockedButton.GetComponent<Button>().onClick.AddListener(UnlockCloneOnArrival);
    }

    protected override void CheckUnlock()
    {
        UnlockDash();
        UnlockCloneOnDash();
        UnlockCloneOnArrival();

    }

    private void UnlockDash()
    {
        if (dashUnLockButton.unLocked)
        {
            dashUnlocked = true;
        }

    }

    private void UnlockCloneOnDash() 
    {
        if (cloneOnDashUnlockedButton.unLocked)
        {

            cloneOnDashUnlocked = true;
        }
    }

    private void UnlockCloneOnArrival()
    {
        if (cloneOnArrivalUnlockedButton.unLocked) 
        { 
            cloneOnArrivalUnlocked = true; 
        }
    }



    public void CloneOnDash()
    {
        if (cloneOnDashUnlocked)
        {
            SkillManager.instance.clone.CreateClone(player.transform, Vector3.zero);
        }

    }
    public void CloneOnArrival()
    {
        if (cloneOnArrivalUnlocked)
        {
            SkillManager.instance.clone.CreateClone(player.transform, Vector3.zero);
        }

    }


}
