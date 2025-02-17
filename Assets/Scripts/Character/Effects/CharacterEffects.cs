﻿using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Listener class which plays the multiple effects produced
/// by the character.
/// </summary>
[RequireComponent(typeof(CharacterControllerCustom))]
[RequireComponent(typeof(CharacterSize))]
[RequireComponent(typeof(CharacterShoot))]
[RequireComponent(typeof(CharacterFusion))]
public class CharacterEffects : MonoBehaviour, CharacterShootListener, CharacterFusionListener, CharacterControllerListener, IrrigateListener, EnemyBehaviourListener {

	/// <summary>
	/// The collision effects will only be played when the
	/// collision happens with these layers.
	/// </summary>
	public LayerMask colliderLayerToPlayEffects;

	/// <summary>
	/// The layer of the last collider hit by the character.
	/// </summary>
	private int _colliderLayer;

	/// <summary>
	/// Effect played while walking.
	/// </summary>
	public MinSpeedEffectInformation walkTrail;

	/// <summary>
	/// Effect played each step of the walking animation.
	/// </summary>
	public MinSpeedEffectInformation walkStep;

	/// <summary>
	/// Effect played when landing.
	/// </summary>
	public MinTimeEffectInformation land;

	/// <summary>
	/// Effect played when jumping.
	/// </summary>
	public EffectInformation jump;

	/// <summary>
	/// Effect played when shooting.
	/// </summary>
	public EffectInformation shoot;

	/// <summary>
	/// Effect played when fusing.
	/// </summary>
	public EffectInformation fuse;

	/// <summary>
	/// Effect played while sliding along a slope.
	/// </summary>
	public EffectInformation slope;

	/// <summary>
	/// Effect played while sliding.
	/// </summary>
	public EffectInformation slide;

	/// <summary>
	/// Effect played when wall jumping.
	/// </summary>
	public EffectInformation wallJump;

	/// <summary>
	/// Effect played when irrigating.
	/// </summary>
	public EffectInformation irrigate;

	/// <summary>
	/// Effect played when being hit.
	/// </summary>
	public EffectInformation hit;

	/// <summary>
	/// Effect played while the player doesn't control the character.
	/// </summary>
	public EffectInformation sleep;

	/// <summary>
	/// Mask for any collision point check.
	/// </summary>
	public LayerMask sceneMask;

	/// <summary>
	/// Reference to the model's transform.
	/// </summary>
	public Transform modelTransfrom;

	/// <summary>
	/// Reference to this entity's Transform component.
	/// </summary>
	private Transform _transform;

	/// <summary>
	/// Reference to this entity's CharacterControllerCustom component.
	/// </summary>
	private CharacterControllerCustom _ccc;

	/// <summary>
	/// Reference to this entity's CharacterSize component.
	/// </summary>
	private CharacterSize _characterSize;

	/// <summary>
	/// Reference to this entity's CharacterFusion component.
	/// </summary>
	private CharacterFusion _characterFusion;

	/// <summary>
	/// Reference to this entity's CharacterShoot component.
	/// </summary>
	private CharacterShoot _characterShoot;

	/// <summary>
	/// Reference to this entity's CharacterController component.
	/// </summary>
	private CharacterController _controller;

	/// <summary>
	/// Reference to this game controller's GameControllerIndependentControl component.
	/// </summary>
	private GameControllerIndependentControl _gcic;

	/// <summary>
	/// The walking effect used by this script.
	/// </summary>
	private Transform _walkTrailEffect;

	/// <summary>
	/// Reference to the walking effect's particle systems.
	/// </summary>
	private Dictionary<ParticleSystem, ParticleSystemState> _walkTrailParticleEffects;

	/// <summary>
	/// The sliding effect used by this script.
	/// </summary>
	private Transform _slideEffect;

	/// <summary>
	/// Reference to the sliding effect's particle systems.
	/// </summary>
	private Dictionary<ParticleSystem, ParticleSystemState> _slideParticleEffects;

	/// <summary>
	/// The slope effect used by this script.
	/// </summary>
	private Transform _slopeEffect;

