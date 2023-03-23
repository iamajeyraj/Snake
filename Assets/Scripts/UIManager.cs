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
    //[SerializeField] GameObject targetPanel;
    //[SerializeField] Slider slider;
    [SerializeField] Text WinnerText;
    string gameLostMsg = "Game Over : You lost";
    string errorMsg = "Enter your name to start";
    [SerializeField] InputField playerNameTextField;
    string playerName; 

    public static UIManager instance;

    private void Awake() {
        instance = this;
        WinnerText.text = "";
    }

    public void ServerConnection() {
        StartGame(PlayMode.Server);
    }

    public void HostConnection() {
        StartGame(PlayMode.Host);
    }

    public void ClientConnection() {
        StartGame(PlayMode.Client);
    }

    void StartGame(PlayMode playMode) {

        //if(string.IsNullOrEmpty(playerNameTextField.text)) {
        //    WinnerText.text = errorMsg;
        //    return;
        //}

        switch(playMode) {
            case PlayMode.Server:
                NetworkManager.Singleton.StartServer();
                break;
            case PlayMode.Host:
                NetworkManager.Singleton.StartHost();
                break;
            case PlayMode.Client:
                NetworkManager.Singleton.StartClient();
                break;
        }
        DisableUI();
    }

    void DisableUI() {
        //playerNameTextField.text = "";
        buttonPanel.SetActive(false);
        //client.interactable = false;
        //host.interactable = false;
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