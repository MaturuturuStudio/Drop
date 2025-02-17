﻿using UnityEngine;
using System.Collections;

public class AIMethods {

    /// <summary>
    /// Move the enemy
    /// </summary>
    /// <param name="originalPosition"></param>
    /// <returns></returns>
    public static Vector3 MoveEnemy(Vector3 originalPosition, Vector3 target, FollowType followType, bool onFloor, float speed) {
        Vector3 finalPosition = originalPosition;

        // Moves the entity using the right function
        switch (followType) {
            case FollowType.MoveTowards:
                finalPosition = Vector3.MoveTowards(originalPosition, target, speed * Time.deltaTime);
                break;
            case FollowType.Lerp:
                finalPosition = Vector3.Lerp(originalPosition, target, speed * Time.deltaTime);
                break;
        }

        if (onFloor)
            finalPosition.y = originalPosition.y;

        return finalPosition;
    }

    /// <summary>
    /// Rotate the entity to target
    /// The target is the new position of the entity after moving
    /// </summary>
    /// <param name="originalPosition">position the entity is</param>
    /// <param name="finalPosition">target point</param>
    public static void RotateEnemyTowards(GameObject enemy, AxisBoolean fixedRotation, Quaternion initialRotation, float speedRotation, Vector3 originalPosition, Vector3 finalPosition) {
        Quaternion finalRotation = Quaternion.identity;

        Vector3 relativePos = finalPosition - originalPosition;
		Quaternion rotation;
		if (relativePos != Vector3.zero)
			rotation = Quaternion.LookRotation(relativePos);
		else
			rotation = Quaternion.identity;

			finalRotation = Quaternion.RotateTowards(enemy.transform.rotation, rotation, speedRotation*Time.deltaTime);
        Quaternion zero = initialRotation;
        Quaternion rotationZero = Quaternion.RotateTowards(enemy.transform.rotation, zero, speedRotation * Time.deltaTime);

        Vector3 finalEuler = finalRotation.eulerAngles;
        if (fixedRotation.axisX) {
            finalEuler.x = rotationZero.eulerAngles.x;
        }

        if (fixedRotation.axisY) {
            finalEuler.y = rotationZero.eulerAngles.y;
        }

        if (fixedRotation.axisZ) {
            finalEuler.z = rotationZero.eulerAngles.z;
        }
        finalRotation.eulerAngles = finalEuler;
        enemy.transform.rotation = finalRotation;
    }

    /// <summary>
    /// Rotate the entity to target
    /// The target is the new position of the entity after moving
    /// </summary>
    /// <param name="originalPosition">position the entity is</param>
    /// <param name="finalPosition">target point</param>
    /// <param name="enemy"></param>
    /// <param name="fixedRotation"></param>
    /// <param name="initialRotation"></param>
    /// <param name="rotationVelocity"></param>
    public static void RotateEnemySlerp(GameObject enemy, AxisBoolean fixedRotation, Quaternion initialRotation, float rotationVelocity, Vector3 originalPosition, Vector3 finalPosition) {
        Quaternion finalRotation = Quaternion.identity;

        Vector3 relativePos = finalPosition - originalPosition;
        Quaternion rotation;
        if (relativePos != Vector3.zero)
            rotation = Quaternion.LookRotation(relativePos);
        else
            rotation = Quaternion.identity;

        finalRotation = Quaternion.Slerp(enemy.transform.rotation, rotation, rotationVelocity * Time.deltaTime);
        Quaternion zero = initialRotation;
        Quaternion rotationZero = Quaternion.Slerp(enemy.transform.rotation, zero, rotationVelocity * Time.deltaTime);

        Vector3 finalEuler = finalRotation.eulerAngles;
        if (fixedRotation.axisX) {
            finalEuler.x = rotationZero.eulerAngles.x;
        }

        if (fixedRotation.axisY) {
            finalEuler.y = rotationZero.eulerAngles.y;
        }

        if (fixedRotation.axisZ) {
            finalEuler.z = rotationZero.eulerAngles.z;
        }
        finalRotation.eulerAngles = finalEuler;


        enemy.transform.rotation = finalRotation;
    }

