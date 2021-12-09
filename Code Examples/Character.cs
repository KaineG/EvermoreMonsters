using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum flipState { NOTFLIPPING, FLIPPING, GOLEFT, GORIGHT,}
public enum facingDir { NONE, UR, DR, DL, UL}

public class Character : MonoBehaviour
{
    public Grid grid;

    [SerializeField]
    playerUI myUI;//Used for players
    fieldStatusUI fieldUI;
    //EnemyUI myEnemyUI;//Used for enemies
    [SerializeField]
    Vector3 fieldHealthbarOffset;//the height of the field healthbar above a characters head.
    
    [SerializeField]
    CharacterStats charStats;

    [SerializeField]
    bool isPlayerCharacter;

    bool isActionInProcess = false;
    bool isPushed = false;
    bool isAlreadyDead = false;//prevents playing death animations twice

    arcSelection curArcSelection;
    
    [SerializeField]
    baseAttack atkAction;
    baseMove moveAction;
    List<Vector3Int> curPath = new List<Vector3Int>();

    //The full list of cards in this characters deck. DO NOT MODIFY
    [SerializeField]
    private List<cardBase> cards = new List<cardBase>();
    
    //The cards remaining in a characters deck during combat. Gets reset when combat begins
    [SerializeField]
    private List<cardBase> unusedCards = new List<cardBase>();
    private List<cardBase> curHandCards = new List<cardBase>();
    private List<cardBase> discardPile = new List<cardBase>();

    //current status effects
    private List<statusEffect> curStatusEffects = new List<statusEffect>();

    //movement and positioning
    public Vector3Int gridPos = new Vector3Int();
    public Vector3Int newPos = new Vector3Int();
    public Vector3Int sourcePos = new Vector3Int();//This is what player uses to select where to attack

    public Vector3 offset;
    public float moveSpeed;

    Vector2 dirInput;

    //anim/skeleton/sprite related
    Animator charAnim;
    flipState flipState;
    public facingDir facingDir;
    [SerializeField]
    GameObject charSpriteParent;
    [SerializeField]
    GameObject frontSpriteParent;
    [SerializeField]
    GameObject backSpriteParent;

    [SerializeField]
    private Sprite charToken; //the sprite used for the UI Token image, NOT the actual character sprite.

    //ap used this turn per action
    int moveAPUsed = 0;
    int atkAPUsed = 0;
    int APusedThisTurn = 0;

    public void destroyEnemyChar()
    {
        if (!isPlayerCharacter
            && this.name != "Aster"
            && this.name != "Orfus")
        {
            Debug.Log(" DESTROYING " + this.name);
            Destroy(this.gameObject);
        }
    }

    public void resetOpacity()
    {
    }

    public void Start()
    {
        moveAction = cardBase.CreateInstance<baseMove>();
        moveAction.assignToCharacter(this);

        newPos = gridPos;

        charAnim = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        if (charSpriteParent != null)
        {
            Quaternion rotationDest;
            switch (flipState)
            {
                case flipState.NOTFLIPPING:
                    break;
                case flipState.FLIPPING:
                    rotationDest = Quaternion.Euler(0, 90, 0);
                    charSpriteParent.transform.rotation = Quaternion.RotateTowards(charSpriteParent.transform.rotation,
                                                        rotationDest,
                                                        1080 * Time.deltaTime);
                    if (charSpriteParent.transform.rotation == rotationDest)
                    {
                        switch (facingDir)
                        {
                            case facingDir.NONE:
                                Debug.LogError("NONE DIRECTION");
                                break;
                            case facingDir.UR:
                                frontSpriteParent.SetActive(false);
                                backSpriteParent.SetActive(true);
                                flipState = flipState.GORIGHT;
                                break;
                            case facingDir.DR:
                                frontSpriteParent.SetActive(true);
                                backSpriteParent.SetActive(false);
                                flipState = flipState.GORIGHT;
                                break;
                            case facingDir.DL:
                                frontSpriteParent.SetActive(true);
                                backSpriteParent.SetActive(false);
                                flipState = flipState.GOLEFT;
                                break;
                            case facingDir.UL:
                                frontSpriteParent.SetActive(false);
                                backSpriteParent.SetActive(true);
                                flipState = flipState.GOLEFT;
                                break;
                        }
                    }
                    break;
                case flipState.GOLEFT:
                    rotationDest = Quaternion.Euler(0, 0, 0);
                    charSpriteParent.transform.rotation = Quaternion.RotateTowards(charSpriteParent.transform.rotation,
                                                        rotationDest,
                                                        1080 * Time.deltaTime);
                    if (charSpriteParent.transform.rotation == rotationDest)
                        flipState = flipState.NOTFLIPPING;
                    break;

                case flipState.GORIGHT:
                    rotationDest = Quaternion.Euler(0, 180, 0);
                    charSpriteParent.transform.rotation = Quaternion.RotateTowards(charSpriteParent.transform.rotation,
                                                        rotationDest,
                                                        1080 * Time.deltaTime);
                    if (charSpriteParent.transform.rotation == rotationDest)
                        flipState = flipState.NOTFLIPPING;
                    break;
            }
        }
    }

