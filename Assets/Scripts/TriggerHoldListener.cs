using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

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
