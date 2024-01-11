using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private NetworkManager netManager;
    private int playerTurn;
    private int noOfTurns;
    private Color activeColor = new Color(1.0f, 0.85f, 0f);
    
    public Button[] gridSpaces;
    public GameObject[] turnIndicators;
    public Sprite[] gameIcons;
    private int[] stateArray = new int[9];

    [Header("UI Elements")]
    public GameObject gridCanvas;
    public GameObject winBubble;
    public GameObject resetButton;
    public GameObject WaitingScreen; 
    
    public bool gameFinished; 

    public AudioSource winnerAudio;


    void Start()
    {
        InitializeBoard();

        netManager = MainMenu.playerType switch
        {
            PlayerType.HOST => new Server(this),
            PlayerType.CLIENT => new Client(this),
            _ => throw new ArgumentException("Invalid playerType")
        };  

        netManager.StartNetworkManager();
    }


    public void InitializeBoard()
    {
        playerTurn = 0;
        noOfTurns = 0;

        turnIndicators[0].GetComponent<TextMeshProUGUI>().color = activeColor;
        turnIndicators[1].GetComponent<TextMeshProUGUI>().color = Color.white;

        gridCanvas.SetActive(false);
        winBubble.SetActive(false);
        resetButton.SetActive(false);

        foreach (Button space in gridSpaces)
        {
            space.GetComponent<Image>().sprite = null;
            space.interactable = false;
            space.enabled = false;
        }

        for (int i = 0; i < stateArray.Length; i++)
        {
            stateArray[i] = -10;
        }
        
        gameFinished = false;
    }


    public void ExecuteMove(int tileNo)
    {
        gridSpaces[tileNo].image.sprite = gameIcons[playerTurn];
        gridSpaces[tileNo].interactable = false;
        stateArray[tileNo] = playerTurn + 1;
        noOfTurns++;

        gameFinished = false;
        if (noOfTurns > 4)
        {
            gameFinished = CheckForSolution();
        }

        int previousPlayerTurn = playerTurn;
        playerTurn = playerTurn == 1 ? 0 : 1;

        turnIndicators[playerTurn].GetComponent<TextMeshProUGUI>().color = activeColor;
        turnIndicators[previousPlayerTurn].GetComponent<TextMeshProUGUI>().color = Color.white;
    }


    bool CheckForSolution()
    {
        int[] solutionsAray = new int[stateArray.Length - 1];

        for (int i = 0; i < 3; i++)
        {
            solutionsAray[i] = stateArray[i * 3] + stateArray[i * 3 + 1] + stateArray[i * 3 + 2];
        }

        for (int i = 0; i < 3; i++)
        {
            solutionsAray[i + 3] = stateArray[i] + stateArray[i + 3] + stateArray[i + 6];
        }

        solutionsAray[6] = stateArray[2] + stateArray[4] + stateArray[6];
        solutionsAray[7] = stateArray[0] + stateArray[4] + stateArray[8];

        foreach (int element in solutionsAray)
        {
            if (element == (playerTurn + 1) * 3)
            {
                DisableBoard();

                winnerAudio.enabled = true;
                winnerAudio.Play();

                if (element == 3)
                    winBubble.GetComponentInChildren<TextMeshProUGUI>().text = "X player won";
                else
                    winBubble.GetComponentInChildren<TextMeshProUGUI>().text = "O player won";

                winBubble.SetActive(true);

                if (MainMenu.playerType == PlayerType.HOST)
                    resetButton.SetActive(true);

                return true;
            }
        }

        if (noOfTurns == 9)
        {
            winBubble.GetComponentInChildren<TextMeshProUGUI>().text = "Tie";
            winBubble.SetActive(true);

            if (MainMenu.playerType == PlayerType.HOST)
                resetButton.SetActive(true);
            return true;
        }
        
        return false;
    }


    public void BeginGame()
    {
        gridCanvas.SetActive(true);
        WaitingScreen.SetActive(false);
    }


    public void DisableBoard()
    {
        foreach (Button space in gridSpaces)
        {
            space.interactable = false;
            space.enabled = false;
        }
    }


    public void EnableBoard()
    {
        for (int i = 0; i < gridSpaces.Length; i++)
        {
            if (stateArray[i] == -10)
            {
                gridSpaces[i].interactable = true;
                gridSpaces[i].enabled = true;
            }
        }
    }


    public void AccessSendMove(int move)
    {
        netManager.SendMove(move);
    }

    public void AccessDispose()
    {
        netManager.Dispose();
    }

    public void Restart()
    {
        InitializeBoard();
        BeginGame();
        netManager.Restart();
    }


}
