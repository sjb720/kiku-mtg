using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float zoomSpeed = 2;
    public float panSpeed = 0.1f;
    public float panSpeedByZoomFactor = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Vector3 mousePosLastFrame = Vector3.zero;
    public Vector3 mouseDelta = Vector3.zero;
    public Vector3 mousePos = Vector3.zero;

    Transform transform;
    Camera cam;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        transform = GetComponent<Transform>();
    }


    // Update is called once per frame
    void Update()
    {
        // mouse position tracking
        mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseDelta = mousePos - mousePosLastFrame;
        mousePosLastFrame = mousePos;

        cam.orthographicSize += cam.orthographicSize + Input.GetAxis("Mouse ScrollWheel") * zoomSpeed < 0 ? 0 : Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;

        if (Input.GetKey(KeyCode.Mouse2))
        {
            transform.position -= new Vector3(Input.mousePositionDelta.x * panSpeed * (cam.orthographicSize * panSpeedByZoomFactor), Input.mousePositionDelta.y * panSpeed * (cam.orthographicSize * panSpeedByZoomFactor), 0);
        }
    }
}