    void FixedUpdate()
    {
        /*
        if (isFadingIn)
        {
            Color curColor = frontSpriteParent.GetComponent<SpriteRenderer>().color;
            float mdf;
            if (fadeDestColor == new Color(0, 0, 0, 0))
                mdf = -.00001f;
            else
                mdf = .00001f;
            frontSpriteParent.GetComponent<SpriteRenderer>().color = curColor + new Color(mdf * Time.deltaTime,
                                                                                          mdf * Time.deltaTime,
                                                                                          mdf * Time.deltaTime,
                                                                                          mdf * Time.deltaTime);
            backSpriteParent.GetComponent<SpriteRenderer>().color = curColor + new Color(mdf * Time.deltaTime,
                                                                                          mdf * Time.deltaTime,
                                                                                          mdf * Time.deltaTime,
                                                                                          mdf * Time.deltaTime);

            if (frontSpriteParent.GetComponent<SpriteRenderer>().color == fadeDestColor)
            {
                isFadingIn = false;
                if (fadeDestColor == new Color(0,0,0,0))
                {
                    frontSpriteParent.gameObject.SetActive(false);
                    backSpriteParent.gameObject.SetActive(false);
                }
            }

        }*/

        if (newPos != gridPos)
        {
            if (isPushed)//doesn't rotate or use a running animation
            {
                Vector3 destPos = grid.CellToWorld(newPos) + offset;
                transform.position = Vector3.MoveTowards(transform.position, destPos, 3f * Time.deltaTime);
                if(transform.position == destPos)
                {
                    Debug.Log("Push Destination Reached");
                    grid.GetComponent<TileInfo>().setIsOccupied(gridPos, false);
                    gridPos = newPos;
                    grid.GetComponent<TileInfo>().setIsOccupied(gridPos, true);
                    isPushed = false;
                    charAnim.SetBool("isMoving",false);
                }
            }
            else
            {
                Vector3 destPos = grid.CellToWorld(newPos) + offset;
                transform.position = Vector3.MoveTowards(transform.position, destPos, moveSpeed * Time.deltaTime);
                if (transform.position == destPos)
                {
                    Debug.Log("Reached Destination");
                    grid.GetComponent<TileInfo>().setIsOccupied(gridPos, false);
                    gridPos = newPos;
                    grid.GetComponent<TileInfo>().setIsOccupied(gridPos, true);
                    //isActionInProcess = false;
                }
            }
        }
        if (charSpriteParent.gameObject.activeInHierarchy)
        {
            if (WorldMode.instance.mode != wMode.FREEROAM
                && !getIsActionInProcess()
                && charAnim.GetBool("isMoving"))
                charAnim.SetBool("isMoving", false);
        }
    }

    #region GETfns
    public bool getIsActionInProcess()
    {
        return isActionInProcess;
    }

    public bool getIsPlayerCharacter()
    {
        return isPlayerCharacter;
    }

    public bool getIsFlipping()
    {
        if (flipState == flipState.NOTFLIPPING)
            return false;
        return true;
    }