    /// <summary>
    /// Rotate the enemy to the target rotation
    /// </summary>
    /// <param name="target"></param>
    public static void RotateEnemy(GameObject enemy, Quaternion target, float speedRotation, bool useFinalOrientation) {
        if (useFinalOrientation) {
            Quaternion finalRotation = Quaternion.identity;
            finalRotation = Quaternion.RotateTowards(enemy.transform.rotation, target, speedRotation * Time.deltaTime);
            enemy.transform.rotation = finalRotation;
        }
    }

    public static float GetMinimumDistance(float speed, float rotationVelocity) {
        float time = 360 / rotationVelocity;
        float longitude = speed * time;
        float radius = longitude / (2 * Mathf.PI);
        return radius;
    }

    public static Vector3 RepelDrop(Transform originTransform, Vector3 destiny, LaunchCharacter launcher) {
        Vector3 origin = originTransform.position;
        //check the angle, at right or at left?
        float angle = Mathf.Atan2(destiny.y - origin.y, destiny.x - origin.x) * 180 / Mathf.PI;
        if (angle < 0) angle *= -1;
        if (angle > 90) {
            angle -= ((angle - 90) / 2);
        } else {
            angle += ((90 - angle) / 2);
        }
        launcher.SetAngle(angle);
        launcher.pointOrigin = originTransform;
        return launcher.GetNeededVelocityVector();
    }

    /// <summary>
    /// True if reached the target rotation
    /// </summary>
    /// <param name="target"></param>
    /// <param name="toleranceDegree"></param>
    /// <returns></returns>
    public static bool CheckTargetRotation(GameObject enemy, Quaternion target, float toleranceDegree) {
        float angle = Quaternion.Angle(enemy.transform.rotation, target);
        return angle < toleranceDegree;
    }

    /// <summary>
    /// True if reached the target point
    /// </summary>
    /// <param name="target"></param>
    /// <param name="minimumDistance"></param>
    /// <returns></returns>
    public static bool CheckTargetPoint(GameObject enemy, Vector3 target, bool onFloor, float minimumDistance) {
        // Checks if the entity is close enough to the target point
        Vector3 position = enemy.transform.position;
        //ignore axis if on floor
        if (onFloor)
            position.y = target.y;
        float squaredDistance = (position - target).sqrMagnitude;
        float distanceGoal = minimumDistance * minimumDistance;
        // The squared distance is used because a multiplication is cheaper than a square root
        return squaredDistance < distanceGoal;
    }

    /// <summary>
    /// Check drop size (go away or detect)
    /// </summary>
    public static void CheckDrop(Animator animator, int sizeLimitDrop) {
        int size = animator.GetInteger("SizeDrop");
        int sizeLimit = sizeLimitDrop;
        if (sizeLimit > 0 && size >= sizeLimit) {
            animator.SetBool("GoAway", true);
        } else if ((sizeLimit <= 0 || size < sizeLimit) && size > 0) {
            animator.SetBool("Detect", true);
        }
    }

    public static Collider[] DropInTriggerArea(Region triggerArea, Vector3 position, LayerMask layerCast) {
        //position left bottom
        Vector3 center = triggerArea.origin + position;
        Vector3 halfSize = triggerArea.size / 2;
        //center it
        center.x += halfSize.x;
        center.y += halfSize.y;
        center.z += halfSize.z;
        Collider[] drops = Physics.OverlapBox(center, halfSize, Quaternion.identity, layerCast, QueryTriggerInteraction.Ignore);
        return drops;

    }

    /// <summary>
    /// Clear all parameters of animator (booleans)
    /// </summary>
    /// <param name="animator"></param>
    public static void ClearAnimatorParameters(Animator animator) {
        animator.SetBool("Detect", false);
        animator.SetBool("Timer", false);
        animator.SetBool("GoAway", false);
        animator.SetBool("Reached", false);
        animator.SetBool("Near", false);
        animator.SetBool("Recolect", false);
        animator.SetBool("AirAttack", false);
    }
}
