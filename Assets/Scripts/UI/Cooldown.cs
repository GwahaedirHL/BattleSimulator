using UnityEngine;
using UnityEngine.Events;

public sealed class Cooldown : MonoBehaviour
{
    float startTime;
    float timeLeft;
    bool IsRunning { get; set; }

    public event UnityAction End;
    public event UnityAction<float> UpdateTime;

    public void ResetTimer()
    {
        End = null;
        UpdateTime = null;
        IsRunning = false;
    }

    public void StartTimer(float cooldownTime)
    {
        startTime = cooldownTime;
        timeLeft = cooldownTime;
        IsRunning = true;
    }

    void Update()
    {
        if (!IsRunning)
            return;       

        timeLeft -= Time.deltaTime;
        if (timeLeft <= 0f)
        {
            timeLeft = 0;
            IsRunning = false;
            End?.Invoke();
            return;
        }

        float normalizedLeft = timeLeft / startTime;
        UpdateTime?.Invoke(normalizedLeft); 
    }
}