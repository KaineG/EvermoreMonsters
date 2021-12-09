using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.EventSystems;

public enum arcSelection { EndTurn, Attack, Move, Cards }

/*
 *  This class is the combat manager, holding references to all characters,
 *      deciding turn order, starting character turns, handling damage etc between 
 *      characters
 */
public class Overseer : MonoBehaviour
{
    #region Singleton
    public static Overseer instance;

    void Awake()
    {
        if (instance != null)
        {
            Debug.LogWarning("More than once instance of Overseer found.");
            return;
        }
        instance = this;
    }
    #endregion

    [SerializeField]
    public Grid grid;
    PlayerControls controls;
    [SerializeField]
    List<playerUI> playerUIs;
    int uiOffset = 0;//For rotating the UI elements between characters
    [SerializeField]
    public Animator anim;

    //characters invlolved in combat
    public List<Character> playerCharacters;
    public List<GameObject> enemies;
    public List<Character> turnOrder;
    public int roundNumber;
    BattleScript curBattleScript;

    int curCharTurnItr;
    Vector2 dirInput;//controller directional input.

    //Field Healthbars
    [SerializeField]
    private GameObject fieldStatusUIPrefab;
    [SerializeField]
    private GameObject fieldStatusUIParent;
    public List<GameObject> enemyFieldUIs = new List<GameObject>();
    private List<GameObject> playerFieldUIs = new List<GameObject>();

    int curAPCost = 0;
    int curAPPwr = 0;

    //Field Actions: Movement, Attack, Card ability
    baseMove moveAction;
    cardBase cardAction;

    arcSelection curArcSelection;

    [SerializeField]
    APStrengthGague apGague;

    [SerializeField]
    GameObject SwapHeroIndicator;

    [SerializeField]
    public GameObject environmentalCollision;

    #region actionTracker
    /// <summary>
    /// The action Tracker functions as a tracker of all the "Action"s that are ongoing.
    /// When a character performs an action, such as a movement, basic attack, or uses a card, 
    ///     the Overseer needs to keep track of all the associated animations and translations, 
    ///     so that the Overseer can wait until all the animations are complete before letting the player
    ///     resume choosing their next move.
    /// Each entry to the actionSubscriber will contain two strings:
    ///     * string nameOfGameObject (i.e., the Character performing the action, the spell Effect object, etc.)
    ///     * string nameOfAction (i.e., baseMove, baseAttack, the used card name)
    /// </summary>
    Dictionary<string, string> actionTracker = new Dictionary<string, string>();

    //adds an action's source info to the actionSubscriber dictionary. 
    //  * string nameOfGameObject (i.e., the Character performing the action, the spell Effect object, etc.)
    //  * string nameOfAction (i.e., baseMove, baseAttack, the used card name)
    public void actionSubscribe(string nameGameObject, string actionName)
    {
        try
        {
            Debug.Log("OVERSEER: actionSubscribe: adding subscription: ("
                + nameGameObject + "," + actionName + ")");
            actionTracker.Add(nameGameObject, actionName);
        }
        catch
        {
            Debug.LogError("OVERSEER: actionSubscribe: Failed to add ("
                + nameGameObject + "," + actionName + ") to the actionSubscriber.");
            Debug.LogError("OVERSEER: ActionTracker KEYS: " + actionTracker.Keys.Count + ":KeyValues: " + actionTracker.Values.Count);

        }
    }

    //when an action is complete, this function removes that action from the tracker
    public void actionUnSubscribe(string nameGameObject)
    {
        if (actionTracker.ContainsKey(nameGameObject))
        {
            actionTracker.Remove(nameGameObject);
            Debug.Log("OVERSEER: actionUnSubscribe: Removing key: " + nameGameObject + " Remaining Keys: " + actionTracker.Count);
            List<KeyValuePair<string, string>> temp = actionTracker.ToList();

            int i = 0;
            foreach (KeyValuePair<string,string> entry in temp)
            {
                Debug.Log("CUR SUBSCRIPTIONS: [" + i + "]: " + entry.Key);
            }
        }
        else
        {
            Debug.LogError("OVERSEER: actionUnSubscribe: cannot remove Key that does not exist: " + nameGameObject);
        }
    }

    public void showAllCurActions()
    {
        foreach (KeyValuePair<string, string> kvp in actionTracker)
        {
            Debug.Log("OVERSEER: ACTION TRACKER ENTRY: Key = " + kvp.Key + ", Value = " + kvp.Value);
        }
    }

    public void clearAllActions()
    {
        actionTracker.Clear();
    }

    //checks whether all actions have yet to complete or not
    public bool isOngoingActions()
    {
        if (actionTracker.Count > 0)
            return true;
        else
            return false;
    }
    #endregion

    public int getPlayerGroupSize()
    {
        return playerCharacters.Count;
    }

    //cues the currently used card to move into the "damage enemy" portion
    public void cueCardAttack()
    {
        Debug.Log("OVERSEER: cueCardAttack");
        if (cardAction != null)
            cardAction.isCardCue = true;
    }

    public void showUI(int whichUI)
    {
        if (whichUI > 2 || whichUI < 0)
            Debug.LogError("OVERSEER: showUI: invalid UI: " + whichUI);
        else
            playerUIs[whichUI].gameObject.SetActive(true);
    }

    public void hideUI(int whichUI, bool isPlayer)
    {
        if (whichUI > 2 || whichUI < 0)
            Debug.LogError("OVERSEER: showUI: invalid UI: " + whichUI);
        else
            playerUIs[whichUI].gameObject.SetActive(false);
    }

