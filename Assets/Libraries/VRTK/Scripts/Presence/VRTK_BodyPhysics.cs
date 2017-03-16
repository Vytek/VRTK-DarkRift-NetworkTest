﻿// Body Physics|Presence|70060
namespace VRTK
{
    using UnityEngine;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Event Payload
    /// </summary>
    /// <param name="target">The target the event is dealing with.</param>
    public struct BodyPhysicsEventArgs
    {
        public GameObject target;
    }

    /// <summary>
    /// Event Payload
    /// </summary>
    /// <param name="sender">this object</param>
    /// <param name="e"><see cref="BodyPhysicsEventArgs"/></param>
    public delegate void BodyPhysicsEventHandler(object sender, BodyPhysicsEventArgs e);

    /// <summary>
    /// The body physics script deals with how a user's body in the scene reacts to world physics and how to handle drops.
    /// </summary>
    /// <remarks>
    /// The body physics creates a rigidbody and collider for where the user is standing to allow physics interactions and prevent walking through walls.
    ///
    /// Upon actually moving in the play area, the rigidbody is set to kinematic to prevent the world from being pushed back in the user's view reducing sickness.
    ///
    /// The body physics script also deals with snapping a user to the nearest floor if they look over a ledge or walk up stairs then it will move the play area to simulate movement in the scene.
    ///
    /// To allow for peeking over a ledge and not falling, a fall restiction can happen by keeping a controller over the existing floor and the snap to the nearest floor will not happen until the controllers are also over the floor.
    /// </remarks>
    /// <example>
    /// `VRTK/Examples/017_CameraRig_TouchpadWalking` has a collection of walls and slopes that can be traversed by the user with the touchpad but the user cannot pass through the objects as they are collidable and the rigidbody physics won't allow the intersection to occur.
    /// </example>
    public class VRTK_BodyPhysics : VRTK_DestinationMarker
    {
        /// <summary>
        /// Options for testing if a play space fall is valid
        /// </summary>
        /// <param name="NoRestriction">Always drop to nearest floor when the headset is no longer over the current standing object.</param>
        /// <param name="LeftController">Don't drop to nearest floor  if the Left Controller is still over the current standing object even if the headset isn't.</param>
        /// <param name="RightController">Don't drop to nearest floor  if the Right Controller is still over the current standing object even if the headset isn't.</param>
        /// <param name="EitherController">Don't drop to nearest floor  if Either Controller is still over the current standing object even if the headset isn't.</param>
        /// <param name="BothControllers">Don't drop to nearest floor only if Both Controllers are still over the current standing object even if the headset isn't.</param>
        public enum FallingRestrictors
        {
            NoRestriction,
            LeftController,
            RightController,
            EitherController,
            BothControllers,
        }

        [Header("Body Collision Settings")]

        [Tooltip("If checked then the body collider and rigidbody will be used to check for rigidbody collisions.")]
        public bool enableBodyCollisions = true;
        [Tooltip("If this is checked then any items that are grabbed with the controller will not collide with the body collider. This is very useful if the user is required to grab and wield objects because if the collider was active they would bounce off the collider.")]
        public bool ignoreGrabbedCollisions = true;
        [Tooltip("The collider which is created for the user is set at a height from the user's headset position. If the collider is required to be lower to allow for room between the play area collider and the headset then this offset value will shorten the height of the generated collider.")]
        public float headsetYOffset = 0.2f;
        [Tooltip("The amount of movement of the headset between the headset's current position and the current standing position to determine if the user is walking in play space and to ignore the body physics collisions if the movement delta is above this threshold.")]
        public float movementThreshold = 0.0015f;
        [Tooltip("The maximum number of samples to collect of headset position before determining if the current standing position within the play space has changed.")]
        public int standingHistorySamples = 5;
        [Tooltip("The `y` distance between the headset and the object being leaned over, if object being leaned over is taller than this threshold then the current standing position won't be updated.")]
        public float leanYThreshold = 0.5f;

        [Header("Snap To Floor Settings")]

