using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Unity.Netcode;

public class GameController : NetworkBehaviour {
    public static GameController instance;
    public Queue<BodyPart> poolOfBodyParts = new Queue<BodyPart>();

    private void Awake() {
        instance = this;
        SnakeGridArea.instance.snakeDead += PlayerDead;
        SnakeGridArea.instance.gameOver += GameOverServerRpc;
    }

    [ServerRpc]
    void GameOverServerRpc() {
        if(IsOwner) {
            Debug.Log("1");
            UIManager.instance.GameOver();
            NetworkManager.Singleton.Shutdown();
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
        Debug.Log("34");
        UIManager.instance.GameOver();
        if(client.IsOwner) {
            DisconnectServerRpc(new ServerRpcParams());
        }
    }

    [ServerRpc]
    public void DisconnectServerRpc(ServerRpcParams serverRpcParams) {
        Debug.Log("3");
        NetworkManager.Singleton.DisconnectClient(serverRpcParams.Receive.SenderClientId);
        //Destroy(networkObject.gameObject);
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
