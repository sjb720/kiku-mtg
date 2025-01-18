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

    // card states
    [SyncVar]
    public string card = "Agatha of the Vile Cauldron";
    
    [SyncVar]
    public bool tapped = false;
    
    [SyncVar]
    public bool flipped = false;
    
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


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        CARD_FACE_DIR_PATH = Application.persistentDataPath + "/card-faces";
        CARD_IMAGE_PATH = CARD_FACE_DIR_PATH + "/" + card + ".png";

        freshCard = Resources.Load<GameObject>("CardInPlay");
        spriteRenderer = GetComponent<SpriteRenderer>();
        transform.Find("DebugText").GetComponent<TextMeshPro>().text = card;

        GetCardImage();
    }

    void GetCardImage()
    {
        Debug.Log("getting card image for "+card);

        // create card cache if doesn't exist
        if (!Directory.Exists(CARD_FACE_DIR_PATH))
        {
            Directory.CreateDirectory(CARD_FACE_DIR_PATH);
        }

        // if card image doesn't exist, create it
        if (File.Exists(CARD_IMAGE_PATH))
        {
            LoadCardImageFromDisk();
        }
        else
        {
            Debug.Log("card file does not exist at " + CARD_IMAGE_PATH);
            GameManager.instance.AddToImageQueue(new ImageLoadRequest(card, spriteRenderer));
        }
    }

    void LoadCardImageFromDisk()
    {
        if (!File.Exists(CARD_IMAGE_PATH))
        {
            Debug.LogError("missing texture for card " + card);
            return;
        }

        Debug.Log("found texture for "+card);

        byte[] textureBytes = File.ReadAllBytes(CARD_IMAGE_PATH);
        Texture2D loadedTexture = new Texture2D(0,0);
        loadedTexture.LoadImage(textureBytes);
        ApplyTexture(loadedTexture);

    }

    void ApplyTexture(Texture2D texture)
    {
        GetComponent<SpriteRenderer>().sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        transform.Find("DebugText").gameObject.SetActive(false);
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

            // If LMB is held down...
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                dragging = true;
                mouseDragPositionDifference = playerCam.mousePos - transform.position;
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

            spriteRenderer.color = Color.red;
        } else
        {
            spriteRenderer.color = Color.white;
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

}