    public void showFieldUI()
    {
        foreach(GameObject pUI in playerFieldUIs)
        {
            pUI.SetActive(true);
        }
        foreach(GameObject eUI in enemyFieldUIs)
        {
            eUI.SetActive(true);
        }
    }

    public void hideFieldUI()
    {
        foreach (GameObject pUI in playerFieldUIs)
            pUI.SetActive(false);
        foreach (GameObject eUI in enemyFieldUIs)
            eUI.SetActive(false);
    }

    public void createFieldUIs()
    {
        foreach(Character pChar in playerCharacters)
        {
            playerFieldUIs.Add(Instantiate(fieldStatusUIPrefab,
                                Vector3.zero,
                                Quaternion.identity,
                                fieldStatusUIParent.transform));
            playerFieldUIs[playerFieldUIs.Count - 1].GetComponent<fieldStatusUI>().setChar(pChar);
            pChar.setFieldUI(playerFieldUIs[playerFieldUIs.Count - 1].GetComponent<fieldStatusUI>());
            playerFieldUIs[playerFieldUIs.Count - 1].GetComponent<fieldStatusUI>().updateFieldStatusInfo();
        }
        foreach(GameObject eChar in enemies)
        {
            enemyFieldUIs.Add(Instantiate(fieldStatusUIPrefab,
                                Vector3.zero,
                                Quaternion.identity,
                                fieldStatusUIParent.transform));
            enemyFieldUIs[enemyFieldUIs.Count - 1].GetComponent<fieldStatusUI>().setChar(
                                eChar.GetComponent<Character>());
            eChar.GetComponent<Character>().setFieldUI(
                                enemyFieldUIs[enemyFieldUIs.Count - 1].GetComponent<fieldStatusUI>());
            enemyFieldUIs[enemyFieldUIs.Count - 1].GetComponent<fieldStatusUI>().updateFieldStatusInfo();
        }
    }

    public void destroyFieldUIs()
    {
        foreach (GameObject pUI in playerFieldUIs)
            Destroy(pUI);
        foreach (GameObject eUI in enemyFieldUIs)
            Destroy(eUI);
        playerFieldUIs.Clear();
        enemyFieldUIs.Clear();
    }

    public Character getCurCharTurn()
    {
        if (turnOrder.Count == 0)
            Debug.LogError("OVERSEER: GETCURCHARTURN: No characters in the turn order.");
        return turnOrder[turnOrder.Count - 1];
    }

    public void addCharacterToPlayers(Character character)
    {
        playerCharacters.Add(character);
        character.setIsPlayerCharacter(true);
    }

    public void removeFromPlayers(Character character)
    {
        playerCharacters.Remove(character);
        character.setIsPlayerCharacter(false);
    }

    // Start is called before the first frame update
    void Start()
    {
        
        curCharTurnItr = 0;

        controls = WorldMode.instance.controls;
        controls.SelectSkillPos.ChooseDirection.performed += ctx => dirInput = ctx.ReadValue<Vector2>();
        controls.SelectSkillPos.ChooseDirection.canceled += ctx => dirInput = Vector2.zero;

        //SwapHeroIndicator.SetActive(false);
        anim.enabled = false;
    }

