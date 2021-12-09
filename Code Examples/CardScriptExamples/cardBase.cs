using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum cardType { Attack, Movement, Spell, Healing, Buff}
[System.Serializable]
public class cardBase : ScriptableObject
{

    public int costToUse;
    public new string name;
    [TextArea(1,2)]
    public string description;
    public cardType type;
    public Affinity element;
    public Sprite icon;
    public Sprite frame;
    public Sprite bannerSmall;
    public Sprite bannerDetailed;
    public Sprite BGtexture;
    protected Character character;

    public bool isCardCue = false;

    protected int apUsed = 1;

    public virtual Vector2Int getRange()
    {
        Debug.LogWarning("CARDBASE: getRange: Shouldn't 'ave come here.");
        return new Vector2Int(0,0);
    }

    public virtual int getCardPower(Character charRef)
    {
        character = charRef;
        int power = character.getPowerStat();
        return power;
    }

    public int getCost()
    {
        return costToUse;
    }

    public virtual void setAPUsed(int i)
    {
        apUsed = i;
    }

    public virtual void onSelected()
    {
        Debug.Log("CARDBASE: OnSelected");
    }

    public virtual void onDeselected()
    {
        Debug.Log("CARDBASE: Card " + name + "onDeselected");
        /*WorldMode.instance.changeWorldMode(wMode.NAVIGATEMENUS);
        FieldHighlight.instance.resetHighlighting();
        cursorControls.instance.setCursorActive(false);*/
    }

    public virtual List<Vector3Int> getDamagePositions()
    {
        Debug.LogWarning("CARDBASE: getDamagePositions: shouldn't be in the base.");
        return new List<Vector3Int>();
    }

    public virtual IEnumerator onUse(Character charRef, Vector3Int targetPos)
    {
        Debug.Log("BASE onUse: Before wait");
        character = charRef;
        yield return new WaitForSeconds(6);
        Debug.Log("BASE onUse: After wait");
    }

    public void assignToCharacter(Character whichChar)
    {
        character = whichChar;
    }

    protected List<Vector3Int> getAffectedPositions(List<Vector3Int> AtkPositionsRelToPlayer)
    {
        List<Vector3Int> globalGridPositions = new List<Vector3Int>();

        foreach(Vector3Int atkPos in AtkPositionsRelToPlayer)
        {
            globalGridPositions.Add(character.gridPos + atkPos);
        }

        return globalGridPositions;
    }

    public bool getIsCardCue()
    {
        return isCardCue;
    }
}
