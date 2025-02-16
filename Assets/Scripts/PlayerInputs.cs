using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputs : MonoBehaviour
	{
		[Header("Character Input Values")]
		public Vector3 move;
		public Vector2 look;
		public bool jump;
		public bool sprint;
		public bool toggleCameraRotation;

		[Header("Movement Settings")]
		public bool analogMovement;

		[Header("Mouse Cursor Settings")]
		public bool cursorLocked = true;
		public bool cursorInputForLook = true;


		//움직임 입력
		public void OnMove(InputValue value)
		{
			MoveInput(value.Get<Vector3>());
		Debug.Log(value.Get<Vector3>());
		}

		public void OnLook(InputValue value)
		{
			if(cursorInputForLook)
			{
				LookInput(value.Get<Vector2>());
			}
		}

	public void OnJump(InputValue value)
	{
		JumpInput(value.isPressed);
	}

	public void OnSprint(InputValue value)
	{
		SprintInput(value.isPressed);
	}

		public void OnFreeCam(InputValue value)
	{
		FreeCamInput(value.isPressed);
	}



    //움직임값 저장
    public void MoveInput(Vector3 newMoveDirection)
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

	public void FreeCamInput(bool newFreeCamState)
	{
        toggleCameraRotation = newFreeCamState;
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
