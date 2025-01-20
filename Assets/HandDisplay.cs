using UnityEngine;

public class HandDisplay : MonoBehaviour
{
    public GameObject cardInHand;
    public GameObject handContentGameObject;

    void Start()
    {
        
    }

    void Update()
    {
        
    }

    // syncs the displayed cards with the card list sent here
    public void SyncDisplayedCards(string cards)
    {
        // destroy the cards in hand
        foreach (Transform child in handContentGameObject.transform)
        {
            Destroy(child.gameObject);
        }

        // spawn in each card per the card list we are showing in our hand
        var cardList = DeckUtils.DeserializeDeck(cards);
        foreach (var card in cardList)
        {
            var go = Instantiate(cardInHand, handContentGameObject.transform);
            go.GetComponent<CardInHand>().card = card;
        }
    }
}
