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
		public bool pause;
		public bool selectBlock;
		public int selectedBlock;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;

#if ENABLE_INPUT_SYSTEM && STARTER_ASSETS_PACKAGES_CHECKED
		

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
		
		public void OnPause(InputAction.CallbackContext context)
		{
			PauseInput(context.ReadValueAsButton());
		}

		public void OnSelectBlock(InputAction.CallbackContext context)
		{
			SelectBlockInput(context.ReadValueAsButton(), int.Parse(context.control.name));
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
		
		private void PauseInput(bool newPauseState)
		{
			pause = newPauseState;
		}

		private void SelectBlockInput(bool newSelectState, int selectedBlock)
		{
			selectBlock = newSelectState;
			this.selectedBlock = selectedBlock;
		}
		
		private void OnApplicationFocus(bool hasFocus)
		{
			SetCursorState(cursorLocked && FindObjectOfType<UIController>().menu.activeSelf);
		}

		private void SetCursorState(bool newState)
		{
			Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
		}
	}
	
}