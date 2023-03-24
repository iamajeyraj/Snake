using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;

public class PlayerSnake : BodyPart {
    [SerializeField] Vector2 direction = Vector2.right;
    [SerializeField] BodyPart bodySpritePrefab;
    [SerializeField] List<BodyPart> bodyParts = new List<BodyPart>();
    [SerializeField] List<string> actionNames;
    [SerializeField] InputAction inputAction;
    [SerializeField] float bodyOffset = 1;
    [SerializeField] bool move;
    [SerializeField] float raycastDistance = 1f;
    const string Apple = "Apple";
    const string BodyPart = "BodyPart";

    public delegate void PlayerHit(PlayerSnake snake,Transform target,PlayerLostReason reason);
    public PlayerHit playerHit;

    private void Awake() {
        SnakeGridArea.instance.snakeDead += PlayerDead; 
        SnakeGridArea.instance.PlayerCreated(this);
        InputBindings();
        bodyParts.Add(this);
    }

    private void Update()
    {
        Debug.DrawRay(transform.position, direction * raycastDistance, Color.green);
    }

    private void FixedUpdate() {
        for(int i = bodyParts.Count - 1; i > 0; i--) {
            bodyParts[i].transform.position = bodyParts[i - 1].transform.position;
        }
        if(networkObject.IsOwner) {
            Vector3 move = new Vector3(Mathf.Round(transform.position.x) + direction.x, Mathf.Round(transform.position.y) + direction.y, transform.position.z);
            this.transform.position = move;
        }
    }

    //Get total area covered by snake 
    public Vector2 GetAreaOfSnake() {
        Vector2 area = Vector2.zero;
        for(int i = 0; i < bodyParts.Count; i++) {
            area += Vector2.one; //1 unit per sqaure
        }
        return area;
    }

    [ServerRpc]
    void GrowServerRpc(ServerRpcParams serverRpcParams) {
        var body = GameController.instance.GetPlayerBody(serverRpcParams.Receive.SenderClientId);   //object pooling                                                                                                   
        if(body == null) {
            body = Instantiate<BodyPart>(bodySpritePrefab, bodyParts.Last().transform.position + ((new Vector3(direction.x, direction.y, 0) * -1)), transform.rotation);
            body.networkObject.SpawnWithOwnership(serverRpcParams.Receive.SenderClientId); //spawn with id
        }
        if(!body.networkObject.IsNetworkVisibleTo(serverRpcParams.Receive.SenderClientId)) {
            body.networkObject.NetworkShow(serverRpcParams.Receive.SenderClientId);
        }
        body.gameObject.SetActive(true);
        if(IsOwner) {  //if host or server
            bodyParts.Add(body);
        } else {
            AddBodyClientRpc(body.NetworkObjectId);  //if client
        }
        //Debug.Log("server rpc" + OwnerClientId);
        SnakeGridArea.instance.CheckAreaCoverage();  //check area covered
    }

    [ClientRpc]
    void AddBodyClientRpc(ulong id) {
        var obj = GetNetworkObject(id);  
        bodyParts.Add(obj.GetComponent<BodyPart>());
    }

    /// <summary>
    /// Get all body parts for pooling.
    /// Can be used by changing the ownership.
    /// </summary>
    /// <returns></returns>
    public List<BodyPart> GetAllBodyPart() {
        bodyParts.Remove(this);
        return bodyParts;
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        switch(collision.transform.tag) {
            case Apple:
                if(IsOwner)
                    GrowServerRpc(new ServerRpcParams());
                break;
            case BodyPart:
                CheckHit();
                break;
        }
    }

    /// <summary>
    /// Check to see where player got hit
    /// </summary>
    void CheckHit() {
        if(IsOwner) {
            var body = collider.GetComponent<BodyPart>();
            if(bodyParts.Contains(body)) {
                playerHit?.Invoke(this, collider.transform, PlayerLostReason.SelfHit);
            } else {
                playerHit?.Invoke(this, collider.transform, PlayerLostReason.HitOther);
            }
        }
    }

    /// <summary>
    /// Every player will get random input binding.
    /// Can add more/different bindings for player movement.
    /// </summary>
    void InputBindings() {
        PlayerSnakeActions inputActions = new PlayerSnakeActions();
        foreach(var actn in inputActions) {
            actionNames.Add(actn.name);   //get all action maps 
        }

        string randomActionName = actionNames[Random.Range(0, actionNames.Count)]; //getting random input binding for every player
        inputAction = inputActions.FindAction(randomActionName);
        inputAction.performed += ActionPerformed;
        inputAction.Enable();
    }

    private void ActionPerformed(InputAction.CallbackContext obj) {
        Vector2 dir = obj.ReadValue<Vector2>();
        if((-1 * direction) != dir) {
            direction = dir;     //changing the direction 
        }
    }

    public void PlayerDead(PlayerLostReason reason, PlayerSnake snake) {
        if(snake == this) {
            Debug.LogError("player died" + reason);
            bodyParts.Remove(this);
            if(snake.IsOwner)
                RemoveAllBodyOwnershipServerRpc(new ServerRpcParams());
            gameObject.SetActive(false);
        }
    }

    [ServerRpc]
    void RemoveAllBodyOwnershipServerRpc(ServerRpcParams p) {
        var obj = NetworkObject.NetworkManager.ConnectedClients[p.Receive.SenderClientId];
        var otherClients = NetworkObject.NetworkManager.ConnectedClientsIds.ToList().FindAll(x => x != p.Receive.SenderClientId);
        obj.OwnedObjects.ForEach(x => {
            otherClients?.ForEach(y => {
                x.NetworkHide(y);
            });
            x.RemoveOwnership();
        });
    }

    private void OnDisable() {
        SnakeGridArea.instance.snakeDead -= PlayerDead;
        inputAction.performed -= ActionPerformed;
    }
}
