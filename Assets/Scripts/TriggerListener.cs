using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Triggers UnityEvent when a controller enters the trigger.
/// Attach this component to a GameObject with a collider configured to be a trigger.
/// There is also visual feedback of pushing the attached button.
/// Listeners for the event can be added in the inspector. 
/// </summary>
public class TriggerListener : MonoBehaviour
{
    private bool pressed = false;
    private Button button;

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
        triggerEnterEvent.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.name.Equals("SelectPoint") || !pressed) return;
        pressed = false;
        button.interactable = true;
    }
}
