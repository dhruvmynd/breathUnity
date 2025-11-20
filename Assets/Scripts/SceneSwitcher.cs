  using UnityEngine;
     using UnityEngine.SceneManagement;
     using UnityEngine.UI;
     using TMPro;
     using System.Collections;

     namespace SFUBreathing
     {
         /// <summary>
         /// Scene switcher that toggles between two scenes while maintaining UI      persistence.
         /// Attach this to a GameObject with a UI Button to switch between scenes.
         /// </summary>
         public class SceneSwitcher : MonoBehaviour
         {
             // Scene names
             private const string SCENE_1 = "0422 good viz";
             private const string SCENE_2 = "0707 red light";

                      // Singleton instance
         private static SceneSwitcher instance;

         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
         private static void Initialize()
         {
             // Ensure singleton exists when runtime starts
             if (instance == null)
             {
                 GameObject go = new GameObject("SceneSwitcher");
                 instance = go.AddComponent<SceneSwitcher>();
             }
         }

                      // UI References
             [Header("UI References")]
             [SerializeField] private Button switchButton;
             [SerializeField] private TextMeshProUGUI buttonText;

             [Header("Scene Switch Settings")]
             [SerializeField] private float fadeTime = 0.5f;
             [SerializeField] private bool useAsyncLoading = true;

             // Current scene tracking
             private string currentSceneName;
             private bool isTransitioning = false;

             // Canvas group for fading (optional)
             private CanvasGroup canvasGroup;

             private void Awake()
             {
                 // Singleton pattern
                 if (instance != null && instance != this)
                 {
                     Destroy(gameObject);
                     return;
                 }

                 instance = this;
                 DontDestroyOnLoad(gameObject);

                 // Cache references
                 if (switchButton == null)
                 {
                     switchButton = GetComponentInChildren<Button>();
                 }

                 if (buttonText == null && switchButton != null)
                 {
                     buttonText = switchButton.GetComponentInChildren<TextMeshProUGUI>();        
                 }

                 canvasGroup = GetComponentInParent<CanvasGroup>();

                 // Initialize
                 currentSceneName = SceneManager.GetActiveScene().name;
                 UpdateButtonText();
             }

             private void Start()
             {
                 // Add button listener
                 if (switchButton != null)
                 {
                     switchButton.onClick.RemoveAllListeners();
                     switchButton.onClick.AddListener(ToggleScene);
                 }
             }

             private void OnEnable()
             {
                 SceneManager.sceneLoaded += OnSceneLoaded;
             }

             private void OnDisable()
             {
                 SceneManager.sceneLoaded -= OnSceneLoaded;
             }

             private void OnDestroy()
             {
                 SceneManager.sceneLoaded -= OnSceneLoaded;
             }

             private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
             {
                 currentSceneName = scene.name;
                 UpdateButtonText();
             }

             /// <summary>
             /// Toggle between the two scenes
             /// </summary>
             public void ToggleScene()
             {
                 if (isTransitioning)
                 {
                     Debug.LogWarning("Scene transition already in progress!");       
                     return;
                 }

                 string targetScene = (currentSceneName == SCENE_1) ? SCENE_2 :       
     SCENE_1;

                 if (useAsyncLoading)
                 {
                     StartCoroutine(LoadSceneAsync(targetScene));
                 }
                 else
                 {
                     LoadSceneImmediate(targetScene);
                 }
             }

             /// <summary>
             /// Load scene immediately
             /// </summary>
             private void LoadSceneImmediate(string sceneName)
             {
                 try
                 {
                     isTransitioning = true;
                     SceneManager.LoadScene(sceneName);
                 }
                 catch (System.Exception e)
                 {
                     Debug.LogError($"Failed to load scene '{sceneName}': {e.Message}");
                     isTransitioning = false;
                 }
             }

             /// <summary>
             /// Load scene asynchronously with optional fade
             /// </summary>
             private IEnumerator LoadSceneAsync(string sceneName)
             {
                 isTransitioning = true;

                 // Disable button during transition
                 if (switchButton != null)
                 {
                     switchButton.interactable = false;
                 }

                 // Fade out if canvas group is available
                 if (canvasGroup != null && fadeTime > 0)
                 {
                     yield return StartCoroutine(Fade(1f, 0f));
                 }

                 // Start async load
                 AsyncOperation asyncLoad =
     SceneManager.LoadSceneAsync(sceneName);

                 if (asyncLoad == null)
                 {
                     Debug.LogError($"Failed to start async load for scene '{sceneName}'");
                     isTransitioning = false;
                     if (switchButton != null)
                     {
                         switchButton.interactable = true;
                     }
                     yield break;
                 }

                 // Wait for scene to load
                 while (!asyncLoad.isDone)
                 {
                     yield return null;
                 }

                 // Fade in
                 if (canvasGroup != null && fadeTime > 0)
                 {
                     yield return StartCoroutine(Fade(0f, 1f));
                 }

                 // Re-enable button
                 if (switchButton != null)
                 {
                     switchButton.interactable = true;
                 }

                 isTransitioning = false;
             }

             /// <summary>
             /// Fade canvas group alpha
             /// </summary>
             private IEnumerator Fade(float startAlpha, float endAlpha)
             {
                 float elapsedTime = 0f;
                 canvasGroup.alpha = startAlpha;

                 while (elapsedTime < fadeTime)
                 {
                     elapsedTime += Time.deltaTime;
                     float t = elapsedTime / fadeTime;
                     canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
                     yield return null;
                 }

                 canvasGroup.alpha = endAlpha;
             }

             /// <summary>
             /// Update button text to show current scene
             /// </summary>
             private void UpdateButtonText()
             {
                 if (buttonText != null)
                 {
                     string displayText = "";

                     if (currentSceneName == SCENE_1)
                     {
                         displayText = "Switch to Red Light";
                     }
                     else if (currentSceneName == SCENE_2)
                     {
                         displayText = "Switch to Good Viz";
                     }
                     else
                     {
                         displayText = "Switch Scene";
                     }

                     buttonText.text = displayText;
                 }
             }

             /// <summary>
             /// Get the current scene name
             /// </summary>
             public string GetCurrentSceneName()
             {
                 return currentSceneName;
             }

             /// <summary>
             /// Check if a scene transition is in progress
             /// </summary>
             public bool IsTransitioning()
             {
                 return isTransitioning;
             }
         }
     }