        [Tooltip("The layers to ignore when raycasting to find floors.")]
        public LayerMask layersToIgnore = Physics.IgnoreRaycastLayer;
        [Tooltip("A check to see if the drop to nearest floor should take place. If the selected restrictor is still over the current floor then the drop to nearest floor will not occur. Works well for being able to lean over ledges and look down. Only works for falling down not teleporting up.")]
        public FallingRestrictors fallRestriction = FallingRestrictors.NoRestriction;
        [Tooltip("When the `y` distance between the floor and the headset exceeds this distance and `Enable Body Collisions` is true then the rigidbody gravity will be used instead of teleport to drop to nearest floor.")]
        public float gravityFallYThreshold = 1.0f;
        [Tooltip("The `y` distance between the floor and the headset that must change before a fade transition is initiated. If the new user location is at a higher distance than the threshold then the headset blink transition will activate on teleport. If the new user location is within the threshold then no blink transition will happen, which is useful for walking up slopes, meshes and terrains to prevent constant blinking.")]
        public float blinkYThreshold = 0.15f;
        [Tooltip("The amount the `y` position needs to change by between the current floor `y` position and the previous floor `y` position before a change in floor height is considered to have occurred. A higher value here will mean that a `Drop To Floor` will be less likely to happen if the `y` of the floor beneath the user hasn't changed as much as the given threshold.")]
        public float floorHeightTolerance = 0.001f;
        [Range(1, 10)]
        [Tooltip("The amount of rounding on the play area Y position to be applied when checking if falling is occuring.")]
        public int fallCheckPrecision = 5;

        /// <summary>
        /// Emitted when a fall begins.
        /// </summary>
        public event BodyPhysicsEventHandler StartFalling;
        /// <summary>
        /// Emitted when a fall ends.
        /// </summary>
        public event BodyPhysicsEventHandler StopFalling;
        /// <summary>
        /// Emitted when movement in the play area begins.
        /// </summary>
        public event BodyPhysicsEventHandler StartMoving;
        /// <summary>
        /// Emitted when movement in the play area ends
        /// </summary>
        public event BodyPhysicsEventHandler StopMoving;
        /// <summary>
        /// Emitted when the body collider starts colliding with another game object
        /// </summary>
        public event BodyPhysicsEventHandler StartColliding;
        /// <summary>
        /// Emitted when the body collider stops colliding with another game object
        /// </summary>
        public event BodyPhysicsEventHandler StopColliding;

        protected Transform playArea;
        protected Transform headset;
        protected Rigidbody bodyRigidbody;
        protected CapsuleCollider bodyCollider;
        protected VRTK_CollisionTracker collisionTracker;
        protected bool currentBodyCollisionsSetting;
        protected GameObject currentCollidingObject = null;
        protected GameObject currentValidFloorObject = null;

        protected VRTK_BasicTeleport teleporter;
        protected float lastFrameFloorY;
        protected float hitFloorYDelta = 0f;
        protected bool initialFloorDrop = false;
        protected bool resetPhysicsAfterTeleport = false;
        protected bool storedCurrentPhysics = false;
        protected bool retogglePhysicsOnCanFall = false;
        protected bool storedRetogglePhysics;
        protected Vector3 lastPlayAreaPosition = Vector3.zero;
        protected Vector2 currentStandingPosition;
        protected List<Vector2> standingPositionHistory = new List<Vector2>();
        protected float playAreaHeightAdjustment = 0.009f;

        protected bool isFalling = false;
        protected bool isMoving = false;
        protected bool isLeaning = false;
        protected bool onGround = true;
        protected bool preventSnapToFloor = false;
        protected bool generateCollider = false;
        protected bool generateRigidbody = false;
        protected Vector3 playAreaVelocity = Vector3.zero;

        // Draws a sphere for current standing position and a sphere for current headset position.
        // Set to `true` to view the debug spheres.
        protected bool drawDebugGizmo = false;

        /// <summary>
        /// The ArePhysicsEnabled method determines whether the body physics are set to interact with other scene physics objects.
        /// </summary>
        /// <returns>Returns true if the body physics will interact with other scene physics objects and false if the body physics will ignore other scene physics objects.</returns>
        public virtual bool ArePhysicsEnabled()
        {
            return (bodyRigidbody ? !bodyRigidbody.isKinematic : false);
        }

