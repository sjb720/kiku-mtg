using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CardInHand : MonoBehaviour, IPointerClickHandler
{

    public string card;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        transform.Find("cardname").GetComponent<TMP_Text>().text = card;
        GameManager.instance.RenderCardSprite(new ImageLoadRequest(card, GetComponent<Image>()));
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        print("clicked!");
        print(eventData.button);
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            PlayCardToBattlefield();
        }
    }

    public void PlayCardToBattlefield()
    {
        CmdPlayCardToBattlefield(NetworkClient.localPlayer.gameObject);
    }


    public void CmdPlayCardToBattlefield(GameObject playerGameObject)
    {
        // send it to the battlefield
        GameObject freshCard = Resources.Load<GameObject>("CardInPlay");
        GameObject spawnedCard = Instantiate(freshCard, new Vector3(transform.root.position.x, transform.root.position.y, 0.0f), Quaternion.identity);
        spawnedCard.GetComponent<CardInPlay>().card = card;
        NetworkServer.Spawn(spawnedCard);

        // remove cards from hand
        var cardsInHand = DeckUtils.DeserializeDeck(transform.root.GetComponent<CameraController>().hand);
        cardsInHand.Remove(card);
        playerGameObject.GetComponent<CameraController>().hand = DeckUtils.SerializeDeck(cardsInHand);

    }
}
