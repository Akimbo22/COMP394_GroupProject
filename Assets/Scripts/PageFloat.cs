using UnityEngine;

public class PageFloat : MonoBehaviour
{
    [Header("Floating Settings")]
    [SerializeField] private float floatHeight = 0.2f;
    [SerializeField] private float floatSpeed = 2f;

    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 90f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.localPosition;
    }

    private void Update()
    {
        float newY = startPosition.y +
            Mathf.Sin(Time.time * floatSpeed) * floatHeight;

        transform.localPosition = new Vector3(
            startPosition.x,
            newY,
            startPosition.z
        );

        transform.Rotate(
            Vector3.up,
            rotationSpeed * Time.deltaTime,
            Space.World
        );
    }
}