        /// <summary>
        /// The ApplyBodyVelocity method applies a given velocity to the rigidbody attached to the body physics.
        /// </summary>
        /// <param name="velocity">The velocity to apply.</param>
        /// <param name="forcePhysicsOn">If true will toggle the body collision physics back on if enable body collisions is true.</param>
        /// <param name="applyMomentum">If true then the existing momentum of the play area will be applied as a force to the resulting velocity.</param>
        public virtual void ApplyBodyVelocity(Vector3 velocity, bool forcePhysicsOn = false, bool applyMomentum = false)
        {
            if (enableBodyCollisions && forcePhysicsOn)
            {
                TogglePhysics(true);
            }

            if (ArePhysicsEnabled())
            {
                float gravityPush = -0.001f;
                Vector3 appliedGravity = new Vector3(0f, gravityPush, 0f);
                bodyRigidbody.velocity = velocity + appliedGravity;
                if (applyMomentum)
                {
                    float rigidBodyMagnitude = bodyRigidbody.velocity.magnitude;
                    Vector3 appliedMomentum = playAreaVelocity / (rigidBodyMagnitude < 1f ? 1f : rigidBodyMagnitude);
                    bodyRigidbody.AddRelativeForce(appliedMomentum, ForceMode.VelocityChange);
                }
                StartFall(currentValidFloorObject);
            }
        }

        /// <summary>
        /// The ToggleOnGround method sets whether the body is considered on the ground or not.
        /// </summary>
        /// <param name="state">If true then body physics are set to being on the ground.</param>
        public virtual void ToggleOnGround(bool state)
        {
            onGround = state;
        }

        /// <summary>
        /// The PreventSnapToFloor method sets whether the snap to floor mechanic should be used.
        /// </summary>
        /// <param name="state">If true the the snap to floor mechanic will not execute.</param>
        public virtual void TogglePreventSnapToFloor(bool state)
        {
            preventSnapToFloor = state;
        }

        /// <summary>
        /// The IsFalling method returns the falling state of the body.
        /// </summary>
        /// <returns>Returns true if the body is currently falling via gravity or via teleport.</returns>
        public virtual bool IsFalling()
        {
            return isFalling;
        }

        /// <summary>
        /// The IsMoving method returns the moving within play area state of the body.
        /// </summary>
        /// <returns>Returns true if the user is currently walking around their play area space.</returns>
        public virtual bool IsMoving()
        {
            return isMoving;
        }

        /// <summary>
        /// The IsLeaning method returns the leaning state of the user.
        /// </summary>
        /// <returns>Returns true if the user is considered to be leaning over an object.</returns>
        public virtual bool IsLeaning()
        {
            return isLeaning;
        }

        /// <summary>
        /// The OnGround method returns whether the user is currently standing on the ground or not.
        /// </summary>
        /// <returns>Returns true if the play area is on the ground and false if the play area is in the air.</returns>
        public virtual bool OnGround()
        {
            return onGround;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            playArea = VRTK_DeviceFinder.PlayAreaTransform();
            headset = VRTK_DeviceFinder.HeadsetTransform();
            if (playArea)
            {
                lastPlayAreaPosition = playArea.position;
                collisionTracker = playArea.GetComponent<VRTK_CollisionTracker>();
                if (collisionTracker == null)
                {
                    collisionTracker = playArea.gameObject.AddComponent<VRTK_CollisionTracker>();
                }
                ManageCollisionListeners(true);
            }
            if (headset)
            {
                currentStandingPosition = new Vector2(headset.position.x, headset.position.z);
            }
            EnableDropToFloor();
            EnableBodyPhysics();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DisableDropToFloor();
            DisableBodyPhysics();
            ManageCollisionListeners(false);
        }

        protected virtual void FixedUpdate()
        {
            CheckBodyCollisionsSetting();
            if (!isFalling)
            {
                CheckHeadsetMovement();
                SnapToNearestFloor();
            }
            else
            {
                CheckFalling();
            }

            CalculateVelocity();

            lastPlayAreaPosition = (playArea ? playArea.position : Vector3.zero);

            UpdateCollider();
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (!VRTK_PlayerObject.IsPlayerObject(collision.gameObject) && currentValidFloorObject && !currentValidFloorObject.Equals(collision.gameObject))
            {
                currentCollidingObject = collision.gameObject;
                OnStartColliding(SetBodyPhysicsEvent(currentCollidingObject));
            }
        }

        protected virtual void OnTriggerEnter(Collider collider)
        {
            if (!VRTK_PlayerObject.IsPlayerObject(collider.gameObject) && currentValidFloorObject && !currentValidFloorObject.Equals(collider.gameObject))
            {
                currentCollidingObject = collider.gameObject;
                OnStartColliding(SetBodyPhysicsEvent(currentCollidingObject));
            }

        }