    public bool getIsPushed()
    {
        return isPushed;
    }
    public bool getIsAlive()
    {
        if (charStats.curHealth == 0)
            return false;
        return true;
    }

    public bool getIsAlreadyDead()
    {
        return isAlreadyDead;
    }

    public int getHealth()
    {
        return charStats.curHealth;
    }

    public int getMaxHealth()
    {
        return charStats.maxHealth;
    }
    
    public int getCurAP()
    {
        return charStats.curAP;
    }

    public int getBonusAP()
    {
        return charStats.bonusAP;
    }

    public int getMaxAP()
    {
        return charStats.maxAP;
    }

    public int getMoveAPUsed()
    {
        return moveAPUsed;
    }

    public int getAtkAPUsed()
    {
        return atkAPUsed;
    }

    public int getCurDeckSize()
    {
        return unusedCards.Count;
    }

    public int getFullDeckSize()
    {
        return cards.Count;
    }

    public int getCurDiscardSize()
    {
        return discardPile.Count;
    }

    public int getBaseMoveRange()
    {
        return charStats.baseMoveRange;
    }

    public int getPowerStat()
    {
        return charStats.power;
    }

    public int getDefStat()
    {
        return charStats.defense;
    }

    public int getResistanceStat()
    {
        return charStats.resistance;
    }

    public int getSpiritStat()
    {
        return charStats.spirit;
    }

    public int getSpeedStat()
    {
        return charStats.tickSpeed;
    }
    public int getAPUsedThisTurn()
    {
        return APusedThisTurn;
    }

    public List<cardBase> getDeck()
    {
        return cards;
    }

    public List<cardBase> getCurHandCards()
    {
        return curHandCards;
    }

    public cardBase getEnemyUICard()
    {
        for (int i = 0; i < curHandCards.Count; i++)
        {
            if (curHandCards[i].costToUse <= getCurAP())
            {
                return curHandCards[i];
            }
        }
        
        return null;
    }
    public void enemySelectButton(cardBase card)
    {
        for(int i = 0; i < curHandCards.Count; i++)
        {
            if (curHandCards[i] == card)
            {
                myUI.enemySelectButton(i);
                return;
            }
        }
    }
    public baseMove getBaseMovement()
    {
        return moveAction;
    }

    public baseAttack getBaseAttack()
    {
        return atkAction;
    }

    public Sprite getToken()
    {
        return charToken;
    }

    public Vector3 getFieldHealthbarOffset()
    {
        return fieldHealthbarOffset;
    }

    public Animator GetAnimator()
    {
        return charAnim;
    }

    public playerUI getUI()
    {
        return myUI;
    }
    #endregion

    #region setFunctions
    public void setIsPlayerCharacter(bool truth)
    {
        isPlayerCharacter = truth;
    }

    public void loadDeck(List<cardBase> deck)
    {
        cards = deck;
    }

    public void setCharUI(playerUI ui)
    {
        myUI = ui;
        myUI.updateCharacterUI(this);
    }

    public void setFieldUI(fieldStatusUI ui)
    {
        fieldUI = ui;
    }

    public void setAnim()
    {
        charAnim = GetComponentInChildren<Animator>();
    }
    public void useAP(int howMuch, arcSelection actionType)
    {
        APusedThisTurn += howMuch;

        if (actionType == arcSelection.Move)
            moveAPUsed += howMuch;
        if (actionType == arcSelection.Attack)
            atkAPUsed += howMuch;
        charStats.useAP(howMuch);

        if (myUI!= null)
            myUI.resetAP(charStats.curAP, charStats.bonusAP, charStats.maxAP);
    }

    public void turnOffAnim()
    {
        charAnim.enabled = false;
    }

    public void turnOnOffSpriteParent(bool truth)
    {
        charSpriteParent.gameObject.SetActive(truth);
    }