    void Update()
    {
        Vector3Int cursorPosChange = Vector3Int.zero;

        #region controlsSelectExecutePos
        if (WorldMode.instance.mode == wMode.SELECTSKILLPOS)
        {
            //WASD/joystick/d-pad: Move the cursor to select a position on the field
            if (controls.SelectSkillPos.ChooseDirection.triggered)
            {
                if (Mathf.Abs(dirInput.x) > Mathf.Abs(dirInput.y))
                {
                    if (dirInput.x < 0)
                        cursorPosChange.y++;
                    else if (dirInput.x > 0)
                        cursorPosChange.y--;
                }
                else
                {
                    if (dirInput.y < 0)
                        cursorPosChange.x--;
                    else
                        cursorPosChange.x++;
                }

                Debug.Log("OVERSEER: Update: changing cursor pos");
                //if new pos exists on the battle field
                if (grid.GetComponent<TileInfo>().doesTileExist(cursorControls.instance.getCursorPos() + cursorPosChange))
                {
                    cursorControls.instance.changeCursorPosition(cursorPosChange);

                    //if attacking and cursor's destination point is within attack range, move cursor
                    if (curArcSelection == arcSelection.Attack || curArcSelection == arcSelection.Cards)
                    {
                        //shows danger zone if cursor is on an attackable position
                        if (FieldHighlight.instance.getFinalNodes().Contains(cursorControls.instance.getCursorPos()))
                            FieldHighlight.instance.showDangerZone(cursorControls.instance.getCursorPos(),
                                                                    cardAction.getDamagePositions());
                        else
                        {
                            FieldHighlight.instance.resetDangerZone();
                        }
                    }                    
                }
            }

            //Circle/Backspace: Reverts to the "NavigateMenus" wMode 
            if (controls.SelectSkillPos.RemoveLastCard.triggered)
            {
                if (curArcSelection == arcSelection.Cards)
                {
                    StartCoroutine(playerUIs[0].deselectButton());
                }
                else
                    StartCoroutine(cancelSelectPos());
            }

            //Space/btnSouth: Confirms a selected position on the field and performs the move/attack/card action
            if (controls.SelectSkillPos.SetCard.triggered)
            {
                if (FieldHighlight.instance.getFinalNodes().Contains(cursorControls.instance.getCursorPos()))
                {
                    //general steps when an action is initiated
                    WorldMode.instance.changeWorldMode(wMode.EXECUTEPLAYERACTIONS);
                    cameraFollowScript.instance.updateTarget(getCurCharTurn().gameObject);
                    FieldHighlight.instance.resetHighlighting();
                    cursorControls.instance.setCursorActive(false);
                    apGague.gameObject.SetActive(false);

                    //Move Action
                    if (curArcSelection == arcSelection.Move)
                    {
                        Debug.Log("OVERSEER: Update: Move Action");

                        //Refund unused AP
                        while (curAPCost > 1)
                        {
                            //recalculate if cursor is within movement area of curAPCost - 1
                            curAPCost--;
                            curAPPwr = calculateAPpwr(getCurCharTurn(), curArcSelection, cardAction, curAPCost);
                            FieldHighlight.instance.resetHighlighting();
                            FieldHighlight.instance.getMoveableArea(curAPPwr);
                            if (!FieldHighlight.instance.getFinalNodes().Contains(cursorControls.instance.getCursorPos()))
                            {
                                curAPCost++;
                                curAPPwr = calculateAPpwr(getCurCharTurn(), curArcSelection, cardAction, curAPCost);
                                FieldHighlight.instance.resetHighlighting();
                                FieldHighlight.instance.getMoveableArea(curAPPwr);
                                break;
                            }
                        }
                        FieldHighlight.instance.resetHighlighting();

                        moveAction = getCurCharTurn().getBaseMovement();
                        moveAction.setNewMovementList(Pathfinding.instance.getPath(getCurCharTurn().gridPos,
                                                                cursorControls.instance.getCursorPos(),
                                                                FieldHighlight.instance.getFinalNodes()),
                                                                getCurCharTurn().gridPos);

                        StartCoroutine(getCurCharTurn().getBaseMovement().onUse(playerCharacters[curCharTurnItr], new Vector3Int()));
                    }

                    //Attack Action
                    if (curArcSelection == arcSelection.Attack)
                    {
                        Debug.Log("OVERSEER: Update: Attack Action");
                        cardAction.setAPUsed(curAPCost);
                        StartCoroutine(cardAction.onUse(getCurCharTurn(), cursorControls.instance.getCursorPos()));
                    }

                    //Card Action
                    if (curArcSelection == arcSelection.Cards)
                    {
                        Debug.Log("OVERSEER: Update: CardAction: " + cardAction.name);
                        StartCoroutine(cardAction.onUse(getCurCharTurn(), cursorControls.instance.getCursorPos()));
                    }
                }
            }

            //LB/Q: decrease the amount of ap used for a move/attack action
            if (controls.SelectSkillPos.APdecrease.triggered)
            { 
                //can only modify ap of Move or Attack actions, not card actions
                if (curAPCost > 1)
                {
                    if (curArcSelection == arcSelection.Move)
                    {
                        curAPCost--;
                        curAPPwr = calculateAPpwr(getCurCharTurn(), curArcSelection, cardAction, curAPCost);
                        FieldHighlight.instance.resetHighlighting();
                        FieldHighlight.instance.getMoveableArea(curAPPwr);
                        apGague.updateAPGague(curAPCost, getCurCharTurn().getCurAP(), curAPPwr, true, false);
                    }
                    if (curArcSelection == arcSelection.Attack)
                    {
                        curAPCost--;
                        curAPPwr = calculateAPpwr(getCurCharTurn(), curArcSelection, cardAction, curAPCost);
                        apGague.updateAPGague(curAPCost, getCurCharTurn().getCurAP(), curAPPwr, true, true);
                    }
                }
            }

            //RB/E: increase the amount of ap used for a move/attack action
            if (controls.SelectSkillPos.APincrease.triggered)
            {
                if (curAPCost < getCurCharTurn().getCurAP())
                {
                    if (curArcSelection == arcSelection.Move)
                    {
                        curAPCost++;
                        curAPPwr = calculateAPpwr(getCurCharTurn(), curArcSelection, cardAction, curAPCost);
                        FieldHighlight.instance.resetHighlighting();
                        FieldHighlight.instance.getMoveableArea(curAPPwr);
                        apGague.updateAPGague(curAPCost, getCurCharTurn().getCurAP(), curAPPwr, true, false);
                    }
                    if (curArcSelection == arcSelection.Attack)
                    {
                        curAPCost++;
                        curAPPwr = calculateAPpwr(getCurCharTurn(), curArcSelection, cardAction, curAPCost);
                        apGague.updateAPGague(curAPCost, getCurCharTurn().getCurAP(), curAPPwr, true, true);
                    }
                }
            }
        }
        #endregion

        #region controlsNavigateMenus
        //controls for navigating the action arc/choose card menus and swapping between heroes.
        else if (WorldMode.instance.mode == wMode.NAVIGATEMENUS)
        {

            if (controls.NavigateMenus.Cancel.triggered)
            {
                Debug.Log("OVERSEER: Update: navigatemenus: cancel:");
                Menu.instance.resetArc(getCurCharTurn(), playerUIs[0]);
            }    
        }
        #endregion

        #region controlsExecuteActions
        else if (WorldMode.instance.mode == wMode.EXECUTEPLAYERACTIONS)
        {

            //Listen for all of actionTracker's subscriptions to finish
            if (!isOngoingActions())
            {
                //discard the used card, if it's a card from the hand
                if (curArcSelection == arcSelection.Cards)
                    getCurCharTurn().discard();

                cueDeadCharacters();
                int battleCondition = checkWinLossCondition();
                if (battleCondition != 0)
                {
                    turnOrder.Clear();
                    TurnOrderIcons.instance.destroyAllIcons();
                    if (battleCondition == 1)
                    {
                        StartCoroutine(curBattleScript.endBattle(false));//win
                    }
                    else
                    {
                        StartCoroutine(curBattleScript.endBattle(true));//lose
                    }
                    return;
                }

                //all animations have finished, Complete the current action and return to menu
                Debug.Log("OVERSEER: Update: finishing current action");
                getCurCharTurn().useAP(curAPCost, curArcSelection);
                
                updateAllUIs();
                clearAllActions();
                WorldMode.instance.changeWorldMode(wMode.NAVIGATEMENUS);
                
                //Temporary
                cameraFollowScript.instance.updateTarget(getCurCharTurn().gameObject);

                //Menu.instance.openArc();
                Menu.instance.resetArc(getCurCharTurn(), playerUIs[0]);
                cursorControls.instance.setCursorActive(false);
            }

        }
        #endregion

        #region EnemyTurn
        else if (WorldMode.instance.mode == wMode.ENEMYTURN)
        {
            //check for active actions
            if (!isOngoingActions())
            {
                //discard card
                if (getCurCharTurn().GetComponent<enemyBrain>().GetArcSelection() == arcSelection.Cards)
                    getCurCharTurn().GetComponent<Character>().discard();

                cueDeadCharacters();
                int battleCondition = checkWinLossCondition();
                if (battleCondition != 0)
                {
                    Debug.Log("Battle over DELETE");
                    turnOrder.Clear();
                    TurnOrderIcons.instance.destroyAllIcons();
                    if (battleCondition == 1)
                    {
                        StartCoroutine(curBattleScript.endBattle(false));//win
                    }
                    else
                    {
                        StartCoroutine(curBattleScript.endBattle(true));//lose
                    }
                    return;
                }

                //all animations have finished, check if Enemy is done with its turn
                if (getCurCharTurn().GetComponent<enemyBrain>().getIsTurnComplete())
                {
                    Debug.Log("Finished Turn DELETE");
                    endTurn();

                    StartCoroutine(startNextTurn());
                    return;
                }

                cameraFollowScript.instance.updateTarget(getCurCharTurn().gameObject);
                FieldHighlight.instance.updateCurCharacter(getCurCharTurn().gameObject.GetComponent<Character>());
                StartCoroutine(getCurCharTurn().gameObject.GetComponent<enemyBrain>().enemyTakeAction());
            }
        }
        #endregion
    }