        protected virtual void OnCollisionExit(Collision collision)
        {
            if (currentCollidingObject && currentCollidingObject.Equals(collision.gameObject))
            {
                OnStopColliding(SetBodyPhysicsEvent(currentCollidingObject));
                currentCollidingObject = null;
            }
        }

        protected virtual void OnTriggerExit(Collider collider)
        {
            if (currentCollidingObject && currentCollidingObject.Equals(collider.gameObject))
            {
                OnStopColliding(SetBodyPhysicsEvent(currentCollidingObject));
                currentCollidingObject = null;
            }
        }

        protected virtual void OnDrawGizmos()
        {
            if (drawDebugGizmo && headset)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(new Vector3(headset.position.x, headset.position.y - 0.3f, headset.position.z), 0.075f);
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(new Vector3(currentStandingPosition.x, headset.position.y - 0.3f, currentStandingPosition.y), 0.05f);
            }
        }

        protected virtual void ManageCollisionListeners(bool state)
        {
            if (collisionTracker)
            {
                if (state)
                {
                    collisionTracker.CollisionEnter += CollisionTracker_CollisionEnter;
                    collisionTracker.CollisionExit += CollisionTracker_CollisionExit;
                    collisionTracker.TriggerEnter += CollisionTracker_TriggerEnter;
                    collisionTracker.TriggerExit += CollisionTracker_TriggerExit;
                }
                else
                {
                    collisionTracker.CollisionEnter -= CollisionTracker_CollisionEnter;
                    collisionTracker.CollisionExit -= CollisionTracker_CollisionExit;
                    collisionTracker.TriggerEnter -= CollisionTracker_TriggerEnter;
                    collisionTracker.TriggerExit -= CollisionTracker_TriggerExit;
                }
            }
        }

        protected virtual void CollisionTracker_TriggerExit(object sender, CollisionTrackerEventArgs e)
        {
            OnTriggerExit(e.collider);
        }

        protected virtual void CollisionTracker_TriggerEnter(object sender, CollisionTrackerEventArgs e)
        {
            OnTriggerEnter(e.collider);
        }

        protected virtual void CollisionTracker_CollisionExit(object sender, CollisionTrackerEventArgs e)
        {
            OnCollisionExit(e.collision);
        }

        protected virtual void CollisionTracker_CollisionEnter(object sender, CollisionTrackerEventArgs e)
        {
            OnCollisionEnter(e.collision);
        }

        protected virtual void OnStartFalling(BodyPhysicsEventArgs e)
        {
            if (StartFalling != null)
            {
                StartFalling(this, e);
            }
        }

        protected virtual void OnStopFalling(BodyPhysicsEventArgs e)
        {
            if (StopFalling != null)
            {
                StopFalling(this, e);
            }
        }

        protected virtual void OnStartMoving(BodyPhysicsEventArgs e)
        {
            if (StartMoving != null)
            {
                StartMoving(this, e);
            }
        }

        protected virtual void OnStopMoving(BodyPhysicsEventArgs e)
        {
            if (StopMoving != null)
            {
                StopMoving(this, e);
            }
        }

        protected virtual void OnStartColliding(BodyPhysicsEventArgs e)
        {
            if (StartColliding != null)
            {
                StartColliding(this, e);
            }
        }

        protected virtual void OnStopColliding(BodyPhysicsEventArgs e)
        {
            if (StopColliding != null)
            {
                StopColliding(this, e);
            }
        }

        protected virtual BodyPhysicsEventArgs SetBodyPhysicsEvent(GameObject target)
        {
            BodyPhysicsEventArgs e;
            e.target = target;
            return e;
        }

        protected virtual void CalculateVelocity()
        {
            playAreaVelocity = (playArea.position - lastPlayAreaPosition) / Time.fixedDeltaTime;
        }

        protected virtual void TogglePhysics(bool state)
        {
            if (bodyRigidbody)
            {
                bodyRigidbody.isKinematic = !state;
            }
            if (bodyCollider)
            {
                bodyCollider.isTrigger = !state;
            }

            currentBodyCollisionsSetting = state;
        }

        protected virtual void CheckBodyCollisionsSetting()
        {
            if (enableBodyCollisions != currentBodyCollisionsSetting)
            {
                TogglePhysics(enableBodyCollisions);
            }
        }

