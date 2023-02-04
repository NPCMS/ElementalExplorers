using UnityEngine;

public class Keyboard : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < gameObject.transform.childCount; i++) {
            GameObject child = gameObject.transform.GetChild(i).gameObject;
            if (child.name == "AlphaNum")
            {
                for (int j = 0; j < child.transform.childCount; j++)
                {
                    GameObject letter = child.transform.GetChild(j).gameObject;
                    letter.AddComponent<AlphaNumInteraction>();
                }
                break;
            }
        }
    }
}
