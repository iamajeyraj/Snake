using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameController : NetworkBehaviour {
    public static GameController instance;
    public Queue<BodyPart> poolOfBodyParts = new Queue<BodyPart>();

    public delegate void PlayerDisconnected(ulong id);
    public PlayerDisconnected playerDisconnected;

    private void Awake() {
        instance = this;
        SnakeGridArea.instance.snakeDead += PlayerDead;
        SnakeGridArea.instance.gameOver += GameOverServerRpc;
    }

    private void Start() {
        NetworkManager.Singleton.OnClientDisconnectCallback += Singleton_OnClientDisconnectCallback;
    }

    private void Singleton_OnClientDisconnectCallback(ulong obj) {
        playerDisconnected?.Invoke(obj);
    }

    [ServerRpc(RequireOwnership = false)]
    void GameOverServerRpc() {
        StartCoroutine(WaitAndShutdown());
    }

    IEnumerator WaitAndShutdown() {
        yield return new WaitForSeconds(0.2f);
        NetworkManager.Singleton.Shutdown();
    }

    public void StartGame(PlayMode playMode) {
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
    }

    public void PlayerDead(PlayerLostReason reason, PlayerSnake snake) {
        switch(reason) {
            case PlayerLostReason.OutOfBounds:
                break;
            case PlayerLostReason.HitOther:
                break;
        }
        var bodyParts = snake.GetAllBodyPart();
        foreach(BodyPart b in bodyParts) {
            b.gameObject.SetActive(false);
            poolOfBodyParts.Enqueue(b);
        }

        if(snake.IsOwner) {
            StartCoroutine(WaitAndDisconnect(snake.networkObject));
        }
    }

    IEnumerator WaitAndDisconnect(NetworkObject client) {
        yield return new WaitForEndOfFrame();
        UIManager.instance.GameOver();
        DisconnectServerRpc(new ServerRpcParams());
    }

    [ServerRpc(RequireOwnership = false)]
    public void DisconnectServerRpc(ServerRpcParams serverRpcParams) {
        NetworkManager.Singleton.DisconnectClient(serverRpcParams.Receive.SenderClientId);
    }

    public BodyPart GetPlayerBody() {
        return poolOfBodyParts.Count > 0 ? poolOfBodyParts.Dequeue() : null;
    }
}

public enum PlayerLostReason {
    OutOfBounds,
    SelfHit,
    HitOther
}
