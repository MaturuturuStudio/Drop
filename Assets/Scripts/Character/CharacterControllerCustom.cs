﻿using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Custom controller that depends on Unity's CharacterController component.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(CharacterSize))]
public class CharacterControllerCustom : MonoBehaviour {

	#region Properties

	/// <summary>
	/// Trick used to allow a character to jump in any situation.
	/// After a GodJump, the character won't be able to GodJump again.
	/// </summary>
	public bool GodJump { get; set; }

	/// <summary>
	/// Contains information about the state of the character controller on the last frame.
	/// </summary>
	public CharacterControllerState State { get; private set; }

	/// <summary>
	/// The parameters of the character controller. They store how the character should behave.
	/// </summary>
	public CharacterControllerParameters Parameters {
		get {
			// If no override parameters have been specified, return the default parameters
			if (_overrideParameters.Count > 0)
				return _overrideParameters.Peek();
			else
				return defaultParameters;
		}
		set {
			if (value != null)
				_overrideParameters.Push(value);
			else if (_overrideParameters.Count > 0)
				_overrideParameters.Pop();
		}
	}

	/// <summary>
	/// The current velocity of the Character. The character will move according to this
	/// velocity each frame.
	/// </summary>
	public Vector3 Velocity { get { return _velocity; } }

	/// <summary>
	/// List of the colliders which this controller collided with on the last frame.
	/// </summary>
	public List<Collider> Collisions { get { return _collisions; } }

	/// <summary>
	/// The velocity the character had just before a collision.
	/// </summary>
	public Vector3 BeforeCollisionVelocity { get; private set; }

	#endregion

	#region Backing Fields

	/// <summary>
	/// Backing field for the Velocity property.
	/// </summary>
	private Vector3 _velocity;

	/// <summary>
	/// Stack of the parameters applied. Used by the Parameters property.
	/// </summary>
	private Stack<CharacterControllerParameters> _overrideParameters;

	/// <summary>
	/// Backing field for the Collisions property.
	/// </summary>
	private List<Collider> _collisions;

	#endregion

	#region Public Attributes

	/// <summary>
	/// Default parameters. If no parameters have been specified, the default ones will 
	/// be used.
	/// </summary>
	public CharacterControllerParameters defaultParameters;

	/// <summary>
	/// If enabled, the character will rotate to face it's velocity.
	/// </summary>
	[HideInInspector]
	public bool rotateWithVelocity = true;

	#endregion

	#region Private Attributes

	/// <summary>
	/// A reference to the CharacterController component of the entity.
	/// </summary>
	private CharacterController _controller;

	/// <summary>
	/// A reference to the entity's Transform component.
	/// </summary>
	private Transform _transform;

	/// <summary>
	/// A reference to the CharacterSize script of the entity.
	/// </summary>
	private CharacterSize _characterSize;

	/// <summary>
	/// List of observers subscribed to the character controller's
	/// events.
	/// </summary>
	private List<CharacterControllerListener> _listeners;

	#endregion

	#region Variables

	/// <summary>
	/// Time since the last time the character jumped.
	/// </summary>
	private float _jumpingTime;

	/// <summary>
	/// After been sent flying, specifies if the character will stop flying when
	/// it hits a collider.
	/// </summary>
	private bool _stopFlyingWhenHit;

	/// <summary>
	/// Time since the last time the character was sent flying.
	/// </summary>
	private float _flyingTime;

	/// <summary>
	/// Stores the sliding state from the previous frame.
	/// </summary>
	private bool _wasSliding;

	/// <summary>
	/// If the character is waiting for the anticipation to end before jumping.
	/// </summary>
	private bool _waitingForJump;

	/// <summary>
	/// Remaining time for the jump anticipation to end.
	/// </summary>
	private float _jumpDelayTime;

	/// <summary>
	/// Position on the global coordinates of the entity while standing on a platform.
	/// </summary>
	private Vector3 _activeGlobalPlatformPoint;

