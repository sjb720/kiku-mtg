using Mirror.BouncyCastle.Tls.Crypto.Impl.BC;
using UnityEngine;

public class CardInPlay : MonoBehaviour
{

    CameraController playerCam;

    // player states
    public bool hovering = false;
    public bool dragging = false;

    // card states
    public string card = "Agatha of the Vile Cauldron";
    public bool tapped = false;
    public bool flipped = false;
    public bool token = false;

    // animation values
    public float tapSpeedAnimation = 2f;

    // components
    SpriteRenderer spriteRenderer;

    // internal values
    
    float rotLerp = 1;
    Quaternion lastRotation = Quaternion.Euler(0, 0, 0);

    // consts
    Quaternion TAPPED_ROT = Quaternion.Euler(0,0,-90);
    Quaternion UNTAPPED_ROT = Quaternion.Euler(0,0,0);

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        playerCam = Camera.main.GetComponent<CameraController>();
    }

    // Update is called once per frame
    void Update()
    {

        if (hovering)
        {
            // When the X key is pressed
            if(Input.GetKeyDown(KeyCode.X))
            {
                Clone();
            }

            // If LMB is held down...
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                dragging = true;
            }

            // If the player taps the card
            if ( Input.GetKeyDown(KeyCode.T)) 
            {
                tapped = !tapped;
                rotLerp = 0;
                lastRotation = transform.rotation;
            }

            spriteRenderer.color = Color.red;
        } else
        {
            spriteRenderer.color = Color.white;
        }

        if (dragging)
        {
            transform.position += new Vector3(playerCam.mouseDelta.x, playerCam.mouseDelta.y, 0);

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

    public void Tap()
    {
        tapped = true;
    }

    public void Untap()
    {
        tapped = false;
    }

    public void Clone()
    {
        Instantiate(gameObject, transform.position - new Vector3(0.1f, 0.1f, 0f), transform.rotation);
    }

}
