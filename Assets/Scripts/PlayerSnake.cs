using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Linq;
using Unity.Netcode;

public class PlayerSnake : BodyPart {
    [SerializeField] Vector2 direction = Vector2.right;
    [SerializeField] BodyPart bodySpritePrefab;
    [SerializeField] List<BodyPart> bodyParts = new List<BodyPart>();
    [SerializeField] List<string> actionNames;
    [SerializeField] InputAction inputAction;
    [SerializeField] float bodyOffset = 1;
    [SerializeField] bool move;
    const string Apple = "Apple";
    const string BodyPart = "BodyPart";
    [SerializeField] float raycastDistance = 1f;
    BodyPart bodyT;
    public delegate void PlayerHit(PlayerSnake snake,Transform target,PlayerLostReason reason);
    public PlayerHit playerHit;

    private void Awake() {
        //SnakeGridArea.instance.GetGridAreaBounds += (x => gridBounds = x);
        SnakeGridArea.instance.snakeDead += PlayerDead;
        SnakeGridArea.instance.PlayerCreated(this);
        InputBindings();
        bodyParts.Add(this);
    }

    private void Update()
    {
        Debug.DrawRay(transform.position, direction * raycastDistance, Color.green);
        //CheckHit();
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

    public Vector2 GetAreaOfSnake() {
        Vector2 area = Vector2.zero;
        for(int i = 0; i < bodyParts.Count; i++) {
            area += Vector2.one; //1 unit per sqaure
        }
        return area;
    }

    [ServerRpc]
    void GrowServerRpc(ServerRpcParams serverRpcParams) {
        var body = GameController.instance.GetPlayerBody();
        if(body == null) {
            body = Instantiate<BodyPart>(bodySpritePrefab, bodyParts.Last().transform.position + ((new Vector3(direction.x, direction.y, 0) * -1)), transform.rotation);
            body.networkObject.SpawnWithOwnership(serverRpcParams.Receive.SenderClientId);
        }
        body.gameObject.SetActive(true);
        if(IsOwner) {
            bodyParts.Add(body);
        } else {
            AddBodyClientRpc(body.NetworkObjectId.ToString());
        }
        Debug.Log("server rpc" + OwnerClientId);
        SnakeGridArea.instance.CheckAreaCoverage();
    }

    [ClientRpc]
    void AddBodyClientRpc(string message) {
        ulong s = ulong.Parse(message);
        var obj = GetNetworkObject(s);
        //Debug.Log("messae" + s +" " + OwnerClientId);
        if(obj.IsOwner) {
            bodyParts.Add(obj.GetComponent<BodyPart>());
            //Debug.Log("it is added "+OwnerClientId);
        }
    }

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

    void CheckHit() {
        //if(IsOwner) {
        //    var hit = Physics2D.Raycast(transform.position, direction, raycastDistance);
        //    if(hit.collider != null) {

        //    }
        //}
        var body = collider.GetComponent<BodyPart>();
        if(body != null && body.transform.tag == BodyPart) {
            if(bodyParts.Contains(body)) {
                playerHit?.Invoke(this, collider.transform, PlayerLostReason.SelfHit);
            } else {
                playerHit?.Invoke(this, collider.transform, PlayerLostReason.HitOther);
            }
        } else {
            //do nothing
        }
    }

    void InputBindings() {
        PlayerSnakeActions inputActions = new PlayerSnakeActions();
        foreach(var actn in inputActions) {
            actionNames.Add(actn.name);
        }
        //string randomActionName = actionNames[Random.Range(0, actionNames.Count)];
        inputAction = inputActions.FindAction(actionNames[1]);
        inputAction.performed += ActionPerformed;
        inputAction.Enable();
    }

    private void ActionPerformed(InputAction.CallbackContext obj) {
        Vector2 dir = obj.ReadValue<Vector2>();
        if((-1 * direction) != dir) {
            direction = dir;
        }
    }

    public void PlayerDead(PlayerLostReason reason, PlayerSnake snake) {
        if(snake == this) {
            Debug.LogError("player died" + reason);
            bodyParts.Remove(this);
            gameObject.SetActive(false);
        }
    }

    private void OnDisable() {
        SnakeGridArea.instance.snakeDead -= PlayerDead;
        inputAction.performed -= ActionPerformed;
    }
}