	/// <summary>
	/// Position of the entity relative to the platform it's standing on.
	/// </summary>
	private Vector3 _activeLocalPlatformPoint;

	/// <summary>
	/// Rotation on the global coordinates of the entity while standing on a platform.
	/// </summary>
	private Quaternion _activeGlobalPlatformRotation;

	/// <summary>
	/// Rotation of the entity relative to the platform it's standing on.
	/// </summary>
	private Quaternion _activeLocalPlatformRotation;

	#endregion

	#region Methods

	/// <summary>
	/// Unity's method called as soon as this entity is created.
	/// It will be called even if the entity is disabled.
	/// </summary>
	public void Awake() {
		// Creates the original state
		State = new CharacterControllerState();

		// Initializes the parameter's stack and lists
		_overrideParameters = new Stack<CharacterControllerParameters>();
		_collisions = new List<Collider>();
		_listeners = new List<CharacterControllerListener>();

		// Recovers the desired components
		_transform = transform;
		_controller = GetComponent<CharacterController>();
		_characterSize = GetComponent<CharacterSize>();

		// Ignores the collisions with the hat layer
		Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Character"), LayerMask.NameToLayer("Cloth"), true);
	}

	/// <summary>
	/// Unity's method called every time the entity becomes enabled.
	/// </summary>
	void OnEnable() {
		// Resets the state
		State.Reset();

		// Enables the rotation
		rotateWithVelocity = true;
	}

	/// <summary>
	/// Subscribes a listener to the controller's events.
	/// Returns false if the listener was already subscribed.
	/// </summary>
	/// <param name="listener">The listener to subscribe</param>
	/// <returns>If the listener was successfully subscribed</returns>
	public bool AddListener(CharacterControllerListener listener) {
		if (_listeners.Contains(listener))
			return false;
		_listeners.Add(listener);
		return true;
	}

	/// <summary>
	/// Unsubscribes a listener to the controller's events.
	/// Returns false if the listener wasn't subscribed yet.
	/// </summary>
	/// <param name="listener">The listener to unsubscribe</param>
	/// <returns>If the listener was successfully unsubscribed</returns>
	public bool RemoveListener(CharacterControllerListener listener) {
		if (!_listeners.Contains(listener))
			return false;
		_listeners.Remove(listener);
		return true;
	}

	#region Force Methods

	/// <summary>
	/// Adds a force to the velocity of the character.
	/// </summary>
	/// <param name="force">Amount of force to add</param>
	/// <param name="mode">The type of force it is applying</param>
	public void AddForce(Vector3 force, ForceMode mode = ForceMode.VelocityChange) {
		switch (mode) {
			case ForceMode.VelocityChange:
				// Adds the velocity ignoring it's mass
				_velocity += force;
				break;
			case ForceMode.Impulse:
				// Adds the velocity using it's mass
				_velocity += force / GetTotalMass();
				break;
			case ForceMode.Acceleration:
				// Adds the force over time ignoring it's mass
				_velocity += force * Time.deltaTime;
				break;
			case ForceMode.Force:
				// Adds the force over time using it's mass
				_velocity += (force / GetTotalMass()) * Time.deltaTime;
				break;
			default:
				Debug.LogError("Not a valid Force Mode: " + mode.ToString());
				return;
		}
	}

	/// <summary>
	/// Sets the velocity of the character.
	/// </summary>
	/// <param name="force">The new velocity of the character</param>
	public void SetForce(Vector3 force) {
		_velocity = force;
	}

	/// <summary>
	/// Sets the horizontal velocity of the character.
	/// The vertical component will not be modified.
	/// </summary>
	/// <param name="x">The new horizontal velocity of the character</param>
	public void SetHorizontalForce(float x) {
		_velocity.x = x;
	}