    //Called at the start of any particular battle. Once per battle.
    public IEnumerator beginCombat(List<Vector3Int> playerStartPositions, BattleScript battleInfo)
    {
        curCharTurnItr = 0;
        curBattleScript = battleInfo;

        //Change the world mode and thereby restrict player controls while battle is starting
        WorldMode.instance.changeWorldMode(wMode.WAIT);

        //add enemies to overseer
        enemies = battleInfo.enemies;

        //move camera to center of the field
        cursorControls.instance.setCursorActive(false);
        cursorControls.instance.transform.position = battleInfo.cameraDestPos;
        cameraFollowScript.instance.changeCameraSpeed(4f);
        StartCoroutine(updateCameraTarget(cursorControls.instance.gameObject));

        //change player facing direction and move to the tile wave start Pos
        playerCharacters[0].changeFacingDirection(battleInfo.waveStartPos);
        StartCoroutine(playerCharacters[0].freeMoveTo(battleInfo.waveStartPos, grid.WorldToCell(playerCharacters[0].transform.position)));
        yield return new WaitForEndOfFrame();
        yield return new WaitWhile(playerCharacters[0].getIsActionInProcess);

        //update battle arena and animate field tiles into being in a wave pattern
        grid.GetComponent<TileInfo>().updateMoveableTiles(battleInfo.getCurBattleGrid());
        FieldHighlight.instance.animateFieldTiles(battleInfo.waveStartPos);
        grid.GetComponent<TileInfo>().hideMoveableTilemap();
        yield return new WaitWhile(isOngoingActions);
        yield return new WaitForSeconds(.25f);
        cameraFollowScript.instance.resetCameraSpeed();

        //when all field tiles have finished animating, delete those game objects and show the tilemap
        FieldHighlight.instance.destroyFieldTiles();
        grid.GetComponent<TileInfo>().showMoveableTilemap();

        //Fade in player's additional characters
        for (int i = 1; i < playerCharacters.Count; i++)
        {
            playerCharacters[i].turnOnOffSpriteParent(true);
            playerCharacters[i].Start();
            playerCharacters[i].transform.position = playerCharacters[0].transform.position;
        }
        yield return new WaitForEndOfFrame();
        for (int i = 1; i < playerCharacters.Count; i++)
        {
            playerCharacters[i].cueAnim(cardType.Attack, "fadeIn");
            playerCharacters[i].GetComponent<CapsuleCollider2D>().enabled = false;
            playerCharacters[i].transform.position = playerCharacters[0].transform.position;
            playerCharacters[i].changeFacingDirection(battleInfo.waveStartPos);
        }

        //Move all characters to their starting positions
        Debug.Log("OVERSEER: beginCombat: Moving characters to starting positions.");
        int itr = 0;
        foreach (Character player in playerCharacters)
        {
            StartCoroutine(player.freeMoveTo(playerStartPositions[itr], grid.WorldToCell(player.transform.position)));
            itr++;
        }

        itr = 0;
        foreach(GameObject enemy in enemies)
        {
            StartCoroutine(enemy.GetComponent<Character>().freeMoveTo(battleInfo.getEnemyStartPositions(itr),
                    grid.WorldToCell(enemy.transform.position)));
            itr++;
        }

        Debug.Log("OVERSEER: beginCombat: All players at starting positions.");
        //all characters finish moving to start positions
        yield return new WaitWhile(isOngoingActions);

        //set first turn order and instantiate turn icons
        getTurnOrder();
        TurnOrderIcons.instance.newTurnOrder(turnOrder);
        roundNumber = 1;

        //Turn off Environmental collisions. 
        if (environmentalCollision != null)
            environmentalCollision.gameObject.SetActive(false);

        //Start battle banners
        BannerAnimScript.instance.turnOnBanner(0);//begin battle

        //Set the characterUIs
        itr = 0;
        if (playerCharacters.Count > 3)
            Debug.LogError("OVERSEER: beginCombat: Player count > 3");

        createFieldUIs();
        
        //animates UI moving on screen
        anim.enabled = true;
        anim.Play("showUI");
        foreach (Character character in playerCharacters)
            character.changeFacingDirection(battleInfo.getBattleFacingDir());
        yield return new WaitWhile(isOngoingActions);
        yield return new WaitForSeconds(0.5f);

        foreach (Character character in turnOrder)
            character.resetDeck();

        StartCoroutine(startNextTurn());
    }