    public void cueAnim(cardType type, string animName)
    {
        if (animName == "dodge")
        {
            Debug.Log("Playing " + this.name + " dodge Anim");
            charAnim.SetTrigger(animName);
        }
        else if (animName == "Death")
        {
            Debug.Log("Playing " + this.name + " death Anim");
            charAnim.SetTrigger(animName);
            isAlreadyDead = true;
        }
        else
        {
            switch (type)
            {
                case cardType.Attack:
                    Debug.Log("Playing " + this.name + " Attack Anim");
                    charAnim.SetTrigger(animName);
                    break;
                case cardType.Spell:
                    Debug.Log("Playing " + this.name + " spell Anim");
                    charAnim.SetTrigger(animName);
                    break;
                case cardType.Movement:
                    Debug.Log("Playing " + this.name + " movement Anim");
                    charAnim.SetBool("isMoving", true);
                    break;
                case cardType.Healing:
                    Debug.Log("Playing " + this.name + " Heal Anim");
                    charAnim.SetTrigger(animName);
                    break;
                case cardType.Buff:
                    Debug.Log("Playing " + this.name + " buff Anim");
                    charAnim.SetTrigger(animName);
                    break;
            }
        }
    }
    #endregion

    #region turnFunctions
    public void resetDeck()
    {
        //reset the deck
        unusedCards.Clear();
        foreach (cardBase card in cards)
            unusedCards.Add(card);
        curHandCards.Clear();
        if (myUI != null)
            myUI.resetCards(curHandCards); //ensures no leftover cardobjects/cardBase from previous battles

        charStats.resetAP();
    }
    public void beginTurn()
    {
        //loop through all stat buffs, debuffs, incrementing their lifespan
        deprecateStatusEffects();
        Debug.Log("Character: [" + this.name + "] beginTurn: Loop through all stat buffs");

        //no actions yet taken this turn
        moveAPUsed = 0;
        atkAPUsed = 0;
        APusedThisTurn = 0;
        charStats.addAPperTurn();

        /* //reset the deck
         unusedCards.Clear();
         foreach (cardBase card in cards)
             unusedCards.Add(card);
         curHandCards.Clear();
         if (myUI != null)
             myUI.resetCards(curHandCards); //ensures no leftover cardobjects/cardBase from previous battles
         //else
             //  Debug.Log("Character: ENEMYUI reset cards");
         discardPile.Clear();*/

        if (unusedCards.Count == 0)
            ShuffleDeck();

        //Draw until 5 cards are in the hand, or the deck is empty
        for (int i = 0; i < 5; i++)
        {
            if (unusedCards.Count > 0)
                drawCard(false);
        }
                
        if (myUI!=null)
        {
            myUI.resetAP(getCurAP(), getBonusAP(), getMaxAP());
            myUI.updateDeckAndDiscardSize(unusedCards.Count, discardPile.Count);
        }
        
        fieldUI.updateFieldStatusInfo();
    }

    public void endTurn()
    {
        //deprecateStatusEffects();
        myUI = null;
    }

    //ends combat
    public void endCombat()
    {

        charStats.curHealth = charStats.maxHealth;
        charAnim.enabled = true;
        isAlreadyDead = false;

        APusedThisTurn = 0;
        moveAPUsed = 0;
        atkAPUsed = 0;
        myUI = null;

        deprecateStatusEffects();
    }

    public void updateUI()
    {
        if (myUI!=null)
        {
            myUI.resetCards(curHandCards);
            myUI.resetAP(getCurAP(), getBonusAP(), getMaxAP());
            myUI.updateHealth(getHealth(), getMaxHealth());
        }
        fieldUI.updateFieldStatusInfo();
    }

    public void drawCard(bool addMoveDelay)
    {
        if (unusedCards.Count > 0) 
        { 
            int whichCard = (int)Random.Range(0, unusedCards.Count - 0.1f); //so max possible itr is 1 less than count size.
            if (myUI != null)
            {
                if (myUI.addCard(unusedCards[whichCard], addMoveDelay))
                {
                    unusedCards.Remove(unusedCards[whichCard]);
                }
            }
            /*else//enemy char
            {
                if (curHandCards.Count < 5)
                {
                    curHandCards.Add(unusedCards[whichCard]);
                    unusedCards.Remove(unusedCards[whichCard]);
                }
            }*/
        }

        fieldUI.updateFieldStatusInfo();
    }

