using UnityEngine;

public class CopyTransform : MonoBehaviour
{
    public void Copy(Transform other){
        transform.position = other.position;
        transform.rotation = other.rotation;
        transform.localScale = other.localScale;

    }

}