    public void getTurnOrder()
    {
        int itr = 0;
        int curCharTickSpd = 0;
        foreach (Character player in playerCharacters)
        {
            if (player.getIsAlive())
            {
                itr = 0;
                curCharTickSpd = (3 * player.getSpeedStat()) - (4 * player.getAPUsedThisTurn());

                foreach (Character chr in turnOrder)
                {
                    if (curCharTickSpd <= (3 * chr.getSpeedStat()) - (4 * chr.getAPUsedThisTurn()))
                        break;
                    itr++;
                }

                turnOrder.Insert(itr, player);
            }
        }

        Character curEnemy;
        
        foreach(GameObject enemy in enemies)
        {
            itr = 0;
            curEnemy = enemy.GetComponent<Character>();
            curCharTickSpd = (3 * curEnemy.getSpeedStat()) - (4 * curEnemy.getAPUsedThisTurn());

            foreach(Character chr in turnOrder)
            {
                if (curCharTickSpd <= (3 * chr.getSpeedStat()) - (4 * chr.getAPUsedThisTurn()))
                    break;
                itr++;
            }

            turnOrder.Insert(itr, curEnemy);
        }
    }

    public IEnumerator startNextTurn()
    {
        WorldMode.instance.changeWorldMode(wMode.WAIT);
        Menu.instance.closeArc();
        hideFieldUI();

        if (turnOrder.Count == 0)
        {
            getTurnOrder();
            TurnOrderIcons.instance.newTurnOrder(turnOrder);
        }

        Character curChar = getCurCharTurn();

        StartCoroutine(updateCameraTarget(curChar.gameObject));
        FieldHighlight.instance.updateCurCharacter(curChar);
        yield return new WaitForEndOfFrame();
        yield return new WaitWhile(isOngoingActions);
        playerUIs[0].gameObject.SetActive(true);
        curChar.setCharUI(playerUIs[0]);
        playerUIs[0].updateDeckAndDiscardSize(getCurCharTurn().getFullDeckSize(),
                                                getCurCharTurn().getCurDiscardSize());
        anim.Play("showUI");

        playerUIs[0].unHoverDeck();
        yield return new WaitForEndOfFrame();
        yield return new WaitWhile(isOngoingActions);
        curChar.beginTurn();

        if (curChar.getIsPlayerCharacter())
        {
            Menu.instance.openArc();
            Menu.instance.resetArc(curChar, playerUIs[0]);
            WorldMode.instance.changeWorldMode(wMode.NAVIGATEMENUS);
        }
        else
        {
            Debug.Log("ONGOING ACTIONS: " + isOngoingActions());
            yield return new WaitWhile(isOngoingActions);
            curChar.GetComponent<enemyBrain>().resetTurn();
            WorldMode.instance.changeWorldMode(wMode.ENEMYTURN);
        }
        showFieldUI();
    }

    public void endTurn()
    {
        getCurCharTurn().endTurn();
        anim.Play("hideUI");
        turnOrder.RemoveAt(turnOrder.Count - 1);
        TurnOrderIcons.instance.nextCharacterTurn();
    }
    
    //Called at the conclusion of a battle, when the battle goals have been completed. Returns to the Freeroam Mode.
    public IEnumerator endCombat()
    {
        Debug.Log("OVERSEER: Ending Combat");
        WorldMode.instance.changeWorldMode(wMode.WAIT);
        yield return new WaitWhile(isOngoingActions);

        if (actionTracker.Count != 0)
            actionTracker.ToList().ForEach(x => Debug.Log("ActionTracker Message: " + x.Key));

        //destroy all UI elements
        destroyFieldUIs();
        anim.Play("hideUI");
        playerUIs[0].resetCards(new List<cardBase>());
        playerUIs[0].resetAP(0, 0, 0);
        TurnOrderIcons.instance.destroyAllIcons();
        FieldHighlight.instance.resetHighlighting();
        Menu.instance.closeArc();
        foreach (Character character in playerCharacters)
        {
            character.endCombat();//restores character health to 1
            character.changeFacingDirection(facingDir.DL);
        }
        yield return new WaitWhile(isOngoingActions);

        foreach(Character character in playerCharacters)
        {
            character.cueAnim(cardType.Attack, "celebrate");
        }

        //Do end combat banner/celebration animations here
        BannerAnimScript.instance.turnOnBanner(1);//begin battle
        yield return new WaitWhile(isOngoingActions);
        grid.GetComponent<TileInfo>().hideMoveableTilemap();

        playerCharacters[0].GetAnimator().Play("idle");
        StartCoroutine(updateCameraTarget(playerCharacters[0].gameObject));

        //Do other end of combat things here: experience, items, banners, whatever
        for (int i = 1; i < playerCharacters.Count; i++)
        {
            StartCoroutine(playerCharacters[i].freeMoveTo(playerCharacters[0].gridPos, playerCharacters[i].gridPos));
        }
        yield return new WaitWhile(isOngoingActions);
        
        for (int i = 1; i < playerCharacters.Count; i++)
        {
            playerCharacters[i].turnOnOffSpriteParent(false);
        }
        Debug.Log("Finished Combat");

        if (environmentalCollision != null)
            environmentalCollision.gameObject.SetActive(true);
        Destroy(curBattleScript.gameObject);
        WorldMode.instance.changeWorldMode(wMode.FREEROAM);
    }