    //called when a card is used. removes the used card from UI and character hand. updates deck sizes.
    public void discard()
    {
        if (myUI!=null)
        {
            cardBase card = myUI.discardUsedCard();
            //curHandCards.Remove(card);
            discardPile.Add(card);
            myUI.updateDeckAndDiscardSize(unusedCards.Count, discardPile.Count);
        }
        fieldUI.updateFieldStatusInfo();
    }

    public void enemyDiscard(cardBase card)
    {
        if (!isPlayerCharacter)
        {
            discardPile.Add(card);
            curHandCards.Remove(card);
            fieldUI.updateFieldStatusInfo();
            if (myUI != null)
                myUI.updateDeckAndDiscardSize(unusedCards.Count, discardPile.Count);
        }
    }

    public void ShuffleDeck()
    {
        foreach (cardBase card in discardPile)
        {
            unusedCards.Add(card);
        }
        discardPile.Clear();
    }
    #endregion

    #region movement
    public void move(Vector3Int changeInPos)
    {
        Debug.Log("Changing new Pos");

        newPos = gridPos + changeInPos;

        changeFacingDirection(newPos);
        charAnim.SetBool("isMoving",true);
    }

    public IEnumerator freeMoveTo(Vector3Int destPos, Vector3Int curWorldToGridPos)
    {
        if (destPos != curWorldToGridPos)
        {
            if (charAnim == null)
                charAnim = GetComponentInChildren<Animator>();

            if (!isPlayerCharacter)
                Debug.Log("ENEMY FREE MOVE");
            Overseer.instance.actionSubscribe(this.name, "FreeMove");
            beginAction();
            grid.GetComponent<TileInfo>().setIsOccupied(gridPos, false);
            gridPos = curWorldToGridPos;
            newPos = destPos;
            changeFacingDirection(newPos);
            charAnim.SetBool("isMoving", true);

            //Debug.Log("Character: " + name + " grid: " + gridPos + " newPos: " + newPos); 
            yield return new WaitUntil(hasReachedDestination);
            finishAction();
            Overseer.instance.actionUnSubscribe(this.name);
        }
    }

    public IEnumerator freeMoveTo(Vector3Int changeInPos)
    {
        if (charAnim == null)
            charAnim = GetComponentInChildren<Animator>();

        if (!isPlayerCharacter)
            Debug.Log("ENEMY FREE MOVE");
        Overseer.instance.actionSubscribe(this.name, "FreeMove");
        beginAction();
        grid.GetComponent<TileInfo>().setIsOccupied(gridPos, false);
        gridPos = grid.WorldToCell(transform.position);
        newPos = gridPos + changeInPos;
        changeFacingDirection(newPos);
        charAnim.SetBool("isMoving", true);

        //Debug.Log("Character: " + name + " grid: " + gridPos + " newPos: " + newPos); 
        yield return new WaitUntil(hasReachedDestination);
        finishAction();
        Overseer.instance.actionUnSubscribe(this.name);
    }
    //pushing a character along x or y axis towards destination.
    //stops pushing if character hits an obstacle or reaches destination
    public IEnumerator push(Vector3Int pushDest)
    {
        Debug.Log("CHARACTER: push: gridPos: " + gridPos + " pushDest: " + pushDest);
        //verify the push is not diagonal
        if (pushDest.x == gridPos.x || pushDest.y == gridPos.y)
        {
            Overseer.instance.actionSubscribe(this.name + "PushMove", "PushMove");
            beginAction();
            Vector3Int unitPushDest = pushDest - gridPos;
            if (unitPushDest.x != 0)
                unitPushDest.x = unitPushDest.x / Mathf.Abs(unitPushDest.x);
            if (unitPushDest.y != 0)
                unitPushDest.y = unitPushDest.y / Mathf.Abs(unitPushDest.y);

            Vector3Int tempPos = gridPos;
            //get furthest push pos towards pushDest
            while (tempPos != pushDest)
            {
                tempPos += unitPushDest;
                //if this pos is NOT occupied
                if (!grid.GetComponent<TileInfo>().getIsOccupied(tempPos, newPos))
                {
                    newPos = tempPos;
                }
                else
                    break;
            }

            //character is pushed
            if (newPos != gridPos)
            {
                isPushed = true;
            }
            yield return new WaitWhile(getIsPushed);

            Overseer.instance.actionUnSubscribe(this.name + "PushMove");
        }
        else
            Debug.Log("CHARACTER: push: gridPos: " + gridPos + " pushDest: " + pushDest);
    }

