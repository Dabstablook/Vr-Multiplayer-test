using UnityEngine;

public class NoteMovement : MonoBehaviour
{
    public float speed = 6f;
    public Vector3 direction = Vector3.zero; // set by the spawner

    Vector3 startPosition;
    float spawnTime;

    void Start()
    {
        startPosition = transform.position;
        spawnTime = Time.time;
        if (direction == Vector3.zero) direction = transform.forward; // fallback
    }

    void Update()
    {
        transform.position = startPosition + direction * speed * (Time.time - spawnTime);
    }
}