	/// <summary>
	/// Reference to the slope effect's particle systems.
	/// </summary>
	private Dictionary<ParticleSystem, ParticleSystemState> _slopeParticleEffects;

	/// <summary>
	/// The sleep effect created by this script.
	/// </summary>
	private Transform _sleepEffect;

	void Awake() {
		// Retrieves the desired components
		_transform = transform;
		_ccc = GetComponent<CharacterControllerCustom>();
		_characterSize = GetComponent<CharacterSize>();
		_characterFusion = GetComponent<CharacterFusion>();
		_characterShoot = GetComponent<CharacterShoot>();
		_controller = GetComponent<CharacterController>();
		_gcic = GameObject.FindGameObjectWithTag(Tags.GameController).GetComponent<GameControllerIndependentControl>();
	}

	void Start() {
		// Subscribes itself to the publishers
		_ccc.AddListener(this);
		_characterFusion.AddListener(this);
		_characterShoot.AddListener(this);

		// Checks if it is asleep and creates the sleeping effect
		if (!_gcic.IsUnderControl(gameObject)) {
			if (_sleepEffect == null)
				_sleepEffect = sleep.PlayEffect(_transform.position, _transform.rotation).transform;
			_sleepEffect.transform.parent = _transform;
			_sleepEffect.transform.localScale = Vector3.one;
		}
	}

	void OnEnable() {
		// Creates and stops the walking effect
		_walkTrailEffect = walkTrail.PlayEffect(_transform.position, Quaternion.identity).transform;
		_walkTrailParticleEffects = new Dictionary<ParticleSystem, ParticleSystemState>();
		foreach (ParticleSystem system in _walkTrailEffect.GetComponentsInChildren<ParticleSystem>()) {
			_walkTrailParticleEffects.Add(system, new ParticleSystemState(system));
			ParticleSystem.EmissionModule emission = system.emission;
			emission.enabled = false;
		}

		// Creates and stops the sliding effect
		_slideEffect = slide.PlayEffect(_transform.position, Quaternion.identity).transform;
		_slideParticleEffects = new Dictionary<ParticleSystem, ParticleSystemState>();
		foreach (ParticleSystem system in _slideEffect.GetComponentsInChildren<ParticleSystem>()) {
			_slideParticleEffects.Add(system, new ParticleSystemState(system));
			ParticleSystem.EmissionModule emission = system.emission;
			emission.enabled = false;
		}

		// Creates and stops the slope effect
		_slopeEffect = slope.PlayEffect(_transform.position, Quaternion.identity).transform;
		_slopeParticleEffects = new Dictionary<ParticleSystem, ParticleSystemState>();
		foreach (ParticleSystem system in _slopeEffect.GetComponentsInChildren<ParticleSystem>()) {
			_slopeParticleEffects.Add(system, new ParticleSystemState(system));
			ParticleSystem.EmissionModule emission = system.emission;
			emission.enabled = false;
		}
	}

	void OnDisable() {
		// Stops and destroys the walking effect
		if (_walkTrailEffect != null) {
			float maxLifetime = 0;
			foreach (ParticleSystem system in _walkTrailParticleEffects.Keys) {
				ParticleSystem.EmissionModule emission = system.emission;
				emission.enabled = false;
				maxLifetime = Mathf.Max(maxLifetime, system.startLifetime);
			}
			_walkTrailParticleEffects.Clear();
			Destroy(_walkTrailEffect.gameObject, maxLifetime);
			_walkTrailEffect = null;
		}

		// Stops and destroys the sliding effect
		if (_slideEffect != null) {
			float maxLifetime = 0;
			foreach (ParticleSystem system in _slideParticleEffects.Keys) {
				ParticleSystem.EmissionModule emission = system.emission;
				emission.enabled = false;
				maxLifetime = Mathf.Max(maxLifetime, system.startLifetime);
			}
			_slideParticleEffects.Clear();
			Destroy(_slideEffect.gameObject, maxLifetime);
			_slideEffect = null;
		}

		// Stops and destroys the slope effect
		if (_slopeEffect != null) {
			float maxLifetime = 0;
			foreach (ParticleSystem system in _slopeParticleEffects.Keys) {
				ParticleSystem.EmissionModule emission = system.emission;
				emission.enabled = false;
				maxLifetime = Mathf.Max(maxLifetime, system.startLifetime);
			}
			_slopeParticleEffects.Clear();
			Destroy(_slopeEffect.gameObject, maxLifetime);
			_slopeEffect = null;
		}
	}