	/// <summary>
	/// Sets the vertical velocity of the character.
	/// The horizontal component will not be modified.
	/// </summary>
	/// <param name="y">The new vertical velocity of the character</param>
	public void SetVerticalForce(float y) {
		_velocity.y = y;
	}

	/// <summary>
	/// Adds a force to the velocity of the character.
	/// This force is applied relative to the gravity instead to the global axis.
	/// </summary>
	/// <param name="force">Amount of force to add</param>
	/// <param name="mode">The type of force it is applying</param>
	public void AddForceRelative(Vector3 force, ForceMode mode = ForceMode.VelocityChange) {
		// Rotates the force according to the gravity and adds it to the velocity
		float gravityAngle = Vector3.Angle(Parameters.Gravity, Vector3.down);
		if (Vector3.Cross(Parameters.Gravity, Vector3.down).z < 0)
			gravityAngle = -gravityAngle;
		AddForce(Quaternion.Euler(0, 0, -gravityAngle) * force, mode);
	}

	/// <summary>
	/// Sets the velocity of the character.
	/// This force is applied relative to the gravity instead to the global axis.
	/// </summary>
	/// <param name="force">The new velocity of the character</param>
	public void SetForceRelative(Vector3 force) {
		// Rotates the force according to the gravity and sets the velocity to it
		float gravityAngle = Vector3.Angle(Parameters.Gravity, Vector3.down);
		if (Vector3.Cross(Parameters.Gravity, Vector3.down).z < 0)
			gravityAngle = -gravityAngle;
		_velocity = Quaternion.Euler(0, 0, -gravityAngle) * force;
	}

	/// <summary>
	/// Sets the horizontal velocity of the character.
	/// The vertical component will not be modified.
	/// This force is applied relative to the gravity instead to the global axis.
	/// </summary>
	/// <param name="x">The new horizontal velocity of the character</param>
	public void SetHorizontalForceRelative(float x) {
		Vector3 verticalVelocity = GetVerticalVelocityRelative();
		Vector3 direction = Vector3.Cross(Vector3.forward, Parameters.Gravity).normalized;
		_velocity = verticalVelocity + direction * x;
	}

	/// <summary>
	/// Sets the vertical velocity of the character.
	/// The horizontal component will not be modified.
	/// This force is applied relative to the gravity instead to the global axis.
	/// </summary>
	/// <param name="y">The new vertical velocity of the character</param>
	public void SetVerticalForceRelative(float y) {
		Vector3 horizontalVelocity = GetHorizontalVelocityRelative();
		Vector3 direction = -Parameters.Gravity.normalized;
		_velocity = horizontalVelocity + direction * y;
	}

	/// <summary>
	/// Returns the velocity of the character on the desired direction.
	/// </summary>
	/// <param name="direction">The direction of the desired velocity</param>
	/// <returns>The velocity of the entity projected on the direction</returns>
	public Vector3 GetVelocityOnDirection(Vector3 direction) {
		return Vector3.Project(_velocity, direction);
	}

	/// <summary>
	/// Returns the velocity of the character on the horizontal axis, relative to the
	/// gravity.
	/// </summary>
	/// <returns>Horizontal component of the velocity</returns>
	public Vector3 GetHorizontalVelocityRelative() {
		Vector3 perpendicular = Vector3.Cross(Vector3.forward, Parameters.Gravity);
		return GetVelocityOnDirection(perpendicular);
	}
	
	/// <summary>
	/// Returns the velocity of the character on the vertical axis, relative to the
	/// gravity.
	/// </summary>
	/// <returns>Vertical component of the velocity</returns>
	public Vector3 GetVerticalVelocityRelative() {
		return GetVelocityOnDirection(-Parameters.Gravity);
	}

	/// <summary>
	/// Returns the current horizontal speed of the character as a value between -1
	/// and 1 (max speed to the left to max speed to the right, with 0 being still).
	/// </summary>
	/// <returns>Normalized speed of the character</returns>
	public float GetNormalizedSpeed() {
		return _velocity.x / (Parameters.maxSpeed * Mathf.Sqrt(GetSize()));
	}