        protected virtual void CheckFalling()
        {
            if (isFalling && VRTK_SharedMethods.RoundFloat(lastPlayAreaPosition.y, fallCheckPrecision) == VRTK_SharedMethods.RoundFloat(playArea.position.y, fallCheckPrecision))
            {
                StopFall();
            }
        }

        protected virtual void SetCurrentStandingPosition()
        {
            if (playArea && !playArea.transform.position.Equals(lastPlayAreaPosition))
            {
                var playareaDifference = playArea.transform.position - lastPlayAreaPosition;
                currentStandingPosition = new Vector2(currentStandingPosition.x + playareaDifference.x, currentStandingPosition.y + playareaDifference.z);
            }
        }

        protected virtual void SetIsMoving(Vector2 currentHeadsetPosition)
        {
            var moveDistance = Vector2.Distance(currentHeadsetPosition, currentStandingPosition);
            isMoving = (moveDistance > movementThreshold ? true : false);
            if (playArea && (!playArea.transform.position.Equals(lastPlayAreaPosition) || !onGround))
            {
                isMoving = false;
            }
        }

        protected virtual void CheckLean()
        {
            //Cast a ray down from the current standing position
            Vector3 standingDownRayStartPosition = (headset ? new Vector3(currentStandingPosition.x, headset.position.y, currentStandingPosition.y) : Vector3.zero);
            Vector3 rayDirection = (playArea ? -playArea.up : Vector3.zero);
            Ray standingDownRay = new Ray(standingDownRayStartPosition, rayDirection);
            RaycastHit standingDownRayCollision;
            bool standingDownRayHit = Physics.Raycast(standingDownRay, out standingDownRayCollision, Mathf.Infinity, ~layersToIgnore);

            if (standingDownRayHit)
            {
                currentValidFloorObject = standingDownRayCollision.collider.gameObject;
            }

            //Don't bother checking for lean if body collisions are disabled
            if (!headset || !playArea || !enableBodyCollisions)
            {
                return;
            }

            //reset the headset x rotation so the forward ray is always horizontal regardless of the headset rotation
            var storedRotation = headset.rotation;
            headset.rotation = new Quaternion(0f, headset.rotation.y, headset.rotation.z, headset.rotation.w);

            var forwardLengthAddition = 0.05f;
            var forwardLength = bodyCollider.radius + forwardLengthAddition;

            Ray forwardRay = new Ray(headset.position, headset.forward);
            RaycastHit forwardRayCollision;

            // Cast a ray forward just outside the body collider radius to see if anything is blocking your path
            if (!Physics.Raycast(forwardRay, out forwardRayCollision, forwardLength, ~layersToIgnore))
            {
                if (standingDownRayHit)
                {
                    Vector3 rayDownStartPosition = headset.position + headset.forward * forwardLength;
                    Ray downRay = new Ray(rayDownStartPosition, -playArea.up);
                    RaycastHit downRayCollision;

                    //Cast a ray down from the end of the forward ray position
                    if (Physics.Raycast(downRay, out downRayCollision, Mathf.Infinity, ~layersToIgnore))
                    {
                        var distancePrecision = 1000f;
                        float rayDownDelta = (Mathf.Round((standingDownRayCollision.distance - downRayCollision.distance) * distancePrecision) / distancePrecision);
                        float playAreaPositionDelta = Mathf.Round(Vector3.Distance(playArea.transform.position, lastPlayAreaPosition) * distancePrecision) / distancePrecision;

                        //If the play area is not moving and the delta between the down rays is greater than 0 then you're probably walking forward over something you can stand on
                        isMoving = (onGround && playAreaPositionDelta <= 0.002f && rayDownDelta > 0f ? true : isMoving);

                        //If the item your standing over is too high to walk on top of then allow leaning over it.
                        isLeaning = (onGround && rayDownDelta > leanYThreshold ? true : false);
                    }
                }
            }

            //put the headset rotation back
            headset.rotation = storedRotation;
        }

        protected virtual void UpdateStandingPosition(Vector2 currentHeadsetPosition)
        {
            standingPositionHistory.Add(currentHeadsetPosition);

            if (standingPositionHistory.Count > standingHistorySamples)
            {
                if (!isLeaning && !currentCollidingObject)
                {
                    var resetStandingPosition = true;
                    for (int i = 0; i < standingHistorySamples; i++)
                    {
                        var currentDistance = Vector2.Distance(standingPositionHistory[i], standingPositionHistory[standingHistorySamples]);
                        resetStandingPosition = (currentDistance <= movementThreshold ? resetStandingPosition : false);
                    }

                    currentStandingPosition = (resetStandingPosition ? currentHeadsetPosition : currentStandingPosition);
                }
                standingPositionHistory.Clear();
            }
        }