    public IEnumerator GameOver()
    {
        WorldMode.instance.changeWorldMode(wMode.WAIT);

        //turn off ui stuff
        destroyFieldUIs();
        anim.Play("hideUI");
        TurnOrderIcons.instance.destroyAllIcons();
        FieldHighlight.instance.resetHighlighting();
        Menu.instance.closeArc();
        yield return new WaitWhile(isOngoingActions);

        //show game over screen
        StartCoroutine(WorldMode.instance.GameOver());
    }    

    //Called when the player selects "End Turn". Ends the hero phase and transitions to the Enemy phase.
    public void endHeroPhase()
    {
        hideFieldUI();
    }

    //Called when all enemies have performed all the actions they are going to perform in a turn. Returns to Hero Phase
    public void endEnemyPhase()
    {
        hideFieldUI();
    }

    public void updateAllUIs()
    {
        foreach (Character player in playerCharacters)
        {
            player.updateUI();
        }
        foreach(GameObject enemy in enemies)
        {
            enemy.GetComponent<Character>().updateUI();
        }
    }

    public bool doesCharacterExistAtPos(Vector3Int pos)
    {
        bool truth = false;

        foreach (Character curChar in playerCharacters)
        {
            if (curChar.gridPos == pos)
            {
                truth = true;
                break;
            }
        }

        if (!truth)
        {
            foreach (GameObject enemy in enemies)
            {
                if (enemy.gameObject.GetComponent<Character>().gridPos == pos)
                {
                    truth = true;
                    break;
                }
            }
        }

        return truth;
    }

    public Character getCharacterAtPos(Vector3Int pos)
    {
        foreach (Character curChar in playerCharacters)
        {
            if (curChar.gridPos == pos)
                return curChar;
        }

        foreach (GameObject enemy in enemies)
            if (enemy.gameObject.GetComponent<Character>().gridPos == pos)
                return enemy.gameObject.GetComponent<Character>();

        return null;
    }

    //returns the set of all positions adjacent to player characters
    //  that an enemy could attack from.
    public List<Vector3Int> getEnemyAttackPositions(Vector3Int sourcePos, Vector2Int range)
    {
        List<Vector3Int> attackPositions = new List<Vector3Int>();

        foreach(Character character in playerCharacters)
        {
            if (character.getIsAlive())
            {
                FieldHighlight.instance.resetHighlighting();
                FieldHighlight.instance.updateCurCharacter(character);
                attackPositions.Union(FieldHighlight.instance.getAttackArea(range));
            }
        }
        FieldHighlight.instance.resetHighlighting();
        return attackPositions;
    }
    
    //returns the list of characters within the attack range of a source position
    public List<Character> getPlayerCharactersWithinRange(Vector3Int sourcePos, Vector2Int range)
    {
        List<Character> charactersWithinRange = new List<Character>();
        int distToCharacter;

        foreach(Character character in playerCharacters)
        {
            if (character.getIsAlive())
            {
                distToCharacter = Mathf.Abs(character.gridPos.x - sourcePos.x) + Mathf.Abs(character.gridPos.y - sourcePos.y);

                if (distToCharacter >= range.x
                    && distToCharacter <= range.y)
                {
                    charactersWithinRange.Add(character);
                }
            }
        }

        return charactersWithinRange;
    }

    //returns the position of the player closest to a specified enemy position.
    public Vector3Int getClosestPlayerPos(Vector3Int enemyPos)
    {
        Vector3Int closestPos = new Vector3Int();
        int distToCurCharacter,
            closestDist = 99;
        foreach (Character character in playerCharacters)
        {
            if (character.getIsAlive())
            {
                distToCurCharacter = Mathf.Abs(character.gridPos.x - enemyPos.x) + Mathf.Abs(character.gridPos.y - enemyPos.y);
                if (distToCurCharacter < closestDist)
                {
                    closestDist = distToCurCharacter;
                    closestPos = character.gridPos;
                }
            }
        }

        return closestPos;
    }
    #region combatActions
    /*
        Functions for the Arc Buttons. Made in the Overseer to references the current character.
        Referenced via the inspector in Unity
     */
    public void arcMoveCharacter()
    {
        if (getCurCharTurn().getCurAP() + getCurCharTurn().getBonusAP() > 0)
        {
            Debug.Log("OVERSEER: moveCharacter: ");
            curArcSelection = arcSelection.Move;
            Menu.instance.closeArc();

            //update card, ap cost, power, and visual gague
            changeCardAction(getCurCharTurn().getBaseMovement());
            apGague.gameObject.SetActive(true);
            curAPCost = 1;
            curAPPwr = calculateAPpwr(getCurCharTurn(), curArcSelection, cardAction, curAPCost);
            apGague.updateCharRef(getCurCharTurn());
            apGague.updateAPGague(curAPCost, getCurCharTurn().getCurAP(),
                                    curAPPwr, true, false);

            //update field, world mode, cursor
            FieldHighlight.instance.getMoveableArea(curAPPwr);
            WorldMode.instance.changeWorldMode(wMode.SELECTSKILLPOS);
            //SwapHeroIndicator.SetActive(false);
            cursorControls.instance.setCursorActive(true);
            cursorControls.instance.setCursorPosition(getCurCharTurn().gridPos);
            cameraFollowScript.instance.updateTarget(cursorControls.instance.gameObject);
        }
    }

