using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher instance;
    private readonly Queue<System.Action> actions = new Queue<System.Action>();

    public static UnityMainThreadDispatcher Instance()
    {
        if (instance == null)
        {
            instance = new GameObject("MainThreadDispatcher").AddComponent<UnityMainThreadDispatcher>();
            DontDestroyOnLoad(instance.gameObject);
        }
        return instance;
    }

    public void Enqueue(System.Action action)
    {
        lock (actions)
        {
            actions.Enqueue(action);
        }
    }

    private void Update()
    {
        while (actions.Count > 0)
        {
            System.Action action = null;
            lock (actions)
            {
                if (actions.Count > 0)
                {
                    action = actions.Dequeue();
                }
            }
            action?.Invoke();
        }
    }
}