	void FixedUpdate() {
		// Checks if the hit object has a valid layer
		bool validLayer = colliderLayerToPlayEffects.ContainsLayer(_colliderLayer);

		// Plays or stops the walking effect
		if (validLayer)
			foreach (ParticleSystem system in _walkTrailParticleEffects.Keys) {
				ParticleSystem.EmissionModule emission = system.emission;
				emission.enabled = _ccc.State.IsGrounded && Mathf.Abs(_ccc.GetNormalizedSpeed()) >= walkTrail.GetMinSpeed(_characterSize.GetSize());
			}

		// Plays or stops the sliding effect
		if (validLayer)
			foreach (ParticleSystem system in _slideParticleEffects.Keys) {
				ParticleSystem.EmissionModule emission = system.emission;
				emission.enabled = _ccc.State.IsSliding;
			}

		// Plays or stops the slope effect
		if (validLayer)
			foreach (ParticleSystem system in _slopeParticleEffects.Keys) {
				ParticleSystem.EmissionModule emission = system.emission;
				emission.enabled = _ccc.State.IsFalling && _ccc.State.IsOnSlope && !_ccc.State.IsSliding;
			}

		// If the character is controlled, stops the sleep effect
		if (_sleepEffect != null && _gcic.IsUnderControl(gameObject)) {
			float maxLifetime = 0;
			foreach (ParticleSystem system in _sleepEffect.GetComponentsInChildren<ParticleSystem>()) {
				ParticleSystem.EmissionModule emission = system.emission;
				emission.enabled = false;
				maxLifetime = Mathf.Max(maxLifetime, system.startLifetime);
			}
			_sleepEffect.transform.parent = null;
			Destroy(_sleepEffect.gameObject, maxLifetime);
			_sleepEffect = null;
		}
	}

	/// <summary>
	/// This method will be called by the animation events.
	/// </summary>
	public void OnWalkStep() {
		// Plays the walk step effect
		if (_ccc.State.IsGrounded && Mathf.Abs(_ccc.GetNormalizedSpeed()) >= walkStep.GetMinSpeed(1)) {
			GameObject effect = walkStep.PlayEffect(_walkTrailEffect.position, modelTransfrom.rotation);
			effect.transform.parent = _transform;
			effect.transform.localScale = Vector3.one;
		}
	}

	public void OnBeginJump(CharacterControllerCustom ccc, float delay) {
		// Do nothing
	}

	public void OnPerformJump(CharacterControllerCustom ccc) {
		// Looks for the ground and plays the jump effect
		RaycastHit hit;
		if (Physics.SphereCast(ccc.transform.position, _controller.radius * _characterSize.GetSize(), ccc.Parameters.Gravity, out hit, 10, sceneMask))
			jump.PlayEffect(hit.point, Quaternion.LookRotation(Vector3.forward, hit.normal), _characterSize.GetSize());
	}

