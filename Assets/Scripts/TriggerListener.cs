using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
        button.Select();
        button.OnSubmit(null);
        triggerEnterEvent.Invoke();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.name.Equals("SelectPoint")) return;
        pressed = false;
        button.OnDeselect(null);
    }
}
