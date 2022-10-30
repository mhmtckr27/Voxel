using System;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
	public class StarterAssetsInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector2 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool dig;
		public bool build;
		public bool save;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED


		private void Awake()
		{
			// Let's create a button action bound to the A button
			// on the gamepad.
			var action = new InputAction(
				type: InputActionType.Button,
				binding: "<Mouse>/leftButton");

// When the action is performed (which will happen when the
// button is pressed and then released) we take the duration
// of the press to determine how many projectiles to spawn.
			action.performed +=
				context =>
				{
					Debug.LogError("HAYRI "  + context.duration);
					DigInput(context.duration > 3f);
				};

		}

		public void OnMove(InputAction.CallbackContext context)
		{
			MoveInput(context.ReadValue<Vector2>());
		}

		public void OnLook(InputAction.CallbackContext context)
		{
			if(cursorInputForLook)
			{
				LookInput(context.ReadValue<Vector2>());
			}
		}

		public void OnJump(InputAction.CallbackContext context)
		{
			JumpInput(context.performed);
		}

		public void OnSprint(InputAction.CallbackContext context)
		{
			SprintInput(context.performed);
		}

		public void OnDig(InputAction.CallbackContext context)
		{
			DigInput(context.ReadValueAsButton());
		}

		public void OnBuild(InputAction.CallbackContext context)
		{
			BuildInput(context.ReadValueAsButton());
		}
		
		public void OnSave(InputAction.CallbackContext context)
		{
			SaveInput(context.ReadValueAsButton());
		}
#endif


		public void MoveInput(Vector2 newMoveDirection)
		{
			move = newMoveDirection;
		} 

		public void LookInput(Vector2 newLookDirection)
		{
			look = newLookDirection;
		}

		public void JumpInput(bool newJumpState)
		{
			jump = newJumpState;
		}

		public void SprintInput(bool newSprintState)
		{
			sprint = newSprintState;
		}
		
		private void DigInput(bool newDigState)
		{
			dig = newDigState;
		}

		private void BuildInput(bool newBuildState)
		{
			build = newBuildState;
		}

		private void SaveInput(bool newSaveState)
		{
			save = newSaveState;
		}
		
		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}