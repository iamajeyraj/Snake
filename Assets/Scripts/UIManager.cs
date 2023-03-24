using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour {
    [SerializeField] GameObject buttonPanel;
    [SerializeField] Button client;
    [SerializeField] Button host;
    [SerializeField] Text WinnerText;
    string gameLostMsg = "Game Over : You lost";
    [SerializeField] InputField playerNameTextField;
    string playerName; 

    public static UIManager instance;

    private void Awake() {
        instance = this;
        WinnerText.text = "";
    }

    public void ServerConnection() {
        GameController.instance.StartGame(PlayMode.Server);
        DisableUI();
    }

    public void HostConnection() {
        GameController.instance.StartGame(PlayMode.Host);
        DisableUI();
    }

    public void ClientConnection() {
        GameController.instance.StartGame(PlayMode.Client);
        DisableUI();
    }

    void DisableUI() {
        buttonPanel.SetActive(false);
        client.interactable = false;
        host.interactable = false;
    }

    public void GameOver() {
        buttonPanel.SetActive(true);
        WinnerText.text = gameLostMsg;
    }
}

public enum PlayMode {
    Server,
    Host,
    Client
}