	#endregion

	#region Input Methods

	/// <summary>
	/// Sets the input of the character on this frame. The velocity will be modified
	/// according to this input while using the acceleration defined on the parameters.
	/// </summary>
	/// <param name="horizontalInput">Signed-normalized value for the horizontal input</param>
	/// <param name="verticalInput">Signed-normalized value for the vertical input</param>
	public void SetInputForce(float horizontalInput, float verticalInput, float deltaTime) {
		// Checks if it can move. Nullifies the input otherwise
		if (!CanMove()) {
			horizontalInput = 0;
			verticalInput = 0;
		}

		// Checks the movement type
		bool horizontal = false;
		bool vertical = false;
		switch (Parameters.movementControl) {
			case CharacterControllerParameters.MovementControl.Horizontal:
				horizontal = true;
				break;
			case CharacterControllerParameters.MovementControl.Vertical:
				vertical = true;
				break;
			case CharacterControllerParameters.MovementControl.Both:
				horizontal = true;
				vertical = true;
				break;
			case CharacterControllerParameters.MovementControl.None:
				break;
			default:
				Debug.LogError("Not a valid Movement Control: " + Parameters.movementControl.ToString());
				return;
		}

		// If the input is relative to the gravity, rotates the velocity to match it
		float gravityAngle = 0;
		if (Parameters.relativeToGravity) {
			gravityAngle = Vector3.Angle(Parameters.Gravity, Vector3.down);
			if (Vector3.Cross(Parameters.Gravity, Vector3.down).z < 0)
				gravityAngle = -gravityAngle;
			_velocity = Quaternion.Euler(0, 0, gravityAngle) * _velocity;
		}

		// Gets the right acceleration
		float acceleration = State.IsGrounded ? Parameters.accelerationOnGround : Parameters.accelerationOnAir;

		// Multiplies the input by the character's size's square root
		float sqrtSize = Mathf.Sqrt(GetSize());
		horizontalInput *= sqrtSize;
		verticalInput *= sqrtSize;

		// Adds the right forces
		if (horizontal && (State.IsGrounded || horizontalInput != 0)) {
			_velocity.x = Mathf.Lerp(Velocity.x, horizontalInput * Parameters.maxSpeed, acceleration * deltaTime);
		}
		if (vertical && (State.IsGrounded || verticalInput != 0)) {
			_velocity.y = Mathf.Lerp(Velocity.y, verticalInput * Parameters.maxSpeed, acceleration * deltaTime);
		}

		// If it's grounded on a slope, subtracts the necessary vertical speed to stick to the ground
		if (State.IsGrounded && Mathf.Abs(State.SlopeAngle) > Parameters.angleThereshold) {
			float velocityStick = _velocity.x * Mathf.Sin(State.SlopeAngle * Mathf.Deg2Rad);
			if (velocityStick > 0)
				velocityStick *= Parameters.slopeStickiness;
			_velocity.y -= velocityStick;
		}

		// If the input was relative to the gravity, restores it's orientation
		if (Parameters.relativeToGravity) {
			_velocity = Quaternion.Euler(0, 0, -gravityAngle) * _velocity;
		}
	}

