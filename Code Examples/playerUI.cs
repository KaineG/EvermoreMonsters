using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class playerUI : MonoBehaviour
{
    //Health
    [SerializeField]
    private Slider charUIHealthbar;
    [SerializeField]
    private Text charUIHealthText;
    
    //Cards
    private List<cardBase> curHandCards = new List<cardBase>();
    [SerializeField]
    private List<GameObject> cardObjects = new List<GameObject>();
    [SerializeField]
    private List<GameObject> cardObjectsInUse = new List<GameObject>();
    private List<int> cardsInUseHandPos = new List<int>();//keeps track of the position in curHand each InUse card was.
    [SerializeField]
    private GameObject cardPrefab;
    [SerializeField]
    private GameObject cardObjectParent;
    [SerializeField]
    private GameObject deckPile;
    [SerializeField]
    private GameObject discardPile;
    [SerializeField]
    private Text txtDeckSize;
    [SerializeField]
    private Text txtDiscardSize;

    int lastCardSelected;

    [SerializeField]
    float cardBaseHeight;
    [SerializeField]
    float cardScale;

    [SerializeField]
    Image cardGlow;

    //Card animation

    [SerializeField]
    GameObject detailedCard;

    //AP
    [SerializeField]
    private List<GameObject> uiAP = new List<GameObject>();
    [SerializeField]
    private GameObject apParent;
    [SerializeField]
    private GameObject apPrefab;
    [SerializeField]
    private Sprite apNormalSprite;
    [SerializeField]
    private Sprite apUsedSprite;
    [SerializeField]
    private Sprite apBonusSprite;

    [SerializeField]
    bool isCenterUI;

    Character curChar;

    [SerializeField]
    Image charToken;

    // Start is called before the first frame update
    void Start()
    {
        if (isCenterUI)
        {
            deckPile.GetComponent<Button>().onClick.AddListener(delegate { drawFromDeck(); });
            discardPile.GetComponent<Button>().onClick.AddListener(delegate { ShuffleDeck(); });
        }
        this.gameObject.SetActive(false);
    }

    //gets the cur hand of cards
    public List<cardBase> getUICurHand()
    {
        return curHandCards;
    }

    //

    public GameObject getBtnToSelect()
    {
        if (curHandCards.Count == 0)
            return deckPile;
        else if (lastCardSelected < curHandCards.Count)
        {
            detailedCard.SetActive(true);
            detailedCard.GetComponent<detailedCardDisplay>().refresh(curHandCards[lastCardSelected], curChar);
            return cardObjects[lastCardSelected];
        }
        else
        {
            detailedCard.SetActive(true);
            detailedCard.GetComponent<detailedCardDisplay>().refresh(curHandCards[0], curChar);
            return cardObjects[0];
        }
    }

    public void turnOnDetailedCard()
    {
        detailedCard.SetActive(true);
    }

    //updates the character UI 
    public void updateCharacterUI(Character whichChar)
    {
        Debug.Log("PLAYERUI: updateCharacterUI: update the character ui");
        curChar = whichChar;

        charToken.sprite = curChar.getToken();

        //update Health values
        charUIHealthbar.maxValue = curChar.getMaxHealth();
        updateHealth(curChar.getHealth(), curChar.getMaxHealth());
        curHandCards = new List<cardBase>();
        curHandCards = curChar.getCurHandCards();
        lastCardSelected = 0;

        //Displays the character hand to the ui
        resetCards(curHandCards);

        //Displays the right number of AP to the UI
        resetAP(curChar.getCurAP(), curChar.getBonusAP(), curChar.getMaxAP());

        //update deck size
        if (isCenterUI)
        {
            txtDeckSize.text = curChar.getCurDeckSize().ToString();
            txtDiscardSize.text = curChar.getCurDiscardSize().ToString();
        }
    }

    //resets the curHandCards and updates them with a new set. Used for switching characters
    public void resetCards(List<cardBase> characterCardHand)
    {
        //resets the cards
        foreach (GameObject card in cardObjects)
            Destroy(card);
        cardObjects.Clear();

        curHandCards = characterCardHand;

        Debug.Log("PLAYERUI: " + curChar.name + " resetCards: cardObjs Count A: " + cardObjects.Count);
        foreach (cardBase card in curHandCards)
        {
            cardObjects.Add(Instantiate(cardPrefab, Vector3.zero, Quaternion.identity, cardObjectParent.transform));
            if (curChar.getIsPlayerCharacter())//player
                cardObjects[cardObjects.Count - 1].GetComponent<cardDisplay>().showCardInfo();
            else
                cardObjects[cardObjects.Count - 1].GetComponent<cardDisplay>().hideCardInfo();

            cardObjects[cardObjects.Count - 1].GetComponent<cardDisplay>().refresh(card, curChar);
            //cardObjects[cardObjects.Count - 1].GetComponent<cardDisplay>().addMoveDelay(cardObjects.Count - 1);
        }
        Debug.Log("PLAYERUI: " + curChar.name + " resetCards: cardObjs Count B: " + cardObjects.Count);
        updateCardPositions(true);
        updateCardBtnNavigation();
    }

    //updates the AP display
    public void resetAP(int curAP, int bonusAP, int maxAP)
    {
        foreach (GameObject ap in uiAP)
            Destroy(ap);
        uiAP.Clear();

        int totalAPShown = curAP + bonusAP;
        if (totalAPShown < maxAP)
            totalAPShown += (maxAP - totalAPShown);
        
        float totalWidth = (25 * totalAPShown) + (5 * (totalAPShown - 1));
        float width;
        for (int itr = 0; itr < totalAPShown; itr++)
        {
            //X = left edge pos + previous AP width + half a single AP width
            width = (float)((-totalWidth / 2) + (30 * itr) + 12.5);
            uiAP.Add(Instantiate(apPrefab, Vector3.zero, Quaternion.identity, apParent.transform));

            uiAP[uiAP.Count - 1].transform.localPosition = new Vector3(width, 0, 0);
            if (!isCenterUI)
                uiAP[uiAP.Count - 1].transform.localScale = new Vector3(.92f, .92f, 1);

            //change image to bonus sprite - default is already normal
            if (itr < curAP)
                uiAP[uiAP.Count - 1].GetComponent<Image>().sprite = apNormalSprite;
            else if (itr < curAP + bonusAP)
                uiAP[uiAP.Count - 1].GetComponent<Image>().sprite = apBonusSprite;
            else
                uiAP[uiAP.Count - 1].GetComponent<Image>().sprite = apUsedSprite;
        }
    }

    //updates the position and spacing of cards in a ui
    void updateCardPositions(bool isMoveInstant)
    {
        //create game objects for each card with the attached information. 
        if (isCenterUI)
        {
            int itr = -cardObjects.Count / 2;
            
            //If UI is center, arc cards around parent origin.
            if (cardObjects.Count % 2 == 0) //even
            {
                Debug.Log("PLAYERUI: " + curChar.name + "updateCardPositions: cardObjs count:" + cardObjects.Count);
                foreach (GameObject card in cardObjects)
                {
                    if (itr == 0)
                        itr++;

                    float adjustedXPos = 0;
                    float adjustedHeight = 0;
                    switch (Mathf.Abs(itr))
                    {
                        case 1:
                            adjustedHeight = cardBaseHeight - 5;
                            adjustedXPos = (itr / Mathf.Abs(itr)) * 53;
                            break;
                        case 2:
                            adjustedHeight = cardBaseHeight - 42;
                            adjustedXPos = (itr / Mathf.Abs(itr)) * 153;
                            break;

                    }
                    Vector3 pos = new Vector3(adjustedXPos, adjustedHeight, 0);

                    if (isMoveInstant)
                    {
                        card.transform.localPosition = pos;
                        card.transform.rotation = Quaternion.Euler(0, 0, -(itr / Mathf.Abs(itr) * (Mathf.Abs(itr * 20) - 10)));
                    }
                    else
                        card.GetComponent<cardDisplay>().moveTo(pos,
                            Quaternion.Euler(0, 0, -(itr / Mathf.Abs(itr) * (Mathf.Abs(itr * 20) - 10))),
                            new Vector3(cardScale, cardScale, 0),
                            .3f,
                            this.name + itr.ToString());

                    itr++;
                }
            }
            else //odd card count
            {
                foreach (GameObject card in cardObjects)
                {
                    float adjustedXPos = 0;
                    float adjustedHeight = 0;
                    switch (Mathf.Abs(itr))
                    {
                        case 0:
                            adjustedHeight = cardBaseHeight;
                            break;
                        case 1:
                            adjustedHeight = cardBaseHeight - 13.4f;
                            adjustedXPos = (itr / Mathf.Abs(itr)) * 103;
                            break;
                        case 2:
                            adjustedHeight = cardBaseHeight - 53.5f;
                            adjustedXPos = (itr / Mathf.Abs(itr)) * 198.7f; 
                            break;

                    }

                    if (isMoveInstant)
                    {
                        card.transform.localPosition = new Vector3(adjustedXPos, adjustedHeight, 0);
                        card.transform.rotation = Quaternion.Euler(0, 0, -itr * 15);
                    }
                    else
                        card.GetComponent<cardDisplay>().moveTo(new Vector3(adjustedXPos, adjustedHeight, 0),
                            Quaternion.Euler(0, 0, -itr * 15),
                            new Vector3(cardScale, cardScale, 0),
                            .3f,
                            this.name + itr.ToString());

                    itr++;
                }
            }

            //InUse cards
            if (cardObjectsInUse.Count > 0)
            {
                itr = 0;
                foreach(GameObject card in cardObjectsInUse)
                {
                    //card.transform.localPosition = new Vector3(-560 + (itr * 120), 590, 0);
                    //card.transform.rotation = Quaternion.identity;
                    card.GetComponent<cardDisplay>().moveTo(new Vector3(-560 + (itr * 120), 590, 0),
                            Quaternion.identity,
                            new Vector3(cardScale, cardScale, 0),
                            .3f,
                            this.name + "inUseCard " + itr.ToString());
                    itr++;
                }
            }
        }
        else
        {
            int itr = 0;
            //spread cards evenly across 175px to the right of parent origin
            foreach (GameObject card in cardObjects)
            {
                //xPos
                Vector3 pos;
                if (curHandCards.Count < 4)
                    pos = new Vector3(25 + (itr * 62), 0, 0);
                else
                    pos = new Vector3(25 + (itr * (160 / curHandCards.Count)),
                                            0,
                                            0);
                if (isMoveInstant)
                {
                card.transform.localPosition = pos;
                card.transform.localScale = new Vector3(.7f, .7f, 1);
                }
                else
                    card.GetComponent<cardDisplay>().moveTo(pos, Quaternion.identity, new Vector3(.7f, .7f, 1), .3f, this.name + itr.ToString());
                
                itr++;
            }
        }

    }

    //draws a card from the character's deck 
    public void drawFromDeck()
    {
        if (curChar.getCurAP() + curChar.getBonusAP() > 0 
            && curHandCards.Count < 5
            && curChar.getCurDeckSize() > 0)
        {
            curChar.useAP(1, arcSelection.Cards);
            curChar.drawCard(false);
        }
        updateDeckAndDiscardSize(curChar.getCurDeckSize(), curChar.getCurDiscardSize());
    }

    //Shuffles the discard pile back into the deck
    public void ShuffleDeck()
    {
        if (curChar.getCurAP() + curChar.getBonusAP() >= 2
            && curChar.getCurDiscardSize() > 0)
        {
            curChar.useAP(2, arcSelection.Cards);
            curChar.ShuffleDeck();

            for (int i = 0; i < 5; i++)
            {
                curChar.drawCard(false);
            }
        }
        updateDeckAndDiscardSize(curChar.getCurDeckSize(), curChar.getCurDiscardSize());
    }

    //called by the Character, this fn receives a card from the character deck and puts it in the curhand
    public bool addCard(cardBase card, bool addMoveDelay)
    {
        if (curHandCards.Count < 5)
        {
            Debug.Log("PLAYERUI: " + curChar.name + " addCard: curHandCards Count A: " + curHandCards.Count);
            card.assignToCharacter(curChar);
            curHandCards.Add(card);
            cardObjects.Add(Instantiate(cardPrefab, deckPile.transform.position, Quaternion.identity, cardObjectParent.transform));
            if(addMoveDelay)
                cardObjects[cardObjects.Count - 1].GetComponent<cardDisplay>().addMoveDelay(cardObjects.Count - 1);
            if (curChar.getIsPlayerCharacter())//player
                cardObjects[cardObjects.Count - 1].GetComponent<cardDisplay>().showCardInfo();
            else
                cardObjects[cardObjects.Count - 1].GetComponent<cardDisplay>().hideCardInfo();
            cardObjects[cardObjects.Count - 1].GetComponent<cardDisplay>().refresh(card, curChar);
            cardObjects[cardObjects.Count - 1].transform.localScale = new Vector3(0.8f, 0.8f, 0);
            Debug.Log("PLAYERUI: " + curChar.name + " addCard: curHandCards Count B: " + curHandCards.Count);
            updateDeckAndDiscardSize(curChar.getCurDeckSize(), curChar.getCurDiscardSize());
            updateCardPositions(false);
            updateCardBtnNavigation();
            return true;
        }
        return false;
    }

    //returns a cardBase object to the Character, telling them to add this card to their discard pile. Also removes a card from curHandCards
    public cardBase discardUsedCard()
    {
        cardBase whichCardDiscarded = curHandCards[lastCardSelected];
        Destroy(cardObjectsInUse[0]);
        cardObjectsInUse.RemoveAt(0);
        cardsInUseHandPos.RemoveAt(0);
        //Destroy(cardObjects[lastCardSelected]);
        //cardObjects.RemoveAt(lastCardSelected);
        curHandCards.RemoveAt(lastCardSelected);
        lastCardSelected = 0;
        updateCardPositions(false);
        updateCardBtnNavigation();
        return whichCardDiscarded;
    }

    // IFF isCenterUI: updates deck and discard size
    public void updateDeckAndDiscardSize(int deckSize, int discardSize)
    {
        if (isCenterUI)
        {
            txtDeckSize.text = deckSize.ToString();
            txtDiscardSize.text = discardSize.ToString();
        }
    }

    //updates the remaining health visual
    public void updateHealth(int curHealth, int maxHealth)
    {
        charUIHealthbar.value = curHealth;
        charUIHealthText.text = curHealth.ToString() + "/" + maxHealth.ToString();
    }

    //IFF isCenterUI: updates the btn navigation between cards, deck pile, and discard pile. If not center, it doesn't matter. Cant interact with it.
    public void updateCardBtnNavigation()
    {
        if (isCenterUI)
        {
            //deck/discard btn navigation
            Button deckBtn = deckPile.GetComponent<Button>();
            Button discardBtn = discardPile.GetComponent<Button>();

            //deck nav
            Navigation curNav = new Navigation();
            curNav.mode = Navigation.Mode.Explicit;
            curNav.selectOnUp = null;
            curNav.selectOnDown = null;
            curNav.selectOnLeft = discardBtn;
            if (cardObjects.Count > 0)
                curNav.selectOnRight = cardObjects[0].GetComponent<Button>();
            else
                curNav.selectOnRight = discardBtn;
            deckBtn.navigation = curNav;

            //discard nav
            if (cardObjects.Count > 0)
                curNav.selectOnLeft = cardObjects[cardObjects.Count - 1].GetComponent<Button>();
            else
                curNav.selectOnLeft = deckBtn;
            curNav.selectOnRight = deckBtn;
            discardBtn.navigation = curNav;

            //Deck EventTriggers
            deckPile.GetComponent<EventTrigger>().triggers.Clear();

            //update deck/discard eventTiggers
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Select;
            entry.callback.AddListener(delegate { hoverDeck(); });
            deckPile.GetComponent<EventTrigger>().triggers.Add(entry);

            Debug.Log("PLAYERUI: updateCardBtnNavigation: Deck Event Trigger listeners B: " + deckPile.GetComponent<EventTrigger>().triggers[0].eventID);
            //Debug.Log("PLAYERUI: updateCardBtnNavigation: Deck Event Trigger listeners C: " + deckPile.GetComponent<EventTrigger>().triggers[0].callback.);

            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Deselect;
            entry.callback.AddListener(delegate { unHoverDeck(); });
            deckPile.GetComponent<EventTrigger>().triggers.Add(entry);


            //Discard EventTriggers
            discardPile.GetComponent<EventTrigger>().triggers.Clear();

            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Select;
            entry.callback.RemoveAllListeners();
            entry.callback.AddListener(delegate { hoverDiscard(); });
            discardPile.GetComponent<EventTrigger>().triggers.Add(entry);

            entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Deselect;
            entry.callback.RemoveAllListeners();
            entry.callback.AddListener(delegate { unHoverDiscard(); });
            discardPile.GetComponent<EventTrigger>().triggers.Add(entry);

            int itr = 0;
            foreach (GameObject card in cardObjects)
            {
                if (itr == 0)
                    curNav.selectOnLeft = deckBtn;
                else
                    curNav.selectOnLeft = cardObjects[itr - 1].GetComponent<Button>();

                if (itr == cardObjects.Count - 1)
                    curNav.selectOnRight = discardBtn;
                else
                    curNav.selectOnRight = cardObjects[itr + 1].GetComponent<Button>();

                //Need a new integer that relates to itr for the delegates, but can't use itr because all delegates end up with the same input val
                int delParam = itr;

                //update BtnNavigation
                card.GetComponent<Button>().navigation = curNav;

                //update Button's OnClick listener
                card.GetComponent<Button>().onClick.RemoveAllListeners();
                card.GetComponent<Button>().onClick.AddListener( delegate { selectButton(delParam); });

                //clear all previous eventTriggers
                card.GetComponent<EventTrigger>().triggers.Clear();

                //update EventTrigger 
                entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.Select;
                entry.callback.RemoveAllListeners();
                entry.callback.AddListener(delegate {showHoverCard(delParam); });
                card.GetComponent<EventTrigger>().triggers.Add(entry);

                entry = new EventTrigger.Entry();
                entry.eventID = EventTriggerType.Deselect;
                entry.callback.RemoveAllListeners();
                entry.callback.AddListener(delegate { hideHoverCard(); });
                card.GetComponent<EventTrigger>().triggers.Add(entry);

                //update card Btn Listeners
                cardObjects[delParam].GetComponent<Button>().onClick.RemoveAllListeners();
                cardObjects[delParam].GetComponent<Button>().onClick.AddListener(delegate { selectButton(delParam); });

                itr++;
            }
        }
    }

    //Passes the Overseer a reference to the selected card and tells it to perform the "onSelect" functionality.
    public void selectButton(int whichCard)
    {
        Debug.Log("PLAYERUI: selectButton: " + (whichCard + 1));
        lastCardSelected = whichCard;
        if (curHandCards.Count < whichCard + 1)
            Debug.Log("PLAYERUI: selectButton: Card does not exist in slot " + (whichCard + 1));
        else if (curHandCards[whichCard].costToUse <= (curChar.getCurAP() + curChar.getBonusAP()))
        {
            //move the selected card to the top-left of the screen
            cardObjectsInUse.Add(cardObjects[whichCard]);
            cardsInUseHandPos.Add(whichCard);
            cardObjects.RemoveAt(whichCard);
            updateCardPositions(false);      

            //Overseer will call the cards "onUse" if it is used, caharUICards->onSelected shows where it would be used etc
            Overseer.instance.changeCardAction(curHandCards[whichCard]);
            Overseer.instance.cardOnSelect();
        }
    }

    public void enemySelectButton(int whichCard)
    {
        Debug.Log("PLAYERUI: selectButton: " + (whichCard + 1));
        lastCardSelected = whichCard;
        if (curHandCards.Count < whichCard + 1)
            Debug.Log("PLAYERUI: selectButton: Card does not exist in slot " + (whichCard + 1));
        else if (curHandCards[whichCard].costToUse <= (curChar.getCurAP() + curChar.getBonusAP()))
        {
            //move the selected card to the top-left of the screen
            cardObjectsInUse.Add(cardObjects[whichCard]);
            cardObjectsInUse[cardObjectsInUse.Count - 1].GetComponent<cardDisplay>().showCardInfo();
            cardObjectsInUse[cardObjectsInUse.Count - 1].GetComponent<cardDisplay>().refresh(curHandCards[whichCard],curChar);
            cardsInUseHandPos.Add(whichCard);
            cardObjects.RemoveAt(whichCard);
            updateCardPositions(false);
        }
    }    

    //removes a card from the "inUse" group and returns whether any remaining cards are "inUse" or not.
    public IEnumerator deselectButton()
    {
        WorldMode.instance.changeWorldMode(wMode.WAIT);
        cardObjects.Insert(cardsInUseHandPos[cardsInUseHandPos.Count - 1], cardObjectsInUse[cardObjectsInUse.Count - 1]);
        cardsInUseHandPos.RemoveAt(cardsInUseHandPos.Count - 1);
        cardObjectsInUse.RemoveAt(cardObjectsInUse.Count - 1);
        updateCardPositions(false);
        if (cardObjectsInUse.Count > 0)
        {
            yield return new WaitForSeconds(0.2f);
            WorldMode.instance.changeWorldMode(wMode.SELECTSKILLPOS);
        }
        else
        {
            StartCoroutine(Overseer.instance.resetCamera());
            yield return new WaitWhile(Overseer.instance.isOngoingActions);
            updateCardBtnNavigation();
            StartCoroutine(Overseer.instance.cancelSelectPos());
        }
    }

    #region eventTriggers
    public void showHoverCard(int whichCard)
    {
        Debug.Log("SHOW HOVER CARD");
        cardGlow.gameObject.SetActive(true);
        cardGlow.rectTransform.localPosition = cardObjectParent.transform.localPosition + cardObjects[whichCard].transform.localPosition;
        cardGlow.rectTransform.sizeDelta = new Vector2(102.75f,125.4f);
        cardGlow.rectTransform.rotation = cardObjects[whichCard].transform.rotation;
        if (isCenterUI)
        {
            detailedCard.SetActive(true);
            detailedCard.GetComponent<detailedCardDisplay>().refresh(curHandCards[whichCard], curChar);
        }
        if (curHandCards[whichCard].costToUse > (curChar.getCurAP() + curChar.getBonusAP()))
            cardGlow.color = Color.red;
        else
            cardGlow.color = Color.white;

        
    }

    public void hideHoverCard()
    {
        Debug.Log("HIDE HOVER CARD");
        cardGlow.gameObject.SetActive(false);
        if (isCenterUI)
            detailedCard.SetActive(false);
    }

    public void hoverDeck()
    {
        Debug.Log("HOVER DECK");
        cardGlow.gameObject.SetActive(true);
        cardGlow.rectTransform.position = deckPile.transform.position;
        cardGlow.rectTransform.sizeDelta = new Vector2(77.9f,95);
        cardGlow.rectTransform.rotation = Quaternion.identity;
        if (isCenterUI)
        {
            detailedCard.SetActive(true);
            detailedCard.GetComponent<detailedCardDisplay>().showDeck("Draw Card", 1);
        }
        if ((curChar.getCurAP() + curChar.getBonusAP()) < 1)//1 = cost of drawing a card
            cardGlow.color = Color.red;
        else
            cardGlow.color = Color.white;
    }

    public void unHoverDeck()
    {
        Debug.Log("UNHOVER DECK");
        cardGlow.gameObject.SetActive(false);
        detailedCard.SetActive(false);
    }

    public void hoverDiscard()
    {
        cardGlow.gameObject.SetActive(true);
        cardGlow.rectTransform.position = discardPile.transform.position;
        cardGlow.rectTransform.sizeDelta = new Vector2(77.9f, 95);
        cardGlow.rectTransform.rotation = Quaternion.identity;
        if (isCenterUI)
        {
            detailedCard.SetActive(true);
            detailedCard.GetComponent<detailedCardDisplay>().showDeck("Shuffle Deck", 3);
        }
        if ((curChar.getCurAP() + curChar.getBonusAP()) < 2)//2 = cost of resetting deck
            cardGlow.color = Color.red;
        else
            cardGlow.color = Color.white;
    }

    public void unHoverDiscard()
    {
        cardGlow.gameObject.SetActive(false);
        detailedCard.SetActive(false);

    }
    #endregion
}
