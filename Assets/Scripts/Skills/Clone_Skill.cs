using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Clone_Skill : Skill
{
    [Header("克隆信息")]
    [SerializeField] private float attackMultiplier;
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private float cloneDuration;
    [Space]
    [Header("幻影攻击")]
    [SerializeField] private UI_SkillTreeSlot cloneAttackUnlockButton;
    [SerializeField] private float cloneAttackMutiplier;
    [SerializeField] private bool canAttack;

    [Header("幻影增强")]
    [SerializeField] private UI_SkillTreeSlot aggresiveCloneUnlockButton;
    [SerializeField] private float aggresiveCloneAttackMutiplier;
    public bool canApplyOnHitEffect{ get; private set; }

    [Header("多重复制")]
    [SerializeField] private UI_SkillTreeSlot multipleUnlockButton;
    [SerializeField] private float multicCloneAttackMultiplier;
    private bool createCloneOnDashStar;
    private bool createCloneOnDashOver;

    
    [SerializeField] private bool canDuplicateClone;
    [SerializeField] float chanceToDuplicate;

    [Header("Crysatl insted of clone")]
    [SerializeField] private UI_SkillTreeSlot crystalInseadUnlockButton;
    [SerializeField] public bool crystalInstedOfClone;

    protected override void Start()
    {
        base.Start();
        cloneAttackUnlockButton.GetComponent<Button>().onClick.AddListener(UnlockCloneAttack);
        aggresiveCloneUnlockButton.GetComponent<Button>().onClick.AddListener(UnlockAggresiveClone);
        multipleUnlockButton.GetComponent<Button>().onClick.AddListener(UnlockMultiClone);
        crystalInseadUnlockButton.GetComponent<Button>().onClick.AddListener(UnlockCrystalInstead);
    }
    protected override void CheckUnlock()
    {
        UnlockCloneAttack();
        UnlockCrystalInstead();
        UnlockMultiClone();
        UnlockAggresiveClone();
    }

    #region Unlock Region
    private void UnlockCloneAttack()
    {
        if (cloneAttackUnlockButton.unLocked)
        {
            canAttack = true;
            attackMultiplier = cloneAttackMutiplier;
        }
    }

    private void UnlockAggresiveClone()
    {
        if (aggresiveCloneUnlockButton.unLocked)
        {
            canApplyOnHitEffect = true;
            attackMultiplier = aggresiveCloneAttackMutiplier;
        }
    }

    private void UnlockMultiClone()
    {
        if (multipleUnlockButton.unLocked)
        {
            canDuplicateClone = true;
            attackMultiplier = multicCloneAttackMultiplier;
        }

    }

    private void UnlockCrystalInstead()
    {
        if (crystalInseadUnlockButton.unLocked) 
        {
            crystalInstedOfClone = true;
        }
    }

    #endregion




    public GameObject FxPrefab => clonePrefab;

    public void CreateClone(Transform _clonePosition,Vector3 _offset)    {
        if (crystalInstedOfClone)
        {
            SkillManager.instance.crystal.CreateCrystal();
            SkillManager.instance.crystal.CurrentCrystalChooseRandomTarget();
            return;
        }


        GameObject newClone = Instantiate(clonePrefab);
        newClone.GetComponent<Clone_Skill_Controller>().SetupClone(_clonePosition,cloneDuration,canAttack,_offset,FindClosesEnemy(_clonePosition),canDuplicateClone,chanceToDuplicate,player,attackMultiplier );

        NetSkillFx.Broadcast(SkillFxKind.Clone, newClone.transform.position, _offset,
            cloneDuration, attackMultiplier, 0f);    }
    

    public void CreateCloneWithDelay(Transform _enemyTransform)
    {      
        StartCoroutine(CloneDelayCoroutine(_enemyTransform, new Vector3(2*player.facingDir, 0)));
    }

    private IEnumerator CloneDelayCoroutine(Transform _transform,Vector3 _offset)
    {
        yield return new WaitForSeconds(.2f);
        CreateClone(_transform, _offset);
    }

    

}
