using System;
using System.Collections;
using System.IO;
using System.Linq;
using Mirror;
using Newtonsoft.Json.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class CardInPlay : NetworkBehaviour
{
    CameraController playerCam;

    // player states
    public bool hovering = false;
    public bool dragging = false;

    [SyncVar (hook = nameof(HandleRenderOrderChange))]
    int renderOrder = 0;

    // card states
    [SyncVar]
    public string card = "";

    [SyncVar]
    public bool tapped = false;
    
    [SyncVar]
    public bool flipped = false;

    [SyncVar (hook = nameof(HandleAltFaceShowingChange))]
    public bool altFaceShowing = false;

    [SyncVar]
    public bool token = false;

    GameObject freshCard;

    // animation values
    public float tapSpeedAnimation = 2f;

    // components
    SpriteRenderer spriteRenderer;

    // internal values
    
    Vector3 mouseDragPositionDifference = Vector3.zero;
    float rotLerp = 1;
    Quaternion lastRotation = Quaternion.Euler(0, 0, 0);

    // consts
    Quaternion TAPPED_ROT = Quaternion.Euler(0,0,-90);
    Quaternion UNTAPPED_ROT = Quaternion.Euler(0,0,0);
    string CARD_FACE_DIR_PATH;
    string CARD_IMAGE_PATH;

    // when our card spawns on the server set up its render order
    public override void OnStartServer()
    {
        base.OnStartServer();
        CmdSetRenderOnTop();
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CARD_FACE_DIR_PATH = Application.persistentDataPath + "/card-faces";
        CARD_IMAGE_PATH = CARD_FACE_DIR_PATH + "/" + card + ".png";

        freshCard = Resources.Load<GameObject>("CardInPlay");
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.Find("DebugText").GetComponent<TextMeshPro>().text = card;
        GameManager.instance.RenderCardSprite(new ImageLoadRequest(card, spriteRenderer));
    }

    // Update is called once per frame
    void Update()
    {

        if (NetworkClient.localPlayer != null)
        {
            playerCam = NetworkClient.localPlayer.gameObject.GetComponent<CameraController>();
        }

        if (playerCam == null)
        {
            return;
        }

        if (hovering)
        {
            // When the X key is pressed
            if(Input.GetKeyDown(KeyCode.X))
            {
                CmdClone();
                print("cloning card");
            }

            // The frame if LMB is pressed down...
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                dragging = true;
                mouseDragPositionDifference = playerCam.mousePos - transform.position;
                CmdSetRenderOnTop();
            }

            // If the player taps the card
            if (Input.GetKeyDown(KeyCode.T)) 
            {
                if (tapped)
                {
                    CmdUntap();
                } else
                {
                    CmdTap();
                }
            }

            if (Input.GetKeyDown(KeyCode.A))
            {
                CmdShowAltFace();
            }

        }

        if (dragging)
        {
            CmdSetPosition(playerCam.mousePos - mouseDragPositionDifference);

            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                dragging = false;
            }
        }


        // rotation update
        transform.rotation = Quaternion.Lerp(lastRotation, tapped ? TAPPED_ROT : UNTAPPED_ROT, tapSpeedAnimation * (1 - (1 - rotLerp) * (1 - rotLerp)));
        rotLerp += rotLerp > 1 ? 0 : Time.deltaTime;
    }

    private void OnMouseEnter()
    {
        hovering = true;
    }

    private void OnMouseExit()
    {
        hovering = false;
    }

    public void HandleAltFaceShowingChange(bool oldValue, bool isShowingAltFace)
    {
        if (isShowingAltFace)
        {
            transform.Find("AltFace").GetComponent<SpriteRenderer>().color = Color.white;
            GetComponent<SpriteRenderer>().color = Color.clear;
        } else
        {
            transform.Find("AltFace").GetComponent<SpriteRenderer>().color = Color.clear;
            GetComponent<SpriteRenderer>().color = Color.white;
        }
    }

    public void HandleRenderOrderChange(int oldValue, int newRenderOrder)
    {
        GetComponent<SpriteRenderer>().sortingOrder = newRenderOrder;
        transform.Find("AltFace").GetComponent<SpriteRenderer>().sortingOrder = newRenderOrder;
    }


    // Alt face
    [Command(requiresAuthority = false)]
    public void CmdShowAltFace()
    {
        altFaceShowing = !altFaceShowing;
    }

    // Tapping

    [Command(requiresAuthority =false)]
    public void CmdTap()
    {
        RpcTap();
    }

    [ClientRpc]
    public void RpcTap()
    {
        tapped = true;
        rotLerp = 0;
        lastRotation = transform.rotation;
    }

    // Untapping

    [Command(requiresAuthority = false)]
    public void CmdUntap()
    {
        RpcUntap();
    }

    [ClientRpc]
    public void RpcUntap()
    {
        tapped = false;
        rotLerp = 0;
        lastRotation = transform.rotation;
    }

    [Command(requiresAuthority = false)]
    public void CmdClone()
    {
        GameObject go = Instantiate(freshCard, transform.position - new Vector3(0.3f, 0.3f, 0f), transform.rotation);
        CardInPlay cip = go.GetComponent<CardInPlay>();
        
        // clone important token info:
        cip.card = card;
        cip.token = true;

        // spawn card on server for clients
        NetworkServer.Spawn(go);
    }

    [Command(requiresAuthority = false)]
    public void CmdSetPosition(Vector3 newPos)
    {
        transform.position = newPos;
    }

    [Command(requiresAuthority = false)]
    public void CmdSetRenderOnTop()
    {
        // increment our highest render order and grab the number
        GameManager.instance.highestRenderOrder += 1;
        renderOrder = GameManager.instance.highestRenderOrder;
    }

}
