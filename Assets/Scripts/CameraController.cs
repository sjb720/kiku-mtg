using System;
using Mirror;
using UnityEngine;

public class CameraController : NetworkBehaviour
{
    [SyncVar (hook = nameof(HandleHandChange))]
    public string hand = "";

    public float zoomSpeed = 2;
    public float panSpeed = 0.1f;
    public float panSpeedByZoomFactor = 1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Vector3 mousePosLastFrame = Vector3.zero;
    public Vector3 mouseDelta = Vector3.zero;
    public Vector3 mousePos = Vector3.zero;

    Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (!isLocalPlayer)
        {
            print("we are not local player, disabling camera");
            cam.enabled = false;
            transform.Find("HandDisplay").gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer) { return; }

        // mouse position tracking
        mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0;
        mouseDelta = mousePos - mousePosLastFrame;
        mousePosLastFrame = mousePos;

        cam.orthographicSize += cam.orthographicSize + Input.GetAxis("Mouse ScrollWheel") * zoomSpeed < 0 ? 0 : Input.GetAxis("Mouse ScrollWheel") * zoomSpeed;

        // Pan camera with middle click
        if (Input.GetKey(KeyCode.Mouse2))
        {
            CmdSetPosition(transform.position -= new Vector3(Input.mousePositionDelta.x * panSpeed * (cam.orthographicSize * panSpeedByZoomFactor), Input.mousePositionDelta.y * panSpeed * (cam.orthographicSize * panSpeedByZoomFactor), 0));
        }

        // Rotate camera to view better
        if (Input.GetKeyDown(KeyCode.R))
        {
            transform.Rotate(new Vector3(0, 0, 180));
        }
    }

    [Command]
    void CmdSetPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    [Command (requiresAuthority =false)]
    public void CmdAddCardToHand(string card)
    {
        Debug.Log("adding " + card + " to hand");
        var cards = DeckUtils.DeserializeDeck(hand);
        cards.Add(card);
        hand = DeckUtils.SerializeDeck(cards);
    }

    void HandleHandChange(string oldHand, string newHand)
    {
        // no need to update the display hand of someone that isnt us
        if (!isLocalPlayer)
        {
            return;
        }

        transform.Find("HandDisplay").GetComponent<HandDisplay>().SyncDisplayedCards(newHand);
    }
}
