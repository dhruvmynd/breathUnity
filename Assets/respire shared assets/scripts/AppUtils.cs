using UnityEngine;

public class AppUtils : MonoBehaviour
{

    public GameObject fadeToWhiteVolume;
    public GameObject fadeToBlackVolume;

    public static void QuitApplication()
    {
        Application.Quit();
    }

    public static void SetFrameRate(int frameRate)
    {
        if (frameRate > 0)
        {
            Application.targetFrameRate = frameRate;
        }
    }

    public static void SetFrameRateFromString(string frameRateString)
    {
        if (int.TryParse(frameRateString, out int frameRate) && frameRate > 0)
        {
            SetFrameRate(frameRate);
        }
    }

    public void FadeToWhiteOrBlack(bool fadeToWhite){
        fadeToWhiteVolume.SetActive(fadeToWhite);
        fadeToBlackVolume.SetActive(!fadeToWhite);
        
    }

    public void DisableAnimator(){
        var animator = GetComponent<Animator>();
        if (animator != null){
            animator.enabled = false;
        }
    }
}