	/// <summary>
	/// Checks if the character can move on his current state. If not, the input
	/// will not be accepted and the character will keep it's velocity.
	/// </summary>
	/// <returns>If the character can move</returns>
	public bool CanMove() {
		// Checks if the controller accepts input
		bool slopeIsVertical = Mathf.Abs(Mathf.Abs(State.SlopeAngle) - Parameters.maxWallSlideAngle) < Parameters.angleThereshold;
		switch (Parameters.movementBehaviour) {
			case CharacterControllerParameters.MovementBehaviour.CanMoveAnywhere:
				return true;
			case CharacterControllerParameters.MovementBehaviour.CantMoveOnSlope:
				// Patch: if the slope is vertical, still allow movement
				return !State.IsOnSlope || slopeIsVertical;
			case CharacterControllerParameters.MovementBehaviour.CantMoveSliding:
				// Patch: if the slope is vertical, still allow movement
				return !State.IsSliding || slopeIsVertical;
			case CharacterControllerParameters.MovementBehaviour.CanMoveOnGround:
				return State.IsGrounded;
			case CharacterControllerParameters.MovementBehaviour.CantMove:
				return false;
			default:
				Debug.LogError("Not a valid Movement Behaviour: " + Parameters.movementBehaviour.ToString());
				return false;
		}
	}

	/// <summary>
	/// Makes the character jump, modifying the vertical force relatively to the gravity.
	/// The character vertical speed will be replaced.
	/// </summary>
	public void Jump() {
		// Checks if it can jump
		if (!CanJump())
			return;

		// Normal jump
		if (!State.IsOnSlope) {
			// Checks the character size
			int size = GetSize();
			if (size > Parameters.minSizeToApplyDelay) {
				// Starts a delay for the jump's anticipation
				_waitingForJump = true;
				_jumpDelayTime = Parameters.jumpDelayPerSize * (size - Parameters.minSizeToApplyDelay);
			}
			else {
				// No need for a delay, performs the jump inmediately
				PerformJump();
			}

			// Notifies the listeners
			_listeners.ForEach(e => e.OnBeginJump(this, _jumpDelayTime));
		}
		// Wall jump
		else if (State.IsSliding) {
			float gravity = Parameters.Gravity.magnitude;

			// Calculates the jump speed to reach the desired height
			float jumpHeight = GetSize() * Parameters.wallJumpHeight;
			float verticalSpeed = Mathf.Sqrt(2 * Mathf.Abs(gravity * jumpHeight));

			// Calculates the time to reach the peak of the wall jump
			float timeToPeak = verticalSpeed / gravity;

			// Calculates the horizontal speed
			float jumpDistance = GetSize() * Parameters.wallJumpDistance;
			float horizontalSpeed = Mathf.Sign(State.SlopeAngle) * jumpDistance / timeToPeak;

			// Rotates the force according to the gravity
			float gravityAngle = Vector3.Angle(Parameters.Gravity, Vector3.down);
			if (Vector3.Cross(Parameters.Gravity, Vector3.down).z < 0)
				gravityAngle = -gravityAngle;
			
			// Sends the player flying using the wall jump speed
			Vector3 finalVelocity = Quaternion.Euler(0, 0, -gravityAngle) * new Vector3(horizontalSpeed, verticalSpeed, 0);
			SendFlying(finalVelocity, false, true, timeToPeak);

			_jumpingTime = Parameters.jumpFrequency;

			// Notifies the listeners
			_listeners.ForEach(e => e.OnWallJump(this));
		}
	}

	private void PerformJump() {
		// Calculates the jump speed to reach the desired height
		float jumpHeight = GetSize() * Parameters.jumpMagnitude;
		float gravity = Parameters.Gravity.magnitude;
		float jumpSpeed = Mathf.Sqrt(2 * gravity * jumpHeight);
		SetVerticalForceRelative(jumpSpeed);

		_jumpingTime = Parameters.jumpFrequency;

		// Resets the flags
		_waitingForJump = false;
		GodJump = false;

		// Notifies the listeners
		_listeners.ForEach(e => e.OnPerformJump(this));
	}