    public void changeFacingDirection(facingDir dir)
    {
        if (dir != facingDir)
        {
            flipState = flipState.FLIPPING;
        }
        facingDir = dir;
    }

    public void changeFacingDirection(Vector3Int facingPos)
    {
        facingDir newFacingDir = facingDir.NONE;
        if (facingPos != gridPos)
        {
            if (facingPos.x < gridPos.x && facingPos.y >= gridPos.y)
            {
                //UL > DL || |x| < |y|
                if (Mathf.Abs(facingPos.y - gridPos.y) > Mathf.Abs(facingPos.x - gridPos.x))
                    newFacingDir = facingDir.UL;
                else
                    newFacingDir = facingDir.DL;
            }
            if (facingPos.x > gridPos.x && facingPos.y <= gridPos.y)
            {
                //DR > UR |x| < |y|
                if (Mathf.Abs(facingPos.x - gridPos.x) < Mathf.Abs(facingPos.y - gridPos.y))
                    newFacingDir = facingDir.DR;
                else
                    newFacingDir = facingDir.UR;
            }
            if (facingPos.y < gridPos.y && facingPos.x <= gridPos.x)
            {
                //DL > DR || |x| > |y|
                if (Mathf.Abs(facingPos.x - gridPos.x) > Mathf.Abs(facingPos.y - gridPos.y))
                    newFacingDir = facingDir.DL;
                else
                    newFacingDir = facingDir.DR;
            }
            if (facingPos.y > gridPos.y && facingPos.x >= gridPos.x)
            {
                //UR > UL || |x| > |y|
                if (Mathf.Abs(facingPos.y - gridPos.y) < Mathf.Abs(facingPos.x - gridPos.x))
                    newFacingDir = facingDir.UR;
                else
                    newFacingDir = facingDir.UL;
            }

            if (newFacingDir != facingDir)
            {
                Debug.Log("Old FAcing: " + facingDir + " NEW: " + newFacingDir);
                flipState = flipState.FLIPPING;
            }
            if (newFacingDir != facingDir.NONE)
                facingDir = newFacingDir;
        }
    }

    public void changeFacingDirection(Vector3 facingPos)
    {
        facingDir newFacingDir = facingDir.NONE;
        if (facingPos != transform.position)
        {
            if (facingPos.x <= transform.position.x)
            {
                if (facingPos.y < transform.position.y)
                    newFacingDir = facingDir.DL;
                else
                    newFacingDir = facingDir.UL;
            }
            else
            {
                if (facingPos.y < transform.position.y)
                    newFacingDir = facingDir.DR;
                else
                    newFacingDir = facingDir.UR;
            }
        
            if (newFacingDir != facingDir)
            {
                Debug.Log("Old FAcing: " + facingDir + " NEW: " + newFacingDir);
                flipState = flipState.FLIPPING;
            }
            if (newFacingDir != facingDir.NONE)
                facingDir = newFacingDir;
        }
    }

    //verifies if newPos == worldPos. Movement cards use this to verify each step on the grid is complete
    public bool hasReachedDestination()
    {
        bool truth = false;
        if (newPos == gridPos)
            truth = true;
        return truth;
    }
    #endregion

    #region attack
    //called at the beginning of a card's onUse function
    public void beginAction()
    {
        isActionInProcess = true;
    }

    //called by the attack effect at the end of its animation to indicate the attack is over
    public void finishAction()
    {
        isActionInProcess = false;
    }

