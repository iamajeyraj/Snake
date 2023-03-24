using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

public class SnakeGridArea : NetworkBehaviour {
    [SerializeField] BoxCollider2D collider;
    [SerializeField] List<PlayerSnake> players;

    public delegate void SnakeDead(PlayerLostReason reason, PlayerSnake snake);
    public SnakeDead snakeDead;

    public delegate void GetBounds(Bounds bounds);
    public GetBounds GetGridAreaBounds;

    public delegate void GameOver();
    public GameOver gameOver; //no players Left //close network session // screen size full

    public delegate void getRandomPointWithoutSnake(Action<Vector3> randomPoint);
    public getRandomPointWithoutSnake GetPointWithoutSnake;

    public static SnakeGridArea instance;

    private void Awake() {
        instance = this;
        GetPointWithoutSnake += GetRandomPointWithoutSnake;
    }

    private void Start() {
        GetGridAreaBounds?.Invoke(collider.bounds);
        GameController.instance.playerDisconnected += OnSnakeDisconnected;
    }

    private void OnSnakeDisconnected(ulong obj)
    {
        if(players.Count == 0) {
            gameOver?.Invoke(); //annonce game over
        }
    }

    /// <summary>
    /// Triggered when total area of the grid is covered 90%.
    /// Grid can be increased. Still all the logic including the snake and apple will work 
    /// </summary>
    public void CheckAreaCoverage() {
        GetGridAreaBounds?.Invoke(collider.bounds);
        var gridArea = transform.localScale.x * transform.localScale.y;

        Vector2 totalSnakeArea = Vector3.zero;
        players.ForEach(x => {
            totalSnakeArea += x.GetAreaOfSnake();
        });
        var snakeArea = totalSnakeArea.x * totalSnakeArea.y;

        float coveragePercentage = (snakeArea / gridArea) * 100;
        if(coveragePercentage > 90) {
            gameOver?.Invoke();
        }
    }

    /// <summary>
    /// Get random point without snake in it
    /// </summary>
    /// <param name="callback"></param>
    public void GetRandomPointWithoutSnake(Action<Vector3> callback) {
        bool withinSnakeBound = false;
        while(true) {
            var random = GetRandomPointWithinBounds();
            foreach(BodyPart body in players) {
                if(body.CheckPointWithinBounds(random)) {
                    withinSnakeBound = true;
                }
            }
            if(!withinSnakeBound) {
                callback?.Invoke(random);
                return;
            }
        }
    }

    /// <summary>
    /// Get random point within grid
    /// </summary>
    /// <returns></returns>
    public Vector2 GetRandomPointWithinBounds() {
        if(collider != null) {
            float x = UnityEngine.Random.Range(collider.bounds.min.x, collider.bounds.max.x);
            float y = UnityEngine.Random.Range(collider.bounds.min.y, collider.bounds.max.y);
            return new Vector2(Mathf.Round(x), Mathf.Round(y));
        }
        return Vector3.zero;
    }

    public void PlayerCreated(PlayerSnake player) {
        if(!players.Contains(player)) {
            players.Add(player);
            player.playerHit += PlayerHit;
        }
    }

    public void PlayerHit(PlayerSnake snake, Transform target, PlayerLostReason reason) {
        //Debug.LogFormat("player hit {0}" + reason);
        AnnouncePlayerLost(snake, reason);
    }

    /// <summary>
    /// Check for player Out of bounds in Grid.
    /// </summary>
    private void FixedUpdate() {
        for(int i = 0; i < players.Count; i++) {
            var snake = players[i];
            if(snake != null && !collider.bounds.Contains(snake.transform.position)) {
                AnnouncePlayerLost(snake, PlayerLostReason.OutOfBounds);
            }
        }
    }

    void AnnouncePlayerLost(PlayerSnake snake, PlayerLostReason reason) {
        players.Remove(snake);
        snakeDead?.Invoke(reason, snake);
        //if(players.Count == 0) {
        //    gameOver?.Invoke();
        //} else {
        //    Debug.LogError("player dead");
        //}
        //snake.networkObject.Despawn(true);
    }
}
