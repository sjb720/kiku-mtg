using Mirror;
using Mirror.BouncyCastle.Tls.Crypto.Impl.BC;
using UnityEngine;

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

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    // Update is called once per frame
    void Update()
    {
        if (playerCam == null)
        {
            playerCam = NetworkClient.localPlayer.gameObject.GetComponent<CameraController>();

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
        GameObject go = Instantiate(gameObject, transform.position - new Vector3(0.1f, 0.1f, 0f), transform.rotation);
        NetworkServer.Spawn(go, connectionToClient);
    }

    [Command(requiresAuthority = false)]
    public void CmdSetPosition(Vector3 newPos)
    {
        transform.position = newPos;
    }

}
