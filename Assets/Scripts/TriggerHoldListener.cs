using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Triggers UnityEvent when a controller is inside the trigger for the specified time.
/// Attach this component to a GameObject with a collider configured to be a trigger.
/// There is also visual feedback of pushing the attached button.
/// Listeners for the event can be added in the inspector.
/// </summary>
public class TriggerHoldListener : MonoBehaviour
{
    private bool pressed = false;
    private Button button;
    private float lastPressTime = 0;

    [SerializeField]
    float triggerTreshhold = 1f;
    [SerializeField]
    UnityEvent triggerEnterEvent;

    private void Start()
    {
        button = GetComponent<Button>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.name.Equals("SelectPoint")) return;
        pressed = true;
        button.interactable = false;
        lastPressTime = Time.time;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.name.Equals("SelectPoint") || !pressed) return;
        pressed = false;
        button.interactable = true;
        lastPressTime = 0;
    }

    void FixedUpdate()
    {
        if (!pressed  || lastPressTime == 0 
            || Time.time - lastPressTime < triggerTreshhold) return; // Dont trigger if below threshhold or already triggered

        lastPressTime = 0;
        triggerEnterEvent.Invoke();
    }
}