	/// <summary>
	/// Checks if the character can jump on his current state.
	/// </summary>
	/// <returns>If the character can jump</returns>
	public bool CanJump() {
		// If it has recently jumped, it cannot jump again
		if (_jumpingTime > 0)
			return false;

		if (GodJump)
			return true;

		// If it's waiting to jump, it cannot jump again
		if (_waitingForJump)
			return false;

		// Checks the jumping behaviour
		switch (Parameters.jumpBehaviour) {
			case CharacterControllerParameters.JumpBehaviour.CanJumpAnywhere:
				return true;
			case CharacterControllerParameters.JumpBehaviour.CanJumpOnSlope:
				return State.IsGrounded || State.IsOnSlope;
			case CharacterControllerParameters.JumpBehaviour.CanJumpSliding:
				return State.IsGrounded || State.IsSliding;
			case CharacterControllerParameters.JumpBehaviour.CanJumpOnGround:
				return State.IsGrounded;
			case CharacterControllerParameters.JumpBehaviour.CantJump:
				return false;
			default:
				Debug.LogError("Not a valid Jump Behaviour: " + Parameters.jumpBehaviour.ToString());
				return false;
		}
	}

	/// <summary>
	/// Sends the character flying with a velocity. The character's current
	/// velocity will be replaced by the new one. The character won't stop
	/// and it will ignore the input.
	/// </summary>
	/// <param name="velocity">The new character's velocity</param>
	/// <param name="useMass">If the velocity change should consider the character's mass</param>
	/// <param name="restoreWhenGrounded">If the character should return to normal when hitting a collider</param>
	public void SendFlying(Vector3 velocity, bool useMass = false, bool restoreWhenHit = true, float flyTime = float.MaxValue) {
		// Stops the character
		Stop();

		// Adds the velocity to the character
		ForceMode mode = useMass ? ForceMode.Impulse : ForceMode.VelocityChange;
		AddForce(velocity, mode);

		// Changes the character's parameters
		_overrideParameters.Clear();
		Parameters = CharacterControllerParameters.FlyingParameters;

		// Sets the flags
		State.IsFlying = true;
		_stopFlyingWhenHit = restoreWhenHit;
		_flyingTime = flyTime;
	}

	/// <summary>
	/// Sends the character flying with a velocity. It will not rotate to
	/// face the direction.
	/// </summary>
	/// <param name="velocity">The new character's velocity</param>
	/// <param name="useMass">If the velocity change should consider the character's mass</param>
	/// <param name="restoreWhenGrounded">If the character should return to normal when hitting a collider</param>
	public void SendFlyingNoRotation(Vector3 velocity, bool useMass = false, bool restoreWhenHit = true, float flyTime = float.MaxValue) {
		// Sets the no rotation flag
		rotateWithVelocity = false;

		// Sends the character flying
		SendFlying(velocity, useMass, restoreWhenHit, flyTime);
	}

	/// <summary>
	/// Stops the character from being flying. The character's parameters
	/// will be restored to their default values.
	/// </summary>
	public void StopFlying() {
		// Restores the character's parameters and sets down the flying flag
		if (State.IsFlying) {
			Parameters = null;
			State.IsFlying = false;
			rotateWithVelocity = true;
		}
	}

	#endregion

	/// <summary>
	/// Moves the character according to it's velocity.
	/// </summary>
	public void PerformMovement() {
		// Decreases the timers
		_jumpingTime -= Time.deltaTime;
		_flyingTime -= Time.deltaTime;
		_jumpDelayTime -= Time.deltaTime;

		// If the jump anticipation has ended, performs the jump
		if (_jumpDelayTime < 0 && _waitingForJump)
			PerformJump();

		// If the flying timer has expired, stops the flight
		if (_flyingTime < 0)
			StopFlying();

		// Adds the gravity to the velocity. If sliding, multiply it by a drag factor.
		float dragFactor = 1;
		if (State.IsSliding)
			dragFactor -= Parameters.slidingDragFactor / Mathf.Sqrt(GetSize());
		_velocity += Parameters.Gravity * Time.deltaTime * dragFactor;

		// Checks if the entity is grounded on a moving platform
		HandleMovingPlatforms();

		// Tries the movement of the entity according to it's velocity
		Move(Velocity * Time.deltaTime);

		// Updates the flying time
		if (!State.IsGrounded)
			State.TimeFloating += Time.deltaTime;
		else
			State.TimeFloating = 0;
	}

