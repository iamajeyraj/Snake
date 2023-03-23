using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ApplePlacer : MonoBehaviour {
    Bounds bounds;

    private void Awake() {
        SnakeGridArea.instance.GetGridAreaBounds += (x => bounds = x);
    }

    private void Start() {
        PositionRandomly();
    }

    private void OnTriggerEnter2D(Collider2D collision) {
        PositionRandomly();
    }

    void PositionRandomly() {
        SnakeGridArea.instance.GetRandomPointWithoutSnake((point)=> {
            transform.position = point;
        });
    }
}
