using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Networking;


public class ImageLoadRequest
{
    public ImageLoadRequest(string cardName, SpriteRenderer sr)
    {
        this.sr = sr;
        this.cardName = cardName;
    }

    public SpriteRenderer sr;
    public string cardName;
}

// game manager for local players, handles card loading and such
public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public Queue<ImageLoadRequest> imageLoadQueue = new Queue<ImageLoadRequest>();

    bool consumingImageQueue = false;

    private void Awake()
    {

        if(instance == null)
        {
            instance = this;
        }
     }

    public void AddToImageQueue(ImageLoadRequest imageRequest)
    {
        imageLoadQueue.Enqueue(imageRequest);

        if (!consumingImageQueue && imageLoadQueue.Count > 0) {
            Debug.Log("starting the image queue");
            consumingImageQueue = true;
            StartCoroutine(StartImageRequestLoop());
        }
    }

    IEnumerator StartImageRequestLoop()
    {
        Debug.Log("looping on queue...");
        var next = imageLoadQueue.Dequeue();
        yield return LoadCardImageFromWeb(next.cardName, next.sr);
        yield return new WaitForSeconds(1.5f);

        // recurse if we have another thing in the queue
        if (imageLoadQueue.Count > 0)
        {
            StartCoroutine(StartImageRequestLoop());
        }
        else
        {
            consumingImageQueue = false;
        }
    }

    IEnumerator LoadCardImageFromWeb(string card, SpriteRenderer cardFaceToApply)
    {
        string CARD_FACE_DIR_PATH = Application.persistentDataPath + "/card-faces";
        string CARD_IMAGE_PATH = CARD_FACE_DIR_PATH + "/" + card + ".png";

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
        string imageURI = token.SelectToken("image_uris.png").ToObject<string>();
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
            ApplyTexture(loadedTexture, cardFaceToApply);
            Debug.Log("got texture for " + card + "!");

            // save the sprite to disk now
            byte[] textureBytes = cardFaceToApply.sprite.texture.EncodeToPNG();
            File.WriteAllBytes(CARD_IMAGE_PATH, textureBytes);
            Debug.Log("successfully wrote " + card + " to disk!");
        }
    }

    void ApplyTexture(Texture2D texture, SpriteRenderer sr)
    {
        sr.sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        sr.transform.Find("DebugText").gameObject.SetActive(false);
    }
}
