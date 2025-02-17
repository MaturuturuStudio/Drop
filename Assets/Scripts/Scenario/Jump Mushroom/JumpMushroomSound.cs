﻿using UnityEngine;

/// <summary>
/// Listener to the mushroom's events that plays the bouncing sound.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(JumpMushroom))]
public class JumpMushroomSound : MonoBehaviour, JumpMushroomListener {

	/// <summary>
	/// If enabled, the pitch of the sound will be modified by the
	/// bouncing character's velocity.
	/// </summary>
	public bool modifyPitch = true;

	/// <summary>
	/// The pitch the sound will have when it bounces to the minimun height.
	/// </summary>
	public float minHeightPitch = 1.0f;

	/// <summary>
	/// The pitch the sound will have when it bounces to the maximum height.
	/// </summary>
	public float maxHeightPitch = 0.5f;

	/// <summary>
	/// Reference to this entity's AudioSource component.
	/// </summary>
	private AudioSource _audioSource;

	void Awake() {
		// Retireves the desired components
		_audioSource = GetComponent<AudioSource>();
	}

	void Start() {
		// Subscribes itself to thepublisher
		GetComponent<JumpMushroom>().AddListener(this);
	}

	public void OnBounce(JumpMushroom mushroom, GameObject bouncingCharacter, Vector3 bounceVelocity, Vector3 collisionPoint, Vector3 collisionNormal) {
		if (modifyPitch)
			_audioSource.pitch = Mathf.Lerp(minHeightPitch, maxHeightPitch, mushroom.GetVelocityFactor(bounceVelocity));
		_audioSource.Play();
	}
}