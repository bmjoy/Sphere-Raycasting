﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Script needed to detect and interact with the object selected by raycast.
/// Attached to the MainCamera, child of the RigidbodyFPS.
/// </summary>
public class InteractWithSelectedObject : MonoBehaviour
{
    private IInteractable objectToInteractWith; // reference pointing to the object best suitable for interaction

    private void OnEnable()
    {
        DetectInteractableObject.ObjectToInteractWithChanged += OnObjectToInteractWithChanged;
        DetectInteractableObjectComparative.ObjectToInteractWithChanged += OnObjectToInteractWithChanged;
    }

    private void OnDisable()
    {
        DetectInteractableObject.ObjectToInteractWithChanged -= OnObjectToInteractWithChanged;
        DetectInteractableObjectComparative.ObjectToInteractWithChanged -= OnObjectToInteractWithChanged;
    }

    private void Update()
    {
        if (Input.GetButtonDown("Interact"))
            CheckForInteractionInput();
#if UNITY_EDITOR
        if (objectToInteractWith != null)
        Debug.DrawRay(this.transform.position, ((MonoBehaviour)objectToInteractWith).transform.position - this.transform.position, Color.green);
#endif

    }

    /// <summary>
    /// Called when Object To Interact With changes in the DetectInteractableObject
    /// script through an event.
    /// </summary>  
    public void OnObjectToInteractWithChanged(IInteractable returnedInteractable)
    {
        objectToInteractWith = returnedInteractable;
    }

    /// <summary>
    /// checks if objectToInteractWith is not null then if interact input is pressed.
    /// activate interact of objectToInteractWith if both return true.
    /// </summary>
    private void CheckForInteractionInput()
    {
        if (objectToInteractWith != null)
        {
            objectToInteractWith.Interact();
        }
        else
        {
            Debug.Log("No Interactable Detected");
        }
    }
}
