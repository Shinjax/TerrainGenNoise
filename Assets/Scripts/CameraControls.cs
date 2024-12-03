using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 10f;
    public float rotationSpeed = 100f;
    public float heightAdjustSpeed = 5f;

    void Update()
    {
        // WASD Movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        transform.Translate(new Vector3(horizontal, 0, vertical) * moveSpeed * Time.deltaTime);

        // Q/E for height adjustment
        if (Input.GetKey(KeyCode.Q))
            transform.Translate(Vector3.down * heightAdjustSpeed * Time.deltaTime);
        if (Input.GetKey(KeyCode.E))
            transform.Translate(Vector3.up * heightAdjustSpeed * Time.deltaTime);

        // Mouse Look (hold right mouse button)
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

            transform.eulerAngles += new Vector3(-mouseY, mouseX, 0);
        }
    }
}