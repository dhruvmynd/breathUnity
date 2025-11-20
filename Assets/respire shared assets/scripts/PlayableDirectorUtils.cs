using UnityEngine;
using UnityEngine.Playables;

public class PlayableDirectorUtils : MonoBehaviour
{
    public PlayableDirector director;

    void Start(){
        if(director == null){
            director = GetComponent<PlayableDirector>();
        }
        if (director == null)
        {
            director = FindAnyObjectByType<PlayableDirector>();
        }
    }
    public void PlayAt(float time)
    {
        director.time = time;
        director.Play();
    }

    public void PlayAtNormalizedTime(float normalizedTime)
    {
        director.time = director.duration * normalizedTime;
        director.Play();
    }
}