        protected virtual void CheckHeadsetMovement()
        {
            var currentIsMoving = isMoving;
            var currentHeadsetPosition = (headset ? new Vector2(headset.position.x, headset.position.z) : Vector2.zero);
            SetCurrentStandingPosition();
            SetIsMoving(currentHeadsetPosition);
            CheckLean();
            UpdateStandingPosition(currentHeadsetPosition);
            if (enableBodyCollisions)
            {
                TogglePhysics(!isMoving);
            }

            if (currentIsMoving != isMoving)
            {
                MovementChanged(isMoving);
            }
        }

        protected virtual void MovementChanged(bool movementState)
        {
            if (movementState)
            {
                OnStartMoving(SetBodyPhysicsEvent(null));
            }
            else
            {
                OnStopMoving(SetBodyPhysicsEvent(null));
            }
        }

        protected virtual void EnableDropToFloor()
        {
            initialFloorDrop = false;
            retogglePhysicsOnCanFall = false;
            teleporter = GetComponent<VRTK_BasicTeleport>();
            if (teleporter)
            {
                teleporter.Teleported += Teleporter_Teleported;
            }
        }

        protected virtual void DisableDropToFloor()
        {
            if (teleporter)
            {
                teleporter.Teleported -= Teleporter_Teleported;
            }
        }

        protected virtual void Teleporter_Teleported(object sender, DestinationMarkerEventArgs e)
        {
            initialFloorDrop = false;
            StopFall();
            if (resetPhysicsAfterTeleport)
            {
                TogglePhysics(storedCurrentPhysics);
            }
        }

        protected virtual void EnableBodyPhysics()
        {
            currentBodyCollisionsSetting = enableBodyCollisions;

            CreateCollider();
            InitControllerListeners(VRTK_DeviceFinder.GetControllerLeftHand(), true);
            InitControllerListeners(VRTK_DeviceFinder.GetControllerRightHand(), true);
        }

        protected virtual void DisableBodyPhysics()
        {
            DestroyCollider();
            InitControllerListeners(VRTK_DeviceFinder.GetControllerLeftHand(), false);
            InitControllerListeners(VRTK_DeviceFinder.GetControllerRightHand(), false);
        }

        protected virtual void CreateCollider()
        {
            generateCollider = false;
            generateRigidbody = false;

            if (!playArea)
            {
                Debug.LogError("No play area could be found. Have you selected a valid Boundaries SDK in the SDK Manager? If you are unsure, then click the GameObject with the `VRTK_SDKManager` script attached to it in Edit Mode and select a Boundaries SDK from the dropdown.");
                return;
            }

            VRTK_PlayerObject.SetPlayerObject(playArea.gameObject, VRTK_PlayerObject.ObjectTypes.CameraRig);
            bodyRigidbody = playArea.GetComponent<Rigidbody>();
            if (bodyRigidbody == null)
            {
                generateRigidbody = true;
                bodyRigidbody = playArea.gameObject.AddComponent<Rigidbody>();
                bodyRigidbody.mass = 100f;
                bodyRigidbody.freezeRotation = true;
            }

            bodyCollider = playArea.GetComponent<CapsuleCollider>();
            if (bodyCollider == null)
            {
                generateCollider = true;
                bodyCollider = playArea.gameObject.AddComponent<CapsuleCollider>();
                bodyCollider.center = new Vector3(0f, 1f, 0f);
                bodyCollider.height = 1f;
                bodyCollider.radius = 0.15f;
            }

            if (playArea.gameObject.layer == 0)
            {
                playArea.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
            }
            TogglePhysics(enableBodyCollisions);
        }

        protected virtual void DestroyCollider()
        {
            if (generateRigidbody)
            {
                Destroy(bodyRigidbody);
            }

            if (generateCollider)
            {
                Destroy(bodyCollider);
            }
        }

