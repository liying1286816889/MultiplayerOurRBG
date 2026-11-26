using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class Blackhole_Skill : Skill
{

    [SerializeField] private UI_SkillTreeSlot blackHoleUnlockButton;
    public bool blackHoleUnlocked { get; private set; }
    [SerializeField] private int amountOfAttacks;
    [SerializeField] private float cloneCooldown;
    [SerializeField] private float blackholeDuration;

    [Header("ËÙ¶ÈÉèÖÃ")]
    [SerializeField]public GameObject blackholePrefab;
    [SerializeField]private float maxSize;
    [SerializeField] private float growSpeed;
    [SerializeField] private float shrinkSpeed;

    Blackhole_Skill_Controller currentBlackhole;

    private void UnlockBlackHole()
    {
        if (blackHoleUnlockButton.unLocked)
        {
            blackHoleUnlocked = true;
        }

    }

    public override bool CanUseSkill()
    {
        return base.CanUseSkill();
    }

    public GameObject FxPrefab => blackholePrefab;

    public override void UseSkill()    {
        base.UseSkill();

        GameObject newBlackhole= Instantiate(blackholePrefab,player.transform.position,Quaternion.identity);
        currentBlackhole=newBlackhole.GetComponent<Blackhole_Skill_Controller>();

        currentBlackhole.SetupBlackhole(
            maxSize,
            growSpeed,
            shrinkSpeed,
            amountOfAttacks,
            cloneCooldown,
            blackholeDuration
            );

        NetSkillFx.Broadcast(SkillFxKind.Blackhole, newBlackhole.transform.position, Vector3.zero,
            maxSize, growSpeed, blackholeDuration);
        AudioManager.instance.PlaySFX(3);
        StartCoroutine(AudioManager.instance.WaitSecond(1f,3));
        AudioManager.instance.PlaySFX(8);
    }

    protected override void Start()
    {
        base.Start();
        blackHoleUnlockButton.GetComponent<Button>().onClick.AddListener(UnlockBlackHole);
    }

    protected override void Update()
    {
        base.Update();
    }
    public bool SkillCompleted()
    {
        if(!currentBlackhole) return false;


        if (currentBlackhole.playerCanExitState)
        {
            currentBlackhole = null;
            return true;
        }
        return false;
    }
    public float GetBlackholeRadious()
    {
        return maxSize / 2;
    }

    protected override void CheckUnlock()
    {
        base.CheckUnlock();

        UnlockBlackHole();
    }

    

}
