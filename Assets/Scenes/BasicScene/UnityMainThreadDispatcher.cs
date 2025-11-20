using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// UnityMainThreadDispatcher - A singleton MonoBehaviour that allows execution of actions on Unity's main thread
/// from background threads or other contexts. This is essential for thread-safe Unity operations.
/// </summary>
public class UnityMainThreadDispatcher : MonoBehaviour
{
    #region Private Fields
    
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance = null;
    
    #endregion
    
    #region Public Static Methods
    
    /// <summary>
    /// Gets the singleton instance of UnityMainThreadDispatcher
    /// </summary>
    /// <returns>The singleton instance</returns>
    /// <exception cref="Exception">Thrown if no instance exists in the scene</exception>
    public static UnityMainThreadDispatcher Instance()
    {
        if (!Exists())
        {
            throw new Exception("UnityMainThreadDispatcher could not find the UnityMainThreadDispatcher object. " +
                              "Please ensure you have added the MainThreadExecutor Prefab to your scene.");
        }
        return _instance;
    }
    
    /// <summary>
    /// Checks if a UnityMainThreadDispatcher instance exists in the scene
    /// </summary>
    /// <returns>True if an instance exists, false otherwise</returns>
    public static bool Exists()
    {
        return _instance != null;
    }
    
    #endregion
    
    #region Unity Lifecycle Methods
    
    /// <summary>
    /// Initializes the singleton instance on Awake
    /// </summary>
    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(this);
        }
    }
    
    /// <summary>
    /// Processes queued actions on the main thread each frame
    /// </summary>
    void Update()
    {
        lock (_executionQueue)
        {
            while (_executionQueue.Count > 0)
            {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
    
    #endregion
    
    #region Public Methods
    
    /// <summary>
    /// Enqueues a coroutine to be executed on the main thread
    /// </summary>
    /// <param name="action">The coroutine to execute</param>
    public void Enqueue(IEnumerator action)
    {
        lock (_executionQueue)
        {
            _executionQueue.Enqueue(() => 
            {
                StartCoroutine(action);
            });
        }
    }
    
    /// <summary>
    /// Enqueues an action to be executed on the main thread
    /// </summary>
    /// <param name="action">The action to execute</param>
    public void Enqueue(Action action)
    {
        Enqueue(ActionWrapper(action));
    }
    
    #endregion
    
    #region Private Methods
    
    /// <summary>
    /// Wraps an Action in a coroutine for execution
    /// </summary>
    /// <param name="a">The action to wrap</param>
    /// <returns>A coroutine that executes the action</returns>
    IEnumerator ActionWrapper(Action a)
    {
        a();
        yield return null;
    }
    
    #endregion
}