	#region Movement Methods

	/// <summary>
	/// Handles the movement of the character while it's standing on a moving platform.
	/// The moving platform can be moved in any way, and the character will follow it
	/// even if it teleports.
	/// </summary>
	private void HandleMovingPlatforms() {
		if (State.GroundedObject != null) {
			// Gets the new global position of the entity relatively to the platform
			Vector3 newGlobalPlatformPoint = State.GroundedObject.transform.TransformPoint(_activeLocalPlatformPoint);
			Vector3 moveDistance = newGlobalPlatformPoint - _activeGlobalPlatformPoint;

			// Moves the entity to match the platform translation
			if (moveDistance != Vector3.zero)
				_transform.Translate(moveDistance, Space.World);

			// Saves the velocity of the platform
			State.PlatformVelocity = moveDistance / Time.deltaTime;
			
			// Support for rotating platforms
			Quaternion newGlobalPlatformRotation = State.GroundedObject.transform.rotation * _activeLocalPlatformRotation;
			Quaternion rotationDiff = newGlobalPlatformRotation * Quaternion.Inverse(_activeGlobalPlatformRotation);

			// Prevents rotation of the local up vector
			rotationDiff = Quaternion.FromToRotation(rotationDiff * transform.up, transform.up) * rotationDiff;
			transform.rotation = rotationDiff * transform.rotation;
		}
		else {
			// Resets the velocity of the platform
			State.PlatformVelocity = Vector3.zero;
		}
	}

	/// <summary>
	/// Moves the character the desired distance.
	/// </summary>
	/// <param name="movement">Movement distance</param>
	private void Move(Vector3 movement) {
		// Stores if the character was sliding on the previous frame
		_wasSliding = State.IsSliding;

		// Resets the state, but keeps the platform's velocity and flying state
		Vector3 platformVelocity = State.PlatformVelocity;
		bool isFlying = State.IsFlying;
		State.Reset();
		State.PlatformVelocity = platformVelocity;
		State.IsFlying = isFlying;

		// Resets the collisions list
		_collisions.Clear();

		// Clamps the movement
		movement.x = Mathf.Clamp(movement.x, -Parameters.maxVelocity.x, Parameters.maxVelocity.x);
		movement.y = Mathf.Clamp(movement.y, -Parameters.maxVelocity.y, Parameters.maxVelocity.y);
		movement.z = Mathf.Clamp(movement.z, -Parameters.maxVelocity.z, Parameters.maxVelocity.z);

		// Stores the falling state
		float velocityAngle = Vector3.Angle(movement, Parameters.Gravity);
		State.IsFalling = Mathf.Abs(velocityAngle) < 90 + Parameters.angleThereshold;

		// Do the actual movement
		_controller.Move(movement);

		// If the Z coordinate is clamped, resets it to zero
		if (Parameters.zClamp) {
			Vector3 clamped = _transform.position;
			clamped.z = 0;
			_transform.position = clamped;
		}

		// Stores the global and local position relative to the ground
		if (State.GroundedObject != null) {
			_activeGlobalPlatformPoint = _transform.position;
			_activeLocalPlatformPoint = State.GroundedObject.transform.InverseTransformPoint(_transform.position);
			
			// Support for moving platform rotation
			_activeGlobalPlatformRotation = transform.rotation;
			_activeLocalPlatformRotation = Quaternion.Inverse(State.GroundedObject.transform.rotation) * transform.rotation;
		}
	}

	/// <summary>
	/// Zeroes the velocity of the controller.
	/// </summary>
	public void Stop() {
		_velocity = Vector3.zero;
	}