    public void arcAttackCharacter()
    {
        if (getCurCharTurn().getCurAP() + getCurCharTurn().getBonusAP() > 0)
        {
            Debug.Log("OVERSEER: attackCharacter: ");
            curArcSelection = arcSelection.Attack;
            Menu.instance.closeArc();

            //update card, ap cost, power, and visual gague
            changeCardAction(getCurCharTurn().getBaseAttack());
            apGague.gameObject.SetActive(true);
            curAPCost = 1;
            curAPPwr = calculateAPpwr(getCurCharTurn(), curArcSelection, cardAction, curAPCost);
            apGague.updateCharRef(getCurCharTurn());
            apGague.updateAPGague(curAPCost, getCurCharTurn().getCurAP(),
                                    curAPPwr, true, true);

            //update field, world mode, cursor
            FieldHighlight.instance.getAttackArea(cardAction.getRange());
            WorldMode.instance.changeWorldMode(wMode.SELECTSKILLPOS);
            //SwapHeroIndicator.SetActive(false);
            cursorControls.instance.setCursorActive(true);
            cursorControls.instance.setCursorPosition(getCurCharTurn().gridPos);
            cameraFollowScript.instance.updateTarget(cursorControls.instance.gameObject);
        }
    }

    public void arcCardButton()
    {
        Debug.Log("OVERSEER: arcCardButton: ");
        Menu.instance.goToCards();
        curArcSelection = arcSelection.Cards;
        playerUIs[0].turnOnDetailedCard();
    }

    public void arcEndTurnButton()
    {
        Debug.Log("OVERSEER: arcEndTurnButton: Ending Hero Phase, beginning Enemy phase.");
        endTurn();
        StartCoroutine(startNextTurn());
    }

    public void changeCardAction(cardBase card)
    {
        cardAction = card;
    }

    public IEnumerator resetCamera()
    {
        cursorControls.instance.setCursorActive(false);
        apGague.gameObject.SetActive(false);
        StartCoroutine(updateCameraTarget(getCurCharTurn().gameObject));
        yield return new WaitForEndOfFrame();
        yield return new WaitWhile(isOngoingActions);
    }

    //Cancels the "select a pos on the field" mode, back to the "choose a card/action
    public IEnumerator cancelSelectPos()
    {
        Debug.Log("OVERSEER: cancelSelectPos: ");
        FieldHighlight.instance.resetHighlighting();
        WorldMode.instance.changeWorldMode(wMode.WAIT);//listens for action tracker 
        cursorControls.instance.setCursorActive(false);
        apGague.gameObject.SetActive(false);

        StartCoroutine(updateCameraTarget(getCurCharTurn().gameObject));
        yield return new WaitForEndOfFrame();
        yield return new WaitWhile(isOngoingActions);

        WorldMode.instance.changeWorldMode(wMode.NAVIGATEMENUS);
        if (curArcSelection == arcSelection.Cards)
            Menu.instance.goToLastSelection();
        else    
            Menu.instance.resetArc(getCurCharTurn(), playerUIs[0]);

        //SwapHeroIndicator.SetActive(true);
        curAPPwr = 0;
        curAPCost = 0;
    }

    //updates the camera with a new target, and waits until it reaches that target to allow further actions.
    public IEnumerator updateCameraTarget(GameObject target)
    {
        actionSubscribe("Camera", "updateTarget " + target.name);
        cameraFollowScript.instance.updateTarget(target);
        yield return new WaitUntil(cameraFollowScript.instance.isCameraAtTarget);
        actionUnSubscribe("Camera");
    }

    //Needs to do call the actual card's onSelect fn first, then this.
    public void cardOnSelect()
    {
        Debug.Log("OVERSEER: cardOnSelect: card: " + cardAction);
        Menu.instance.closeArc();
            
        //cardAction.onSelected handles which highlighting to do.
        cardAction.onSelected();
            
        WorldMode.instance.changeWorldMode(wMode.SELECTSKILLPOS);
        //SwapHeroIndicator.SetActive(false);
        cursorControls.instance.setCursorActive(true);
        cursorControls.instance.setCursorPosition(getCurCharTurn().gridPos);
        cameraFollowScript.instance.updateTarget(cursorControls.instance.gameObject);

        //shows danger zone if cursor is on an attackable position
        if (cardAction.type != cardType.Movement && 
            FieldHighlight.instance.getFinalNodes().Contains(cursorControls.instance.getCursorPos()))
            FieldHighlight.instance.showDangerZone(cursorControls.instance.getCursorPos(),
                                                    cardAction.getDamagePositions());
        else
        {
            FieldHighlight.instance.resetDangerZone();
        }

        apGague.gameObject.SetActive(true);
        curAPCost = cardAction.getCost();
        curAPPwr = calculateAPpwr(getCurCharTurn(), curArcSelection, cardAction, curAPCost);
        apGague.updateCharRef(getCurCharTurn());
        apGague.updateAPGague(curAPCost, curAPCost, curAPPwr, false, true);
        
    }

    //Player character attacks call this function to damage their enemies.
    public void attack(List<Vector3Int> relAtkPositions, Vector3Int targetPos, 
                        cardType type, Affinity element, int cardPwr)
    {
        Vector3Int universalAtkPos;
        Character targetChar;

        foreach (Vector3Int relAtkPos in relAtkPositions)
        {
            universalAtkPos = targetPos + relAtkPos;

            Debug.Log("OVERSEER: is attacking: " + universalAtkPos);

            //if an enemy is at the universalAtkPos
            if (doesCharacterExistAtPos(universalAtkPos))
            {
                Debug.Log("OVERSEER: Enemy exists at: " + universalAtkPos);
                Debug.Log("OVERSEER: PwrAtPos:" + cardPwr);

                targetChar = getCharacterAtPos(universalAtkPos);
                targetChar.takeAttack(cardPwr, type, element);

            }
        }
    }

