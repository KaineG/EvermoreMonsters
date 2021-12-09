using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Cards/Spell")]
public class Spell : cardBase
{
    [SerializeField]
    private List<Vector3Int> dmgPositions = new List<Vector3Int>();
    [SerializeField]
    private Vector2Int range;//x = min, y = max
    [SerializeField]
    GameObject spellEffect;
    [SerializeField]
    private float spellMultiplyMod;

    [SerializeField]
    private statusEffect statEffect;
    [SerializeField]
    bool isStatusTargetSelf;

    //Spell effect here : Fireball, etc.

    public override Vector2Int getRange()
    {
        return range; 
    }

    //Spell damamge = PWR + (SPR * Modifier)
    public override int getCardPower(Character charRef)
    {
        character = charRef;
        return ((character.getPowerStat() + (int)character.getCumulativeStatIfAffected(affectedStat.PWR))
                    + (int)((character.getSpiritStat() + character.getCumulativeStatIfAffected(affectedStat.SPR))
                                * spellMultiplyMod));
    }

    public override void onSelected()
    {
        FieldHighlight.instance.getAttackArea(range);
    }

    public override void onDeselected()
    {
        base.onDeselected();
        Debug.Log("FIREBALL: Deselect");
    }

    public override List<Vector3Int> getDamagePositions()
    {
        return dmgPositions;
    }

    public override IEnumerator onUse(Character charRef, Vector3Int targetPos)
    {
        character = charRef;

        Debug.Log("FIREBALL: Movements:" + dmgPositions.Count);
        int cardPwr;

        //Subscribe to the action Tracker
        Overseer.instance.actionSubscribe(character.name, this.name);

        character.beginAction();

        //get the damage per position, adjusted by the character's base damamge
        cardPwr = getCardPower(charRef);

        //spawn the Effect object

        //rotate towards the attack direction and wait till it finishes rotating
        character.changeFacingDirection(targetPos);
        yield return new WaitWhile(character.getIsFlipping);

        //Start Character animation (char animation will cue: effect, hurt enemy, finishAction[sets IsActionInProcess = true]
        character.cueAnim(type, "Spell");
        if(spellEffect != null)
        {
            isCardCue = false;
            Instantiate(spellEffect, Overseer.instance.grid.CellToWorld(targetPos), Quaternion.identity);
        }
        else
        {
            Debug.LogError("No Spell Effect present");
            Overseer.instance.cueCardAttack();
        }

        //TEMPORARY calling the above functions manually
        //hurtEnemyAnim();
        //yield return new WaitForSeconds(1.5f);
        //character.finishAction();

        Debug.Log("FIREBALL: Before dmg cue");
        yield return new WaitUntil(getIsCardCue);
        Debug.Log("FIREBALL: After dmg cue");
        isCardCue = false;

        //damage any enemies hit
        Overseer.instance.attack(dmgPositions, targetPos, type, element, cardPwr);

        yield return new WaitForSeconds(0.25f);
        if (statEffect != null)
        {
            if (isStatusTargetSelf)
                Overseer.instance.causeStatusEffects(character.gridPos, dmgPositions, statEffect);
            else
                Overseer.instance.causeStatusEffects(targetPos, dmgPositions, statEffect);
        }
        
        yield return new WaitWhile(character.getIsActionInProcess);
        //yield return new WaitForSeconds(0.5f);
        Overseer.instance.actionUnSubscribe(character.name);
    }

}
    