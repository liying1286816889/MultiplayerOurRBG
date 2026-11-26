using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_SkillTreeSlot : MonoBehaviour, IPointerEnterHandler,IPointerExitHandler,ISaveManager
{

    private UI ui;
    private Image skillImage;


    [SerializeField] private int skillCost;
    [SerializeField] string skillName;
    [TextArea]
    [SerializeField] string skillDescription;
    [SerializeField] Color lockedSkillColor;
    [SerializeField] Color UnlockedSkillColor;



    public bool unLocked;
    [SerializeField] private UI_SkillTreeSlot[] shouldBeUnLocked;
    [SerializeField] private UI_SkillTreeSlot[] shouldBeLocked;
    

    private void OnValidate()
    {
        gameObject.name="SkillTreeSlot_UI -"+skillName;
    }

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => UnLockSkillSlot());
    }


    private void Start()
    {
        skillImage = GetComponent<Image>();

        ui = GetComponentInParent<UI>();
        skillImage.color = lockedSkillColor;

        if (unLocked)
        {
            skillImage.color = Color.white;
        }

    }

    public void UnLockSkillSlot()
    {
        for (int i = 0; i < shouldBeUnLocked.Length; i++)
        {
            if (shouldBeUnLocked[i].unLocked == false)
            {
                return;
            }
            
        }

        for (int i = 0; i < shouldBeLocked.Length; i++)
        {
            if (shouldBeLocked[i].unLocked == true)
            {
                return;
            }

        }

        if (unLocked == false)
        {
            if (PlayerManager.instance.HaveEnoughMoney(skillCost) == false)
            {
                return;
            }
        }
        
        unLocked = true;
        skillImage.color = UnlockedSkillColor;
    }




    public void OnPointerEnter(PointerEventData eventData)
    {
        ui.skillToolTip.ShowToolTip(skillDescription, skillName,skillCost.ToString());
    }

    

    public void OnPointerExit(PointerEventData eventData)
    {
        ui.skillToolTip.HideToolTip();    
    }

    public void LoadData(GameData _data)
    {
        if (_data.skillTree.TryGetValue(skillName,out bool value))
        {
            unLocked = value;
        }


    }

    public void SaveData(ref GameData _data)
    {
        if(_data.skillTree.TryGetValue(skillName,out bool value))
        {
            _data.skillTree.Remove(skillName);
            _data.skillTree.Add(skillName, unLocked);
        }
        else
        {
            _data.skillTree.Add(skillName, unLocked); 
        }


    }
}