    public void takeAttack(int cardPwr, cardType type, Affinity element)
    {
        bool isDead = false;
        Debug.Log("CHARACTER: " + this.name + " takeAttack: cardPwr: " + cardPwr + " Type: " + type + " element: " + element);
        if (type == cardType.Spell || !doesCharacterDodge())
        {
            //play the hurt animation
            charAnim.SetTrigger("isHurt");

            //modify health stat, returns 'true' if character is dead
            int modifiedDmg = modifyIncomingDmg(cardPwr, type, element);
            isDead = charStats.takeDamage(modifiedDmg);
            fieldText.instance.createDmgPopup(this, modifiedDmg.ToString(), Color.red);

            //update UI
            if (myUI != null)
                myUI.updateHealth(charStats.curHealth, charStats.maxHealth);

            fieldUI.updateFieldStatusInfo();
        }
        else
        {
            fieldText.instance.createDmgPopup(this,
                                                "Dodge", Color.white);

            cueAnim(type, "dodge");
            //Character dodges
            //charAnim.SetTrigger("dodge");
            //Show floating "dodge" instead of damage numbers
            Debug.Log("Character " + this.name + " Dodged an attack.");
        }
    }

    //calculates whether a character dodges a physical attack
    bool doesCharacterDodge()
    {
        int dodge = (int)(100 * Random.value);
        Debug.Log("CHARACTER: Dodge num: " + dodge + " char Dex: " + (charStats.dexterity * 5));
        if (dodge < ((charStats.dexterity + (int)getCumulativeStatIfAffected(affectedStat.DEX)) * 5))
            return true;
        else
            return false;
        
    }

    int modifyIncomingDmg(int cardPwr, cardType type, Affinity element)
    {
        int dmgTaken;
        int statEffectModifier = 0;

        int cardPwrAfterShield = cardPwr;
        //cardPWR reduced by any existing shield amount before being reduced by def/res
        cardPwrAfterShield = useShieldEffect(cardPwr);

        if (type == cardType.Attack)
        {
            statEffectModifier = (int)getCumulativeStatIfAffected(affectedStat.DEF);

            if (statEffectModifier < 0
                && Mathf.Abs(statEffectModifier) > charStats.defense)
                statEffectModifier = -charStats.defense;

            //ensure that -/+ effect modifier doesn't make dmg heal, or heals dmg
            if (charStats.defense + statEffectModifier > cardPwrAfterShield)
                dmgTaken = 0;
            else
                dmgTaken = cardPwrAfterShield - charStats.defense - statEffectModifier;
        }
        else if (type == cardType.Spell)
        {
            statEffectModifier = (int)getCumulativeStatIfAffected(affectedStat.RES);

            if (statEffectModifier < 0
                && Mathf.Abs(statEffectModifier) > charStats.resistance)
                statEffectModifier = -charStats.resistance;

            if (charStats.resistance + statEffectModifier > cardPwrAfterShield)
                dmgTaken = 0;
            else
                dmgTaken = cardPwrAfterShield - charStats.resistance - statEffectModifier;
        }
        else
        {
            dmgTaken = 0;
            Debug.LogError("Trying to Modify damage taken for a non-attack/spell card");
        }
        return dmgTaken;
    }
    #endregion