	/// <summary>
	/// Unity's method for handling the collisions derived from the CharacterController
	/// component.
	/// Creates the right state of the character and modifies it's velocity based on
	/// the collision information.
	/// </summary>
	/// <param name="hit">Information of the collision</param>
	public void OnControllerColliderHit(ControllerColliderHit hit) {
		// Notifies the listeners
		_listeners.ForEach(e => e.OnPreCollision(this, hit));

		// There has been collisions this frame. Stops the wall jumping
		State.HasCollisions = true;
		_collisions.Add(hit.collider);
		if (_stopFlyingWhenHit)
			StopFlying();

		// Spheres have their normal inverted for whatever reason
		Vector3 normal = hit.normal;
		if (hit.collider is SphereCollider)
			normal = -hit.normal;

		// Gets the vector's projection on the XY plane
		normal = Vector3.ProjectOnPlane(normal, Vector3.forward);

		// Saves the velocity of the character
		BeforeCollisionVelocity = Velocity;

		// Before modifying the velocity, applies force to the other object if it allows it
		Rigidbody otherRigidbody = hit.collider.attachedRigidbody;
		if (otherRigidbody != null && !otherRigidbody.isKinematic) {
			Vector3 force = Vector3.Project(_velocity, -normal) * GetTotalMass();
			otherRigidbody.AddForceAtPosition(force, hit.point, ForceMode.Impulse);
		}

		// Looks for the angle between the collision normal an the gravity
		State.SlopeAngle = Vector3.Angle(normal, -Parameters.Gravity);
		if (Vector3.Cross(normal, -Parameters.Gravity).z < 0)
			State.SlopeAngle = -State.SlopeAngle;

		if (!hit.collider.CompareTag(Tags.SlideAlways) && (hit.collider.CompareTag(Tags.SlideNever) || Mathf.Abs(State.SlopeAngle) < _controller.slopeLimit + Parameters.angleThereshold)) {
			// The collider is considered ground
			State.IsGrounded = true;
			State.IsFalling = false;
			State.IsOnSlope = false;
			State.IsSliding = false;
			State.GroundedObject = hit.collider.gameObject;

			// Removes the velocity's vertical component
			SetVerticalForceRelative(0);
		}
		else {
			// The character is not grounded
			State.IsGrounded = false;
			State.GroundedObject = null;

			// Projects the speed to the normal's perpendicular
			Vector3 normalPerpendicular = Vector3.Cross(normal, Vector3.forward);
			_velocity = Vector3.Project(_velocity, normalPerpendicular);
			
			// Checks if the character is on a slope
			if (hit.collider.CompareTag(Tags.SlideAlways) || Mathf.Abs(State.SlopeAngle) < Parameters.maxWallSlideAngle + Parameters.angleThereshold) {
				// The collider is considered a slope
				State.IsOnSlope = true;

				if (State.IsFalling && hit.collider.CompareTag(Tags.WallJump)) {
					// The character is now sliding
					State.IsSliding = true;

					// If the character wasn't sliding, stops it
					if (!_wasSliding)
						Stop();
				}
				else {
					State.IsSliding = false;
				}
			}
			else {
				State.IsOnSlope = false;
			}
		}

		// Calls the special method on the collided object
		hit.gameObject.SendMessage("OnCustomControllerCollision", hit, SendMessageOptions.DontRequireReceiver);

		// Notifies the listeners
		_listeners.ForEach(e => e.OnPostCollision(this, hit));
	}

	#endregion

	#region Other Methods
	
	/// <summary>
	/// Returns the size of the character. If no size has been defined, returns a default
	/// value of 1.
	/// </summary>
	/// <returns>The size of the character</returns>
	private int GetSize() {
		if (_characterSize != null && _characterSize.isActiveAndEnabled)
			return _characterSize.GetSize();
		else
			return 1;
	}

	/// <summary>
	/// Returns the total mass of the character, scaled by it's size.
	/// </summary>
	/// <returns>The character's mass</returns>
	public float GetTotalMass() {
		return Parameters.baseMass * GetSize();
	}

	#endregion

	#endregion
}
