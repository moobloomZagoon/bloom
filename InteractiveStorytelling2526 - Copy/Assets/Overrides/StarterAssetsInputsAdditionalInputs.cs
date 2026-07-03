using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    public class StarterAssetsInputsAdditionalInputs : StarterAssetsInputs
    {
        public bool interact;
        public bool inventory;

        public UnityEvent OnInventoryButton;

        // Sensitivity multiplier (adjust in Inspector if needed)
        public float mouseSensitivity = 0.2f;

        // Deadzone to prevent mouse drift
        public float lookDeadzone = 0.0001f;

        public void OnInteract(InputValue value)
        {
            InteractInput(value.isPressed);
        }

        public void InteractInput(bool newInteractState)
        {
            interact = newInteractState;
        }

        public void OnInventory(InputValue value)
        {
            InventoryInput(value.isPressed);
        }

        public void InventoryInput(bool newInventoryState)
        {
            inventory = newInventoryState;

            if (newInventoryState && OnInventoryButton != null)
            {
                OnInventoryButton.Invoke();
            }
        }

        public void OnLookAround(InputValue value)
        {
            if (!cursorInputForLook) return;

            Vector2 look = value.Get<Vector2>();

            // Apply deadzone properly (DON'T return early)
            if (look.sqrMagnitude < lookDeadzone)
            {
                look = Vector2.zero;
            }

            // Apply sensitivity
            look *= mouseSensitivity;

            // Always send input (even zero) to stop drifting
            LookInput(look);
        }

        void Start()
        {
            // Lock and hide cursor for FPS look
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}