	public void OnPostCollision(CharacterControllerCustom ccc, ControllerColliderHit hit) {
		if (hit.collider.CompareTag(Tags.Player))
			return;

		_colliderLayer = hit.gameObject.layer;

		// Plays the landing effect
		Quaternion normalRotation = Quaternion.LookRotation(Vector3.forward, hit.normal);
		if (ccc.State.IsGrounded && ccc.State.TimeFloating > land.minTime) {
			land.PlayEffect(hit.point, normalRotation, _characterSize.GetSize());
		}

		// Calculates some values for the particles
		Vector3 eulerRotation = new Vector3(Mathf.PI + Vector3.Angle(Vector3.left, hit.normal) * Mathf.Deg2Rad, Mathf.PI / 2, 0);
		float sizeFactor = _characterSize.GetSize();

		// Positions the walking effect
		if (ccc.State.IsGrounded) {
			_walkTrailEffect.position = hit.point;
			_walkTrailEffect.rotation = normalRotation;
			foreach (KeyValuePair<ParticleSystem, ParticleSystemState> system in _walkTrailParticleEffects) {
				system.Key.startRotation3D = eulerRotation;
				system.Value.UpdateWithSize(sizeFactor);
			}
		}

		// Positions the slope effect
		if (ccc.State.IsOnSlope && !ccc.State.IsSliding) {
			_slopeEffect.position = hit.point;
			_slopeEffect.rotation = normalRotation;
			foreach (KeyValuePair<ParticleSystem, ParticleSystemState> system in _slopeParticleEffects) {
				system.Key.startRotation3D = eulerRotation;
				system.Value.UpdateWithSize(Mathf.Sqrt(sizeFactor));
			}
		}
		// Positions the sliding effect
		else if (ccc.State.IsSliding) {
			_slideEffect.position = hit.point;
			_slideEffect.rotation = normalRotation;
			foreach (KeyValuePair<ParticleSystem, ParticleSystemState> system in _slideParticleEffects) {
				system.Key.startRotation3D = eulerRotation;
				system.Value.UpdateWithSize(sizeFactor);
			}
		}
	}

	public void OnPreCollision(CharacterControllerCustom ccc, ControllerColliderHit hit) {
		// Do nothing
	}

	public void OnWallJump(CharacterControllerCustom ccc) {
		// Since to wall jump the character needs to be sliding, uses the slide effect position
		if (_slideEffect != null)
			wallJump.PlayEffect(_slideEffect.transform.position, _slideEffect.transform.rotation, _characterSize.GetSize());
	}

	public void OnBeginFusion(CharacterFusion originalCharacter, GameObject fusingCharacter, ControllerColliderHit hit) {
		// Do nothing
	}

	public void OnEndFusion(CharacterFusion finalCharacter) {
		// Plays the fusion effect
		Transform characterTransform = finalCharacter.transform;
		fuse.PlayEffect(characterTransform.position, characterTransform.rotation, finalCharacter.GetSize());
	}

	public void OnEnterShootMode(CharacterShoot character) {
		// Do nothing
	}

	public void OnExitShootMode(CharacterShoot character) {
		// Do nothing
	}

	public void OnShoot(CharacterShoot shootingCharacter, GameObject shotCharacter, Vector3 velocity) {
		// Plays the shooting effect
		Transform characterTransform = shotCharacter.transform;
		int size = shootingCharacter.GetComponent<CharacterSize>().GetSize() + shotCharacter.GetComponent<CharacterSize>().GetSize();
		float radius = _controller.radius * size;
		shoot.PlayEffect(characterTransform.position + radius * velocity.normalized, Quaternion.LookRotation(Vector3.forward, velocity), size);
	}

	public void OnIrrigate(Irrigate irrigated, GameObject irrigating, int dropsConsumed) {
		// Plays the irrigation effect
		Transform characterTransform = irrigating.transform;
		irrigate.PlayEffect(characterTransform.position, characterTransform.rotation, _characterSize.GetSize());
	}

	public void OnBeginChase(AIBase enemy, GameObject chasedObject) {
		// Do nothing
	}

	public void OnEndChase(AIBase enemy, GameObject chasedObject) {
		// Do nothing
	}

	public void OnAttack(AIBase enemy, GameObject attackedObject, Vector3 velocity) {
		// Plays the hit effect
		if (attackedObject == gameObject) {
			Transform characterTransform = attackedObject.transform;
			hit.PlayEffect(characterTransform.position, characterTransform.rotation, _characterSize.GetSize());
		}
	}

	public void OnBeingScared(AIBase enemy, GameObject scaringObject, int scaringSize) {
		// Do nothing
	}

	public void OnStateAnimationChange(AnimationState previousState, AnimationState actualState) {
		// Do nothing
	}
}
