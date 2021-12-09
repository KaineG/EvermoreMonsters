using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.UI;


/*
 * World Mode use case/purpose:
 * *Freeroam: Player running around the world and exploring the environment
 * *SelectSkillPos: A combat mode, used when a player has selected a skill 
 *      and is deciding where on the map to use it
 * *ChooseCard: Unused I think, check back and remove this one if unused
 * *ExecutePlayerActions: A combat mode, waits for all animations to be complete. 
 *      Can improve combat to not need this.
 * *EnemyTurn: A Combat mode. Has no control scheme. Enemies take their turn.
 * *NavigateMenus: A Combat mode. Navigate through the Action arc and UI buttons to choose actions.
 * *Wait: A mode that waits infinitely until something else changes the world mode. Used for all
 *      unspecified things that need a delay, but that are not specific combat actions.
 *      ex. camera movement
 */
public enum wMode { FREEROAM, SELECTSKILLPOS, CHOOSECARD, EXECUTEPLAYERACTIONS, ENEMYTURN, NAVIGATEMENUS, WAIT}

public class WorldMode : MonoBehaviour
{
    public PlayerControls controls;
    public Vector2 move;

    [SerializeField]
    public Canvas gameOverCanvas;

    #region Singleton
    public static WorldMode instance;

    void Awake()
    {
        controls = new PlayerControls();
        if (instance != null)
        {
            Debug.LogWarning("More than one instance of WorldMode found.");
            return;
        }
        instance = this;
    }
    #endregion

    [SerializeField]
    public wMode mode;

    public void Start()
    {
        //mode = wMode.FREEROAM;
        OnEnable();
    }

    public void Update()
    {

    }

    public void changeWorldMode(wMode newMode)
    {
        Debug.Log("Changing World Mode to:" + newMode);
        mode = newMode;
        OnEnable();
    }

    void OnEnable()
    {
        OnDisable();
        //State tranisitions go here:::::

        switch (mode)
        {
            case wMode.FREEROAM:
                controls.Freeroam.Enable();
                break;
            case wMode.SELECTSKILLPOS:
                controls.SelectSkillPos.Enable();
                break;
            //case wMode.CHOOSECARD:
              //  controls.ChooseCard.Enable();
                //break;

            //A "Do nothing" mode while character actions proceed
            case wMode.EXECUTEPLAYERACTIONS:
                break;
            //case wMode.ENEMYTURN:
                //no control scheme.
                //break;
            case wMode.NAVIGATEMENUS:
                controls.NavigateMenus.Enable();
                break;
            case wMode.ENEMYTURN:
                break;
        }
    }

    void OnDisable()
    {
        controls.Freeroam.Disable();

        controls.SelectSkillPos.Disable();

        controls.ChooseCard.Disable();

        controls.ChooseDialog.Disable();

        controls.NavigateMenus.Disable();

    }

    //shows the game over screen
    public IEnumerator GameOver()
    {
        gameOverCanvas.GetComponent<Animator>().Play("GameOver");
        yield return null;//wait a cycle
        yield return new WaitWhile(Overseer.instance.isOngoingActions);

        EventSystem.current.SetSelectedGameObject(gameOverCanvas.GetComponentInChildren<Button>().gameObject);
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game");
        Application.Quit();
    }

    public void reloadScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
