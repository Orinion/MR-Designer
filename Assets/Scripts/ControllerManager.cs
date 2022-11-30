using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

/// <summary>
/// Class to manage ray and direct interactors
/// </summary>
public class ControllerManager : MonoBehaviour
{
    /// <summary>
    /// Order when instances of type <see cref="ActionBasedControllerManager"/> are updated.
    /// </summary>
    /// <remarks>
    /// Executes before controller components to ensure input processors can be attached
    /// to input actions and/or bindings before the controller component reads the current
    /// values of the input actions.
    /// </remarks>
    public const int k_UpdateOrder = XRInteractionUpdateOrder.k_Controllers - 1;

    [Space]
    [Header("Interactors")]

    [SerializeField]
    [Tooltip("The GameObject containing the interactor used for direct manipulation.")]
    XRDirectInteractor m_DirectInteractor;

    [Space]
    [Header("Controller Actions")]

    [SerializeField]
    [Tooltip("The reference to the action of selecting with this controller.")]
    InputActionReference m_Select;
     
}

