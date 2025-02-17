﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[ExecuteInEditMode]
public class WaterRepulsion : MonoBehaviour {
    #region Public attributes
    /// <summary>
    /// Delay
    /// </summary>
    public float delay = 0.8f;
    //Launch data
    public LaunchCharacter launch = new LaunchCharacter();
    #endregion

    #region Private attributes
    /// <summary>
    /// Drops that touched the water
    /// </summary>
    private List<GameObject> _enteredDrop;
    /// <summary>
    /// Drop expeled
    /// </summary>
    private List<GameObject> _expelDrop;

    /// <summary>
    /// Bounds of the water
    /// </summary>
    private Bounds _ownCollider;

	/// <summary>
	/// List of listeners registered to this component's events.
	/// </summary>
	private List<WaterRepulsionListener> _listeners = new List<WaterRepulsionListener>();
    #endregion

    #region Methods

    /// <summary>
    /// Subscribes a listener to the components's events.
    /// Returns false if the listener was already subscribed.
    /// </summary>
    /// <param name="listener">The listener to subscribe</param>
    /// <returns>If the listener was successfully subscribed</returns>
    public bool AddListener(WaterRepulsionListener listener) {
		if (_listeners.Contains(listener))
			return false;
		_listeners.Add(listener);
		return true;
	}

	/// <summary>
	/// Unsubscribes a listener to the components's events.
	/// Returns false if the listener wasn't subscribed yet.
	/// </summary>
	/// <param name="listener">The listener to unsubscribe</param>
	/// <returns>If the listener was successfully unsubscribed</returns>
	public bool RemoveListener(WaterRepulsionListener listener) {
		if (!_listeners.Contains(listener))
			return false;
		_listeners.Remove(listener);
		return true;
	}

	// Use this for initialization
	void Start () {
        //get the collider
        _ownCollider = GetComponent<Collider>().bounds;
        //create list
        _enteredDrop = new List<GameObject>();
        _expelDrop = new List<GameObject>();
    }

    public void Update() {
        //no drop? get out
        if (_enteredDrop.Count == 0) return;
        if (!Application.isPlaying) return;

        //for every drop in water...
        for (int i = _enteredDrop.Count; i > 0; i--) {
            GameObject drop = _enteredDrop[i-1];
            //get position and bounds
            Vector3 position = drop.transform.position;
            float halfSize = drop.GetComponent<CharacterSize>().GetSize() * 0.5f;
            //get four direction of drop
            Vector3[] vertices = new Vector3[4];
            vertices[0] = position;
            vertices[0].x -= halfSize;
            vertices[1] = position;
            vertices[1].x += halfSize;
            vertices[2] = position;
            vertices[2].y -= halfSize;
            vertices[3] = position;
            vertices[3].y += halfSize;


            //check if all points are inside the water
            bool result = true;
            for(int j=0; j<vertices.Length && result; j++) {
                result = _ownCollider.Contains(vertices[j]);
            }

            //is inside? get the drop out!
            if (result) {
                _enteredDrop.RemoveAt(i-1);
                //start the delay
                StartCoroutine(ExpelDrop(drop));
            }
        }
    }

    

    /// <summary>
    /// Check the drop inside the water
    /// </summary>
    /// <param name="other"></param>
    public void OnTriggerEnter(Collider other) {
        //get the component if is a drop
        GameObject drop = other.gameObject;
        if (drop.tag != Tags.Player) return;
        
        if (!_expelDrop.Contains(drop)) {
            _enteredDrop.Add(drop);
            //position
            Vector3 position = drop.transform.position;
            position.z = 0;


			// Notifies the listeners
			foreach (WaterRepulsionListener listener in _listeners)
				listener.OnWaterEnter(this, drop);
		}
    }

    /// <summary>
    /// Remove the drops that is outside the water
    /// </summary>
    /// <param name="other"></param>
    public void OnTriggerExit(Collider other) {
        //get the component if is a drop
        GameObject drop = other.gameObject;
        if (drop.tag != Tags.Player) return;
        _enteredDrop.Remove(drop);
        _expelDrop.Remove(drop);
    }

    /// <summary>
    /// Get the bounds of the collider
    /// </summary>
    /// <returns></returns>
	public Bounds GetCollider() {
        return _ownCollider;
    }

    /// <summary>
    /// Draw the launch
    /// </summary>
    public void OnDrawGizmos() {
        launch.OnDrawGizmos();
    }

    /// <summary>
    /// Check the given drop and expel it
    /// </summary>
    /// <param name="drop"></param>
    /// <returns></returns>
    private IEnumerator ExpelDrop(GameObject drop) {
        _expelDrop.Add(drop);
        drop.SetActive(false);
        yield return new WaitForSeconds(delay);

        //put drop on point expulsion
        drop.transform.position = launch.pointOrigin.position;
        drop.SetActive(true);
        CharacterControllerCustom controller = drop.GetComponent<CharacterControllerCustom>();

		//set particle effect
		//int scale=(int)(drop.transform.localScale.x);
		//position
		Vector3 position=drop.transform.position;
        position.z = 0;
        //ParticleExit(position, scale);
		

        //send it flying (stop previous flying)
        controller.StopFlying();
        Vector3 velocity = launch.GetNeededVelocityVector();
        controller.SendFlying(velocity);

		// Notifies the listeners
		foreach (WaterRepulsionListener listener in _listeners)
			listener.OnWaterExit(this, drop, velocity);
	}

    
    #endregion
}
