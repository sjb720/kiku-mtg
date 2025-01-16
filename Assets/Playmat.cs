using Mirror;
using TMPro;
using UnityEngine;
using System.Collections.Generic;
using System.Security.Cryptography;
using System;

public class Playmat : NetworkBehaviour
{

    // references
    public TMP_InputField colorInput,deckListInput,errorDisplay;
    public GameObject claimMattCanvas;

    // delimited by |
    [SyncVar]
    public string deckList;

    [SyncVar]
    public string commanders;

    [SyncVar(hook = nameof(SetClaimed))]
    public bool claimed;

    [SyncVar(hook = nameof(SetColor))]
    public Color matColor;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void ClaimMat()
    {
        CmdClaimMatt(colorInput.text, deckListInput.text);
    }

    [Command(requiresAuthority = false)]
    void CmdClaimMatt(string color, string deckListInput)
    {
        // get our mat color
        Color newCol = Color.white;
        ColorUtility.TryParseHtmlString(color, out newCol);

        // get our decklist
        var deckInfo = parseDecklist(deckListInput);
        deckInfo.decklist.Shuffle();


        commanders = string.Join("\n", deckInfo.commanders);
        deckList = string.Join("\n", deckInfo.decklist);
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

    
}


public static class DeckUtils
{
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