        protected virtual void UpdateCollider()
        {
            if (bodyCollider)
            {
                var newpresenceColliderYSize = (headset ? headset.transform.localPosition.y - headsetYOffset : 0f);
                var newpresenceColliderYCenter = Mathf.Max((newpresenceColliderYSize / 2) + playAreaHeightAdjustment, bodyCollider.radius + playAreaHeightAdjustment);

                if (headset && bodyCollider)
                {
                    bodyCollider.height = Mathf.Max(newpresenceColliderYSize, bodyCollider.radius);
                    bodyCollider.center = new Vector3(headset.localPosition.x, newpresenceColliderYCenter, headset.localPosition.z);
                }
            }
        }

        protected virtual void InitControllerListeners(GameObject mappedController, bool state)
        {
            if (mappedController)
            {
                IgnoreCollisions(mappedController.GetComponentsInChildren<Collider>(), true);

                var grabbingController = mappedController.GetComponent<VRTK_InteractGrab>();
                if (grabbingController && ignoreGrabbedCollisions)
                {
                    if (state)
                    {
                        grabbingController.ControllerGrabInteractableObject += new ObjectInteractEventHandler(OnGrabObject);
                        grabbingController.ControllerUngrabInteractableObject += new ObjectInteractEventHandler(OnUngrabObject);
                    }
                    else
                    {
                        grabbingController.ControllerGrabInteractableObject -= new ObjectInteractEventHandler(OnGrabObject);
                        grabbingController.ControllerUngrabInteractableObject -= new ObjectInteractEventHandler(OnUngrabObject);
                    }
                }
            }
        }

        protected virtual IEnumerator RestoreCollisions(GameObject obj)
        {
            yield return new WaitForEndOfFrame();
            if (obj)
            {
                var objScript = obj.GetComponent<VRTK_InteractableObject>();
                if (objScript && !objScript.IsGrabbed())
                {
                    IgnoreCollisions(obj.GetComponentsInChildren<Collider>(), false);
                }
            }
        }

        protected virtual void IgnoreCollisions(Collider[] colliders, bool state)
        {
            if (playArea)
            {
                var collider = playArea.GetComponent<Collider>();
                if (collider.gameObject.activeInHierarchy)
                {
                    foreach (var controllerCollider in colliders)
                    {
                        if (controllerCollider.gameObject.activeInHierarchy)
                        {
                            Physics.IgnoreCollision(collider, controllerCollider, state);
                        }
                    }
                }
            }
        }

        protected virtual void OnGrabObject(object sender, ObjectInteractEventArgs e)
        {
            if (e.target)
            {
                StopCoroutine("RestoreCollisions");
                IgnoreCollisions(e.target.GetComponentsInChildren<Collider>(), true);
            }
        }

        protected virtual void OnUngrabObject(object sender, ObjectInteractEventArgs e)
        {
            if (gameObject.activeInHierarchy && playArea.gameObject.activeInHierarchy)
            {
                StartCoroutine(RestoreCollisions(e.target));
            }
        }

        protected virtual bool FloorIsGrabbedObject(RaycastHit collidedObj)
        {
            var obj = collidedObj.transform.GetComponent<VRTK_InteractableObject>();
            return (obj && obj.IsGrabbed());
        }

        protected virtual bool FloorHeightChanged(float currentY)
        {
            var yDelta = Mathf.Abs(currentY - lastFrameFloorY);
            return (yDelta > floorHeightTolerance);
        }

        protected virtual bool ValidDrop(bool rayHit, RaycastHit rayCollidedWith, float floorY)
        {
            return (rayHit && teleporter && teleporter.ValidLocation(rayCollidedWith.transform, rayCollidedWith.point) && !FloorIsGrabbedObject(rayCollidedWith) && FloorHeightChanged(floorY));
        }

        protected virtual float ControllerHeightCheck(GameObject controllerObj)
        {
            Ray ray = new Ray(controllerObj.transform.position, -playArea.up);
            RaycastHit rayCollidedWith;
            Physics.Raycast(ray, out rayCollidedWith, Mathf.Infinity, ~layersToIgnore);
            return controllerObj.transform.position.y - rayCollidedWith.distance;
        }