    public void pushCharacter(Vector3Int attackerPos, Vector3Int targetPos, List<Vector3Int> dmgPositions, int pushDistance)
    {
        Vector3Int universalAtkPos;
        Character targetChar;

        foreach (Vector3Int dmgPos in dmgPositions)
        {
            universalAtkPos = targetPos + dmgPos;

            if (doesCharacterExistAtPos(universalAtkPos))
            {
                Debug.Log("OVERSEER: Pushing enemy at " + universalAtkPos);

                targetChar = getCharacterAtPos(universalAtkPos);

                //find push direction, times by push distance
                Vector3Int pushDir = universalAtkPos - attackerPos;
                if (pushDir.x != 0 && pushDir.y != 0)
                    Debug.LogError("OVERSEER: PushCharacter: Trying to push diagonally.");
                Debug.Log("OVERSEER: pushDir: " + pushDir);
                pushDir *= pushDistance;

                //pushDestPos
                Vector3Int pushDest = universalAtkPos + pushDir;

                StartCoroutine(targetChar.push(pushDest));
            }
        }
    }

    //called by the chargeAttack script because it is not a monobehaviour
    public void characterCharge(Character whichChar, Vector3Int destPos)
    {
        StartCoroutine(whichChar.push(destPos));
    }

    public void causeStatusEffects(Vector3Int targetPos, List<Vector3Int> dmgPositions, statusEffect statusEffect)
    {
        Vector3Int universalAtkPos;
        Character targetChar;

        foreach(Vector3Int dmgPos in dmgPositions)
        {
            universalAtkPos = targetPos + dmgPos;

            if (doesCharacterExistAtPos(universalAtkPos))
            {
                targetChar = getCharacterAtPos(universalAtkPos);
                Debug.Log("OVERSEER: Causing status effect " + statusEffect + "on " + targetChar.gameObject.name);
                targetChar.addStatusEffect(statusEffect);
            }
        }
    }

    public void cueDeadCharacters()
    {
        foreach (Character player in playerCharacters)
        {
            if (!player.getIsAlive() && !player.getIsAlreadyDead())
            {
                actionSubscribe(player.name + "anim", player.name);
                player.cueAnim(cardType.Attack, "Death");
                //remove from characters in combat
                if (!turnOrder.Remove(player))
                    Debug.Log(player + " is not in the turn order.");
                TurnOrderIcons.instance.newTurnOrder(turnOrder);
            }
        }

        Character curEnemy;
        for (int i = 0; i < enemies.Count; i++)
        {
            curEnemy = enemies[i].GetComponent<Character>();
            if(!curEnemy.getIsAlive() && !curEnemy.getIsAlreadyDead())
            {
                actionSubscribe(curEnemy.name + "anim", curEnemy.name);
                curEnemy.cueAnim(cardType.Attack, "Death");
                //remove enemy from characters in combat
                grid.GetComponent<TileInfo>().setIsOccupied(curEnemy.gridPos, false);
                turnOrder.Remove(curEnemy);
                enemies.Remove(curEnemy.gameObject);
                TurnOrderIcons.instance.newTurnOrder(turnOrder);
            }
        }
    }

    public int checkWinLossCondition()
    {
        bool areEnemiesDead = true;
        bool arePlayersDead = true;
        int battleCondition = 0; //0 = battle continues, 1 = win, 2 = loss

        //check if there are any players alive
        foreach (Character player in playerCharacters)
        {
            if (player.getIsAlive())
            {
                arePlayersDead = false;
                break;
            }
        }
        //check for any living enemies
        foreach (GameObject enemy in enemies)
        {
            if (enemy.GetComponent<Character>().getIsAlive())
            {
                areEnemiesDead = false;
                break;
            }
        }

        //win conditions: enemies dead, players alive
        if (areEnemiesDead && !arePlayersDead)
            battleCondition = 1;
        if (arePlayersDead)
            battleCondition = 2;

        return battleCondition;
    }

    //calculates the strength of a movement/attack action based on how much AP is used.
    public int calculateAPpwr(Character curCharacter, arcSelection attackType, cardBase card, int cost)
    {
        int basePwr = 0;
        int power = 0;
        int bonus = 0;
        int alreadyUsed = 0;

        if (attackType == arcSelection.Move)
        {
            basePwr = curCharacter.getBaseMoveRange();
            alreadyUsed = curCharacter.getMoveAPUsed();
        }

        if (attackType == arcSelection.Attack)
        {
            basePwr = card.getCardPower(curCharacter);
            alreadyUsed = curCharacter.getAtkAPUsed();
        }

        if (attackType == arcSelection.Cards)
            power = card.getCardPower(curCharacter);

        else //move or attack
        {
            if (attackType == arcSelection.Move)
            {
                for (int i = 0; i < cost; i++)
                {
                    bonus = basePwr - (2 * (i + alreadyUsed));
                    if (bonus <= 0)
                        bonus = 1;
                    power += bonus;
                }
            }

            if (attackType == arcSelection.Attack)
            {
                for (int i = 0; i < cost; i++)
                {
                    bonus = basePwr - i - alreadyUsed;
                    if (bonus <= 0)
                        bonus = 1;
                    power += bonus;
                }
            }
        }

        return power;
    }

    public void enemyUpdateAPCost(int cost)
    {
        curAPCost = cost;
    }
    #endregion
}
