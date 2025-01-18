using Mirror;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Security.Cryptography;
using System;
using System.Linq;

public class Playmat : NetworkBehaviour
{

    // resources
    GameObject freshCard;

    // references
    public TMP_InputField colorInput,deckListInput;
    public TMP_Text errorDisplay, libDisplay;
    public GameObject claimMattCanvas;

    // delimited by n. untouched original state of deck.
    [SyncVar]
    public string deckList;

    [SyncVar (hook = nameof(HandleLibraryChange))]
    public string library;

    [SyncVar]
    public string commanders;

    [SyncVar(hook = nameof(SetClaimed))]
    public bool claimed;

    [SyncVar(hook = nameof(SetColor))]
    public Color matColor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        freshCard = Resources.Load<GameObject>("CardInPlay");
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ClaimMat()
    {
        CmdClaimMatt(colorInput.text, deckListInput.text);
    }

    public void DrawCard()
    {
        CmdDrawCard();
    }

    [Command(requiresAuthority =false)]
    void CmdDrawCard()
    {
        var lib = DeckUtils.DeserializeDeck(library);

        if (lib.Count == 0)
        {
            return;
        }

        var card = lib[0];
        GameObject go = Instantiate(freshCard, Vector3.zero, transform.rotation);
        go.GetComponent<CardInPlay>().card = card;
        NetworkServer.Spawn(go);

        lib.RemoveAt(0);
        library = DeckUtils.SerializeDeck(lib);
    }

    [Command(requiresAuthority = false)]
    void CmdClaimMatt(string color, string deckListInput)
    {
        // get our mat color
        Color newCol = Color.white;
        ColorUtility.TryParseHtmlString(color, out newCol);

        // get our decklist
        var deckInfo = parseDecklist(deckListInput);
        deckList = DeckUtils.SerializeDeck(deckInfo.decklist);

        // shuffle the decklist and put it in our library
        deckInfo.decklist.Shuffle();
        library = DeckUtils.SerializeDeck(deckInfo.decklist);

        // take our commanders and 
        commanders = DeckUtils.SerializeDeck(deckInfo.commanders);
        
        matColor = newCol;
        claimed = true;
    }

    struct DeckInfo
    {
        public List<string> commanders;
        public List<string> decklist;
    }

    DeckInfo parseDecklist(string decklist)
    {
        List<string> cards = new List<string>();
        List<string> commanders = new List<string>();

        bool nextIsCommanders = false;

        string[] lines = decklist.Split("\n");
        foreach (string line in lines)
        {
            if (line.Length > 1)
            {
                // split into 2 substrings by space to get the number and card name
                string[] cardInfo = line.Split(new[] { ' ' }, 2);

                int count = int.Parse(cardInfo[0]);
                for (int i = 0; i < count; i++)
                {
                    if(nextIsCommanders)
                    {
                        commanders.Add(cardInfo[1]);
                    } else
                    {
                        cards.Add(cardInfo[1]);
                    }
                }

            } else
            {
                // parse commanders after we get an empty line
                nextIsCommanders = true;
            }
        }


        DeckInfo di = new DeckInfo();
        di.commanders = commanders;
        di.decklist = cards;
        return di;
    }

    void SetColor(Color o, Color n)
    {
        GetComponent<SpriteRenderer>().color = n;
    }

    void SetClaimed(bool o, bool n)
    {
        claimMattCanvas.SetActive(!n);
    }

    void HandleLibraryChange(string oldLibrary, string newLibrary) {
        libDisplay.text = newLibrary;
    }


}


public static class DeckUtils
{

    public static List<string> DeserializeDeck(string deckList)
    {
        var list = deckList.Split("\n");
        return list.OfType<string>().ToList();
    }

    public static string SerializeDeck(List<string> deckList)
    {
        return String.Join("\n", deckList);
    }

    public static void Shuffle<T>(this IList<T> list)
    {
        RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
        int n = list.Count;
        while (n > 1)
        {
            byte[] box = new byte[1];
            do provider.GetBytes(box);
            while (!(box[0] < n * (Byte.MaxValue / n)));
            int k = (box[0] % n);
            n--;
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
}