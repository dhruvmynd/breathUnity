using UnityEngine;
using UnityEngine.UI;

public class UpdateTextWithTime : MonoBehaviour
{
    public Text text;  
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        text.text = Time.time.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        text.text = Time.time.ToString();
    }
}
