using UnityEngine;
using UnityEngine.UI;

public class CardInHand : MonoBehaviour
{

    public string card;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        GameManager.instance.RenderCardSprite(new ImageLoadRequest(card, GetComponent<Image>()));
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