        protected virtual bool ControllersStillOverPreviousFloor()
        {
            if (fallRestriction == FallingRestrictors.NoRestriction)
            {
                return false;
            }

            var controllerDropThreshold = 0.05f;
            var rightController = VRTK_DeviceFinder.GetControllerRightHand();
            var leftController = VRTK_DeviceFinder.GetControllerLeftHand();
            var previousY = playArea.position.y;
            var rightCheck = (rightController.activeInHierarchy && Mathf.Abs(ControllerHeightCheck(rightController) - previousY) < controllerDropThreshold);
            var leftCheck = (leftController.activeInHierarchy && Mathf.Abs(ControllerHeightCheck(leftController) - previousY) < controllerDropThreshold);

            if (fallRestriction == FallingRestrictors.LeftController)
            {
                rightCheck = false;
            }

            if (fallRestriction == FallingRestrictors.RightController)
            {
                leftCheck = false;
            }

            if (fallRestriction == FallingRestrictors.BothControllers)
            {
                return (rightCheck && leftCheck);
            }

            return (rightCheck || leftCheck);
        }

        protected virtual void SnapToNearestFloor()
        {
            if (!preventSnapToFloor && (enableBodyCollisions || enableTeleport) && headset && headset.transform.position.y > playArea.position.y)
            {
                Ray ray = new Ray(headset.transform.position, -playArea.up);
                RaycastHit rayCollidedWith;
                bool rayHit = Physics.Raycast(ray, out rayCollidedWith, Mathf.Infinity, ~layersToIgnore);
                float hitFloorY = headset.transform.position.y - rayCollidedWith.distance;
                hitFloorYDelta = playArea.position.y - hitFloorY;

                if (initialFloorDrop && (ValidDrop(rayHit, rayCollidedWith, hitFloorY) || retogglePhysicsOnCanFall))
                {
                    storedCurrentPhysics = ArePhysicsEnabled();
                    resetPhysicsAfterTeleport = false;
                    TogglePhysics(false);

                    HandleFall(hitFloorY, rayCollidedWith);
                }
                initialFloorDrop = true;
                lastFrameFloorY = hitFloorY;
            }
        }

        protected virtual bool PreventFall(float hitFloorY)
        {
            return (hitFloorY < playArea.position.y && ControllersStillOverPreviousFloor());
        }

        protected virtual void HandleFall(float hitFloorY, RaycastHit rayCollidedWith)
        {
            if (PreventFall(hitFloorY))
            {
                if (!retogglePhysicsOnCanFall)
                {
                    retogglePhysicsOnCanFall = true;
                    storedRetogglePhysics = storedCurrentPhysics;
                }
            }
            else
            {
                if (retogglePhysicsOnCanFall)
                {
                    storedCurrentPhysics = storedRetogglePhysics;
                    retogglePhysicsOnCanFall = false;
                }

                if (enableBodyCollisions && (!teleporter || !enableTeleport || hitFloorYDelta > gravityFallYThreshold))
                {
                    GravityFall(rayCollidedWith);
                }
                else if (teleporter && enableTeleport)
                {
                    TeleportFall(hitFloorY, rayCollidedWith);
                }
            }
        }

        protected virtual void StartFall(GameObject targetFloor)
        {
            isFalling = true;
            isMoving = false;
            isLeaning = false;
            onGround = false;
            OnStartFalling(SetBodyPhysicsEvent(targetFloor));
        }

        protected virtual void StopFall()
        {
            isFalling = false;
            onGround = true;
            OnStopFalling(SetBodyPhysicsEvent(null));
        }

        protected virtual void GravityFall(RaycastHit rayCollidedWith)
        {
            StartFall(rayCollidedWith.collider.gameObject);
            TogglePhysics(true);
            ApplyBodyVelocity(Vector3.zero);
        }

        protected virtual void TeleportFall(float floorY, RaycastHit rayCollidedWith)
        {
            StartFall(rayCollidedWith.collider.gameObject);
            var currentFloor = rayCollidedWith.transform.gameObject;
            var newPosition = new Vector3(playArea.position.x, floorY, playArea.position.z);
            var originalblinkTransitionSpeed = teleporter.blinkTransitionSpeed;

            teleporter.blinkTransitionSpeed = (Mathf.Abs(hitFloorYDelta) > blinkYThreshold ? originalblinkTransitionSpeed : 0f);
            OnDestinationMarkerSet(SetDestinationMarkerEvent(rayCollidedWith.distance, currentFloor.transform, rayCollidedWith, newPosition, uint.MaxValue, true));
            teleporter.blinkTransitionSpeed = originalblinkTransitionSpeed;

            resetPhysicsAfterTeleport = true;
        }
    }
}