    #region statusEffects
    public void addStatusEffect(statusEffect effect)
    {
        statusEffect newEffect = new statusEffect();
        newEffect.effectName = effect.effectName;
        newEffect.effectTrigger = effect.effectTrigger;
        newEffect.affectedStat = effect.affectedStat;
        newEffect.effectSize = effect.effectSize;
        newEffect.turnDuration = effect.turnDuration;
        newEffect.statusElement = effect.statusElement;

        curStatusEffects.Add(newEffect);

        if (curStatusEffects[curStatusEffects.Count-1].effectTrigger == statusTrigger.onTurn)
        {
            //AP and HP increases to actual character stats
            switch(curStatusEffects[curStatusEffects.Count -1].affectedStat)
            {
                case affectedStat.onTurnDmg:
                    takeAttack(curStatusEffects[curStatusEffects.Count - 1].onTurnDmg(),
                               cardType.Spell,
                               curStatusEffects[curStatusEffects.Count - 1].getEffectElement());
                    fieldText.instance.createDmgPopup(this,
                        curStatusEffects[curStatusEffects.Count - 1].effectSize.ToString(),
                        Color.red);
                    break;
                case affectedStat.AP:
                    charStats.addBonusAP(curStatusEffects[curStatusEffects.Count - 1].effectSize);
                    if (myUI != null)
                        myUI.resetAP(charStats.curAP, charStats.bonusAP, charStats.maxAP);
                    fieldText.instance.createDmgPopup(this,
                        "AP +" + curStatusEffects[curStatusEffects.Count - 1].effectSize.ToString(),
                        Color.blue);
                    break;
                case affectedStat.HP:
                    charStats.heal(curStatusEffects[curStatusEffects.Count - 1].effectSize);
                    if (myUI != null)
                        myUI.updateHealth(getHealth(), getMaxHealth());
                    fieldUI.updateFieldStatusInfo();
                    fieldText.instance.createDmgPopup(this,
                        curStatusEffects[curStatusEffects.Count - 1].effectSize.ToString(),
                        Color.blue);
                    break;
            }

            if (curStatusEffects[curStatusEffects.Count - 1].turnDuration == 0)
                curStatusEffects.RemoveAt(curStatusEffects.Count - 1);
        }
        else
        {
            Color whichColor;
            if (newEffect.effectSize > 0)
                whichColor = Color.blue;
            else
                whichColor = Color.red;

            string statusText = newEffect.affectedStat.ToString();
            if (newEffect.effectSize > 0)
                statusText += " + ";
            statusText += newEffect.effectSize.ToString();

            fieldText.instance.createDmgPopup(this,
                    statusText,
                    whichColor);
        }

        
    }

    //reduces the duration of existing status effects and gets rid of those with duration <= 0
    public void deprecateStatusEffects()
    {
        for (int i = 0; i < curStatusEffects.Count; i++)
        {
            if (curStatusEffects[i].reduceEffectDuration(this.name))
            {
                fieldText.instance.createDmgPopup(this,
                    "removed " + curStatusEffects[i].affectedStat.ToString() + " + " + curStatusEffects[i].effectSize.ToString(),
                    Color.white);
                Debug.Log("From " + this.name + " Removing status effect: " + curStatusEffects[i].effectName);
                curStatusEffects.RemoveAt(i);
                i--;
                if (i < 0)
                    i = 0;
            }
        }
    }

    public void triggerStatusEffectsOnTurn()
    {
        List<affectedStat> affectedStats = new List<affectedStat>();
        List<int> allEffectsSize = new List<int>();

        foreach(statusEffect statEffect in curStatusEffects)
        {
            if (statEffect.effectTrigger == statusTrigger.onTurn)
            {
                //get onTurnDmg of current status effect and take that damage
                if (statEffect.onTurnDmg() > 0)
                {
                    takeAttack(statEffect.onTurnDmg(), cardType.Spell, statEffect.getEffectElement());
                }
            }
        }
    }

    public float getCumulativeStatIfAffected(affectedStat stat)
    {
        float affectedStatSum = 0;

        foreach (statusEffect statEffect in curStatusEffects)
        {
            affectedStatSum += statEffect.getIfStatAffected(stat);    
        }
        Debug.Log("CHARACTER: " + this.name + " getting stat: " + stat + " Size: " + affectedStatSum); 
        return affectedStatSum;
    }


    //come back to this later
    public int useShieldEffect(int cardPwr)
    {
        int shieldReducedDmg = cardPwr;

        foreach(statusEffect statEffect in curStatusEffects)
        {
            if (statEffect.affectedStat == affectedStat.Shield)
                //reduce shield amount, alll that jazz
                continue;
        }

        return cardPwr;
    }
    #endregion

    public void gainNewCard(cardBase card)
    {
        Debug.Log("CHARACTER: gainNewCard: adding Card to deck.");
        Debug.Log("Deck Size before: " + cards.Count);
        cards.Add(card);
        Debug.Log("Deck Size after:  " + cards.Count);
    }
}

