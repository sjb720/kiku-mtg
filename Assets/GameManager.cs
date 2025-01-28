using System.Collections;
using System.Collections.Generic;
using System.IO;
using Mirror;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ImageLoadRequest
{
    public ImageLoadRequest(string cardName, SpriteRenderer sr)
    {
        this.sr = sr;
        this.cardName = cardName;
    }

    public ImageLoadRequest(string cardName, Image img)
    {
        this.img = img;
        this.cardName = cardName;
    }

    public Image img;
    public SpriteRenderer sr;
    public string cardName;
}

// game manager for local players, handles card loading and such
public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    public Queue<ImageLoadRequest> imageLoadQueue = new Queue<ImageLoadRequest>();

    bool consumingImageQueue = false;


    // the layer to set the next card we grab to
    [SyncVar]
    public int topCardLayer = 0;

    [SyncVar]
    public int highestRenderOrder;

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
        }
    }

    public string GetCardImageLocalPath(string card)
    {
        return GetCardFaceDirectory() + "/" + card + ".png";
    }

    public string GetCardFaceDirectory()
    {
        return Application.persistentDataPath + "/card-faces";
    }

    public void RenderCardSprite(ImageLoadRequest ilr)
    {
        var card = ilr.cardName;
        if (card == "")
        {
            Debug.LogError("card is empty string, cannot find card sprite");
            return;
        }

        Debug.Log("getting card image for " + card);

        // create card cache if doesn't exist
        if (!Directory.Exists(GetCardFaceDirectory()))
        {
            Directory.CreateDirectory(GetCardFaceDirectory());
        }

        // if card image doesn't exist, create it
        if (File.Exists(GetCardImageLocalPath(card)))
        {
            LoadCardImageFromDisk(ilr);
            LoadAltCardImageFromDisk(ilr);
        }
        else
        {
            Debug.Log("card file does not exist at " + GetCardImageLocalPath(card) + ", adding card to image queue");
            AddToImageQueue(ilr);
        }
    }

    public void AddToImageQueue(ImageLoadRequest imageRequest)
    {
        imageLoadQueue.Enqueue(imageRequest);

        if (!consumingImageQueue && imageLoadQueue.Count > 0) {
            Debug.Log("!! starting the image queue !!");
            consumingImageQueue = true;
            StartCoroutine(StartImageRequestLoop());
        }
    }

    IEnumerator StartImageRequestLoop()
    {
        while (imageLoadQueue.Count > 0) {
            var next = imageLoadQueue.Dequeue();
            Debug.Log("making a network request for card "+next.cardName);
            yield return LoadCardImageFromWeb(next);
            yield return new WaitForSeconds(1.5f);
        }
        Debug.Log("!! ending the image queue !!");
        consumingImageQueue = false;
    }

    void LoadCardImageFromDisk(ImageLoadRequest ilr)
    {
        if (!File.Exists(GetCardImageLocalPath(ilr.cardName)))
        {
            Debug.LogError("missing texture for card " + ilr.cardName);
            return;
        }

        Debug.Log("found texture for " + ilr.cardName);

        byte[] textureBytes = File.ReadAllBytes(GetCardImageLocalPath(ilr.cardName));
        Texture2D loadedTexture = new Texture2D(0, 0);
        loadedTexture.LoadImage(textureBytes);
        Sprite sprite = CreateCardSpriteFromTexture(loadedTexture);
        ApplySprite(sprite, ilr);
    }

    void LoadAltCardImageFromDisk(ImageLoadRequest ilr)
    {
        if (!File.Exists(GetCardImageLocalPath(ilr.cardName+"_alt")))
        {
            Debug.Log("no alt face found " + ilr.cardName);
            return;
        }

        Debug.Log("found alt face texture for " + ilr.cardName);

        byte[] textureBytes = File.ReadAllBytes(GetCardImageLocalPath(ilr.cardName+"_alt"));
        Texture2D loadedTexture = new Texture2D(0, 0);
        loadedTexture.LoadImage(textureBytes);
        Sprite sprite = CreateCardSpriteFromTexture(loadedTexture);
        ApplyAltSprite(sprite, ilr);
    }

    IEnumerator LoadCardImageFromWeb(ImageLoadRequest ilr)
    {
        string card = ilr.cardName;
        // get the card data from scryfall, ty scryfall :)
        UnityWebRequest cardDetailsRequest = UnityWebRequest.Get("https://api.scryfall.com/cards/named?fuzzy=" + card);
        yield return cardDetailsRequest.SendWebRequest();

        if (cardDetailsRequest.result != UnityWebRequest.Result.Success)
        {
            Debug.Log("failed to get card image url from scryfall");
            yield break;
        }

        // parse into our dto and ask for the png using the uri
        var text = cardDetailsRequest.downloadHandler.text;
        JToken token = JToken.Parse(text);
        string imageURI;
        string altImageURI = "";
        try
        {
            imageURI = token.SelectToken("image_uris.png").ToObject<string>();
        } catch
        {
            Debug.Log("failed to get image uris, is "+card +" a double sided card?");
            try
            {
                imageURI = token.SelectToken("card_faces[0].image_uris.png").ToObject<string>();
                altImageURI = token.SelectToken("card_faces[1].image_uris.png").ToObject<string>();
            } catch
            {
                Debug.Log("failed to get both faces, not sure what " + card + " is.");
                yield break;
            }
        }

        UnityWebRequest imageResponse = UnityWebRequestTexture.GetTexture(imageURI);
        yield return imageResponse.SendWebRequest();

        if (imageResponse.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("failed to get card image from web");
        }
            else
        {
            // set our texture then save to disk for max player enjoyment
            Debug.Log("got image data for " + card + "!");
            Texture2D loadedTexture = DownloadHandlerTexture.GetContent(imageResponse);
            Sprite sprite = CreateCardSpriteFromTexture(loadedTexture);
            ApplySprite(sprite, ilr);
            Debug.Log("got texture for " + card + "!");

            // save the sprite to disk now
            byte[] textureBytes = sprite.texture.EncodeToPNG();
            File.WriteAllBytes(GetCardImageLocalPath(card), textureBytes);
            Debug.Log("successfully wrote " + card + " to disk!");
        }

        // if we have an alternate image we must also grab we're not done yet...
        if (altImageURI != "")
        {
            UnityWebRequest altImageResponse = UnityWebRequestTexture.GetTexture(altImageURI);
            yield return altImageResponse.SendWebRequest();

            if (altImageResponse.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("failed to get card image from web");
            }
            else
            {
                // set our texture then save to disk for max player enjoyment
                Debug.Log("got image data for " + card + "(alt) !");
                Texture2D loadedTexture = DownloadHandlerTexture.GetContent(altImageResponse);
                Sprite sprite = CreateCardSpriteFromTexture(loadedTexture);
                ApplyAltSprite(sprite, ilr);
                Debug.Log("got texture for " + card + "!");

                // save the sprite to disk now
                byte[] textureBytes = sprite.texture.EncodeToPNG();
                File.WriteAllBytes(GetCardImageLocalPath(card+"_alt"), textureBytes);
                Debug.Log("successfully wrote " + card + " to disk!");
            }
        }
    }

    Sprite CreateCardSpriteFromTexture(Texture2D texture)
    {
        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }

    void ApplySprite(Sprite sprite, ImageLoadRequest ilr)
    {
        if (ilr.sr != null)
        {
            ilr.sr.sprite = sprite;
            ilr.sr.transform.Find("DebugText").gameObject.SetActive(false);
        }

        if (ilr.img != null)
        {
            ilr.img.sprite = sprite;
            ilr.img.transform.Find("cardname").gameObject.SetActive(false);
        }
    }

    void ApplyAltSprite(Sprite sprite, ImageLoadRequest ilr)
    {
        if (ilr.sr != null)
        {
            ilr.sr.transform.Find("AltFace").GetComponent<SpriteRenderer>().sprite = sprite;
        }

        if (ilr.img != null)
        {
            // TODO figure this out
        }
    }
}
