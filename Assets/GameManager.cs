using Mirror;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public GameObject card;

    private void Update()
    {

        if (Input.GetKeyDown(KeyCode.L))
        {
            GameObject go = Instantiate(card);
            NetworkServer.Spawn(go, connectionToClient);
        }
       }
}
