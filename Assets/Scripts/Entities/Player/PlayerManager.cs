using System.Linq;
using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private AudioClip[] m_JumpAudio;
    [SerializeField] private AudioClip[] m_LandAudio;
    [SerializeField] private AudioClip[] m_TileFootstepsAudio;
    [SerializeField] private AudioClip[] m_GroundFootstepsAudio;

    private Animator m_Animator;
    private AudioSource m_VoiceAudioSource;
    private CapsuleCollider m_CapsuleCollider;
    private Rigidbody m_Rigidbody;
    private InputManager m_InputManager;

    private Camera m_Camera;
    private Transform m_CameraRoot;
    private Transform m_CameraLookAt;
    private AudioSource m_FootstepsAudioSource;

    private string m_CurrentSurfaceTag;

    private int m_HorizontalVelocityHash;
    private int m_VerticalVelocityHash;
    private int m_AerialVelocityHash;
    private int m_IsGroundedHash;

    private bool m_IsCursorLocked;
    private bool m_IsGrounded;
    private bool m_WasGrounded;
    private bool m_IsGroundedForJump;
    private bool m_IsLeftFootGrounded;
    private bool m_IsRightFootGrounded;
    private bool m_isAnyFootGrounded;

    private float m_HorizontalRotation;
    private float m_AerialVelocity;
    private float m_AerialTime;
    private float m_LastJumpTime;
    private float m_LastRightFootHeight;
    private float m_LastLeftFootHeight;
    private float m_MovementVelocity;
    private float m_FalloffWeight;
    private float m_LastPelvisHeight;

    private const float m_Sensitivity = 12;
    private const float m_UpperLookLimit = -45f;
    private const float m_LowerLookLimit = 75f;
    private const float m_CameraLookAtDistance = 15f;
    private const float m_GroundCheckDistance = 1.2f;
    private const float m_JumpCheckDistance = 1f;
    private const float m_WalkVelocity = 2f;
    private const float m_RunVelocity = 4f;
    private const float m_JumpForce = 195f;
    private const float m_JumpAudioPlaySoundChance = 0.3f;
    private const float m_AnimatorBlendTime = 8f;
    private const float m_MaxAerialVelocity = 1f;
    private const float m_AerialIncreaseDuration = 1f;
    private const float m_CapsuleColliderHeight = 1.575f;
    private const float m_CapsuleColliderCenter = 0.7875f;
    private const float m_CapsuleColliderRadius = 0.35f;
    private const float m_FloatDistance = 2f;
    private const float m_StepHeight = 0.25f;
    private const float m_StepReachForce = 20f;
    private const float m_FloatDisableDuration = 0.2f;
    private const float m_LookAtIKWeight = 1f;
    private const float m_BodyLookAtIKWeight = 0.05f;
    private const float m_HeadLookAtIKWeight = 1f;
    private const float m_EyesLookAtIKWeight = 1f;
    private const float m_ClampLookAtIKWeight = 0.5f;
    private const float m_FootIKWeight = 1f;
    private const float m_FootIKHipsWeight = 0.45f;
    private const float m_FootPositionWeight = 1f;
    private const float m_FootRotationWeight = 1f;
    private const float m_MaxStepHeight = 0.4f;
    private const float m_FootRadiusSize = 0.05f;
    private const float m_GroundOffset = 0f;
    private const float m_HipsPositionSpeedValue = 1f;
    private const float m_FeetPositionSpeedValue = 2f;
    private const float m_FeetRotationSpeedDegrees = 90f;

    private Vector2 m_CurrentVelocity; 
    private Vector3 m_AirborneVelocity;
    private Vector3 m_CapsuleColliderCenterInLocalSpace;
    private Vector3 m_LastCharacterPosition;
    private Vector3 m_LeftFootIKPosition;
    private Vector3 m_RightFootIKPosition;
    private Vector3 m_LeftFootNormal;
    private Vector3 m_RightFootNormal;
    
    private Quaternion m_LeftFootIKRotation;
    private Quaternion m_RightFootIKRotation;
    private Quaternion m_LastLeftFootRotation;
    private Quaternion m_LastRightFootRotation;

    private LayerMask m_GroundLayer = ~0;

    private void Awake()
    {
        SetCursorLock(true);
        InitializeComponents();
        InitializeAnimatorHashes();
        UpdateCapsuleColliderData();
        CalculateCapsuleColliderDimensions();
    }

    private void OnAnimatorIK()
    {
        ApplyUpperBodyIK();
        UpdatePelvisHeight();
        UpdateFootIK(AvatarIKGoal.LeftFoot, ref m_LeftFootIKPosition, ref m_LeftFootNormal, ref m_LeftFootIKRotation, ref m_LastLeftFootHeight, ref m_LastLeftFootRotation);
        UpdateFootIK(AvatarIKGoal.RightFoot, ref m_RightFootIKPosition, ref m_RightFootNormal, ref m_RightFootIKRotation, ref m_LastRightFootHeight, ref m_LastRightFootRotation);
    }

    private void LateUpdate()
    {
        Look();
        PositionCameraAndLookTarget();
    }

    private void FixedUpdate()
    {
        ResetVelocity();
        GroundCheck();
        UpdateAerialVelocity();
        FloatCapsuleCollider();
        Move();
        UpdateIKWeight();
        CalculateVelocity();
        PerformFeetRaycastSolver();
    }

    private void Update()
    {
        Jump();
        Interact();
    }

    public void SetCursorLock(bool isLocked)
    {
        m_IsCursorLocked = isLocked;
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !isLocked;
    }

    private void InitializeComponents()
    {
        m_Animator = GetComponent<Animator>();
        m_VoiceAudioSource = GetComponent<AudioSource>();
        m_CapsuleCollider = GetComponent<CapsuleCollider>();
        m_Rigidbody = GetComponent<Rigidbody>();
        m_InputManager = GetComponent<InputManager>();

        m_Camera = GetComponentsInChildren<Camera>(true).FirstOrDefault();
        m_CameraRoot = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals("CameraRoot", System.StringComparison.OrdinalIgnoreCase));
        m_CameraLookAt = GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals("CameraLookAt", System.StringComparison.OrdinalIgnoreCase));
        m_FootstepsAudioSource = GetComponentsInChildren<AudioSource>(true).FirstOrDefault(a => a != m_VoiceAudioSource);
    }

    private void InitializeAnimatorHashes()
    {
        m_HorizontalVelocityHash = Animator.StringToHash("HorizontalVelocity");
        m_VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
        m_AerialVelocityHash = Animator.StringToHash("AerialVelocity");
        m_IsGroundedHash = Animator.StringToHash("IsGrounded");
    }

    private void UpdateCapsuleColliderData()
    {
        m_CapsuleColliderCenterInLocalSpace = m_CapsuleCollider.center;
    }

    private void CalculateCapsuleColliderDimensions()
    {
        m_CapsuleCollider.radius = m_CapsuleColliderRadius;
        m_CapsuleCollider.height = m_CapsuleColliderHeight * (1f - m_StepHeight);

        Vector3 newCapsuleColliderCenter = new Vector3(0f, m_CapsuleColliderCenter + ((m_CapsuleColliderHeight - m_CapsuleCollider.height) / 2f), 0f);

        m_CapsuleCollider.center = newCapsuleColliderCenter;

        UpdateCapsuleColliderData();
    }

    private void Look()
    {
        float lookHorizontal = m_InputManager.Look.x * m_Sensitivity * Time.smoothDeltaTime;
        float lookVertical = m_InputManager.Look.y * m_Sensitivity * Time.smoothDeltaTime;

        m_HorizontalRotation = Mathf.Clamp(m_HorizontalRotation - lookVertical, m_UpperLookLimit, m_LowerLookLimit);
        m_Camera.transform.localRotation = Quaternion.Euler(m_HorizontalRotation, 0f, 0f);
        m_Rigidbody.MoveRotation(m_Rigidbody.rotation * Quaternion.Euler(0f, lookHorizontal, 0f));
    }

    private void PositionCameraAndLookTarget()
    {
        m_Camera.transform.position = m_CameraRoot.position;
        m_CameraLookAt.position = m_Camera.transform.position + m_Camera.transform.forward * m_CameraLookAtDistance;
    }

    private void GroundCheck()
    {
        m_WasGrounded = m_IsGrounded;

        m_IsGrounded = Physics.Raycast(m_Rigidbody.worldCenterOfMass, Vector3.down, out RaycastHit animationHit, m_GroundCheckDistance, m_GroundLayer, QueryTriggerInteraction.Ignore);

        m_IsGroundedForJump = Physics.Raycast(m_Rigidbody.worldCenterOfMass, Vector3.down, m_JumpCheckDistance, m_GroundLayer, QueryTriggerInteraction.Ignore);

        m_CurrentSurfaceTag = m_IsGrounded && animationHit.collider != null ? animationHit.collider.tag : "Untagged";

        m_isAnyFootGrounded = m_IsLeftFootGrounded || m_IsRightFootGrounded;

        m_FalloffWeight = LerpWeightValue(m_FalloffWeight, m_isAnyFootGrounded ? 1f : 0f, 1f, 10f, Time.fixedDeltaTime) * m_FootIKWeight;

        if (m_WasGrounded && !m_IsGrounded)
        {
            m_AirborneVelocity = transform.TransformDirection(new Vector3(m_CurrentVelocity.x, 0f, m_CurrentVelocity.y));
        }

        if (m_IsGrounded)
        {
            m_AirborneVelocity = Vector3.zero;
        }

        m_Animator.SetBool(m_IsGroundedHash, m_IsGrounded);
    }

    private void UpdateAerialVelocity()
    {
        if (!m_IsGrounded)
        {
            m_AerialTime += Time.fixedDeltaTime;
            
            float progress = Mathf.Clamp01(m_AerialTime / m_AerialIncreaseDuration);
            
            m_AerialVelocity = Mathf.Lerp(0f, m_MaxAerialVelocity, progress);
        }
        else
        {
            m_AerialTime = 0f;
            m_AerialVelocity = 0f;
        }

        m_Animator.SetFloat(m_AerialVelocityHash, m_AerialVelocity);
    }

    private void FloatCapsuleCollider()
    {
        if (!m_IsGrounded || Time.time - m_LastJumpTime < m_FloatDisableDuration)
        {
            return;
        }

        if (Physics.Raycast(m_CapsuleCollider.bounds.center, Vector3.down, out RaycastHit hit, m_FloatDistance, m_GroundLayer, QueryTriggerInteraction.Ignore))
        {
            float distanceToFloatingPoint = m_CapsuleColliderCenterInLocalSpace.y * transform.localScale.y - hit.distance;

            if (distanceToFloatingPoint == 0f)
            {
                return;
            }

            float amountToLift = distanceToFloatingPoint * m_StepReachForce - m_Rigidbody.linearVelocity.y;

            Vector3 liftForce = new Vector3(0f, amountToLift, 0f);

            m_Rigidbody.AddForce(liftForce, ForceMode.VelocityChange);
        }
    }

    private void Move()
    {
        float targetVelocity = (m_InputManager.Move == Vector2.zero) ? 0f : (m_InputManager.OnRunHeld() ? m_RunVelocity : m_WalkVelocity);
        
        if (m_IsGrounded)
        { 
            m_CurrentVelocity.x = Mathf.Lerp(m_CurrentVelocity.x, m_InputManager.Move.x * targetVelocity, m_AnimatorBlendTime * Time.fixedDeltaTime);
            m_CurrentVelocity.y = Mathf.Lerp(m_CurrentVelocity.y, m_InputManager.Move.y * targetVelocity, m_AnimatorBlendTime * Time.fixedDeltaTime);

            m_Rigidbody.AddForce(transform.TransformDirection(new Vector3(m_CurrentVelocity.x, 0f, m_CurrentVelocity.y)) - new Vector3(m_Rigidbody.linearVelocity.x, 0f, m_Rigidbody.linearVelocity.z), ForceMode.VelocityChange);
        }
        else
        {
            m_Rigidbody.AddForce(m_AirborneVelocity, ForceMode.VelocityChange);
        }

        m_Animator.SetFloat(m_HorizontalVelocityHash, m_CurrentVelocity.x);
        m_Animator.SetFloat(m_VerticalVelocityHash, m_CurrentVelocity.y);
    }

    private void ResetVelocity()
    {
        if (!m_IsCursorLocked)
        {
            m_CurrentVelocity = Vector2.zero;
            m_Rigidbody.linearVelocity = Vector3.zero;
        }
    }

    private void Jump()
    {
        if (m_InputManager.OnJumpPressed() && m_IsGroundedForJump)
        {
            m_Rigidbody.AddForce(-m_Rigidbody.linearVelocity.y * Vector3.up, ForceMode.VelocityChange);
            m_Rigidbody.AddForce(Vector3.up * m_JumpForce, ForceMode.Impulse);
            m_LastJumpTime = Time.time;

            PlayRandomJumpAudio();
        }
    }

    private void PlayRandomJumpAudio()
    {
        if (Random.value <= m_JumpAudioPlaySoundChance)
        {
            int randomIndex = Random.Range(0, m_JumpAudio.Length);
            
            AudioClip selectedClip = m_JumpAudio[randomIndex];
            
            m_VoiceAudioSource.PlayOneShot(selectedClip);
        }
    }

    private AudioClip[] GetAudioClipsForSurface(string surfaceTag, bool isFootstep)
    {
        switch (surfaceTag)
        {
            case "Tile":
                return isFootstep ? m_TileFootstepsAudio : m_LandAudio;
            case "Ground":
                return isFootstep ? m_GroundFootstepsAudio : m_LandAudio;
            default:
                return isFootstep ? m_GroundFootstepsAudio : m_LandAudio;
        }
    }

    public void PlayFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight <= 0.2f)
        {
            return;
        }

        AudioClip[] footstepClips = GetAudioClipsForSurface(m_CurrentSurfaceTag, true);

        int randomIndex = Random.Range(0, footstepClips.Length);
        
        AudioClip clipToPlay = footstepClips[randomIndex];

        m_FootstepsAudioSource.PlayOneShot(clipToPlay);
    }


    public void OnLand(AnimationEvent animationEvent)
    {
        AudioClip[] landClips = GetAudioClipsForSurface(m_CurrentSurfaceTag, false);

        int randomIndex = Random.Range(0, landClips.Length);
        
        AudioClip clipToPlay = landClips[randomIndex];

        m_FootstepsAudioSource.PlayOneShot(clipToPlay);
    }

    private void Interact()
    {

    }

    private void ApplyUpperBodyIK()
    {
        m_Animator.SetLookAtWeight(m_LookAtIKWeight, m_BodyLookAtIKWeight, m_HeadLookAtIKWeight, m_EyesLookAtIKWeight, m_ClampLookAtIKWeight);
        m_Animator.SetLookAtPosition(m_CameraLookAt.position);
    }

    private void UpdateIKWeight()
    {
        Vector3 characterSpeedVector = (m_LastCharacterPosition - transform.position) / Time.fixedDeltaTime;
        m_MovementVelocity = Mathf.Clamp(characterSpeedVector.magnitude, 1f, characterSpeedVector.magnitude);
        m_LastCharacterPosition = transform.position;
    }

    private void CalculateVelocity()
    {
        Vector3 currentPosition = transform.position;
        Vector3 speedVector = (m_LastCharacterPosition - currentPosition) / Time.fixedDeltaTime;
        m_MovementVelocity = Mathf.Clamp(speedVector.magnitude, 1f, speedVector.magnitude);
        m_LastCharacterPosition = currentPosition;
    }

    private void PerformFeetRaycastSolver()
    {
        SolveFootIK(HumanBodyBones.LeftFoot, ref m_LeftFootIKPosition, ref m_LeftFootNormal, ref m_LeftFootIKRotation, ref m_IsLeftFootGrounded);
        SolveFootIK(HumanBodyBones.RightFoot, ref m_RightFootIKPosition, ref m_RightFootNormal, ref m_RightFootIKRotation, ref m_IsRightFootGrounded);
    }

    private void UpdatePelvisHeight()
    {
        float leftFootOffset = m_LeftFootIKPosition.y - transform.position.y;
        float rightFootOffset = m_RightFootIKPosition.y - transform.position.y;

        float smallestFootOffset = (leftFootOffset < rightFootOffset) ? leftFootOffset : rightFootOffset;

        Vector3 newBodyPosition = m_Animator.bodyPosition;

        float targetPelvisHeight = smallestFootOffset * (m_FootIKHipsWeight * m_FalloffWeight);

        m_LastPelvisHeight = Mathf.MoveTowards(m_LastPelvisHeight, targetPelvisHeight, m_HipsPositionSpeedValue * Time.deltaTime);

        newBodyPosition.y += m_LastPelvisHeight + m_GroundOffset;

        m_Animator.bodyPosition = newBodyPosition;
    }

    private void UpdateFootIK(AvatarIKGoal footGoal, ref Vector3 targetPosition, ref Vector3 surfaceNormal, ref Quaternion targetRotation, ref float lastFootHeight, ref Quaternion lastFootRotation)
    {
        Vector3 animatorIKPosition = m_Animator.GetIKPosition(footGoal);
        Quaternion animatorIKRotation = m_Animator.GetIKRotation(footGoal);

        Vector3 localAnimatorPos = transform.InverseTransformPoint(animatorIKPosition);
        Vector3 localTargetPos = transform.InverseTransformPoint(targetPosition);

        lastFootHeight = Mathf.MoveTowards(lastFootHeight, localTargetPos.y, m_FeetPositionSpeedValue * Time.deltaTime);

        localAnimatorPos.y += lastFootHeight;

        Vector3 smoothedWorldPos = transform.TransformPoint(localAnimatorPos);
        smoothedWorldPos += surfaceNormal * m_GroundOffset;

        Quaternion relative = Quaternion.Inverse(targetRotation * animatorIKRotation) * animatorIKRotation;
        lastFootRotation = Quaternion.RotateTowards(lastFootRotation, Quaternion.Inverse(relative), m_FeetRotationSpeedDegrees * Time.deltaTime);

        Quaternion finalRotation = animatorIKRotation * lastFootRotation;

        m_Animator.SetIKPosition(footGoal, smoothedWorldPos);
        m_Animator.SetIKPositionWeight(footGoal, m_FootPositionWeight * m_FalloffWeight);
        m_Animator.SetIKRotation(footGoal, finalRotation);
        m_Animator.SetIKRotationWeight(footGoal, m_FootRotationWeight * m_FalloffWeight);
    }

    private float LerpWeightValue(float currentValue, float targetValue, float increaseSpeed, float decreaseSpeed, float deltaTime)
    {
        if (currentValue == targetValue)
        {
            return targetValue;
        }

        if (currentValue < targetValue)
        {
            return Mathf.MoveTowards(currentValue, targetValue, (increaseSpeed * m_MovementVelocity) * deltaTime);
        }
        else
        {
            return Mathf.MoveTowards(currentValue, targetValue, (decreaseSpeed * m_MovementVelocity) * deltaTime);
        }
    }

    private void SolveFootIK(HumanBodyBones footBone, ref Vector3 ikPosition, ref Vector3 surfaceNormal, ref Quaternion ikRotation, ref bool isGrounded)
    {
        Transform footTransform = m_Animator.GetBoneTransform(footBone);
        Vector3 raycastOrigin = footTransform.position;

        raycastOrigin.y = transform.position.y + m_MaxStepHeight;

        raycastOrigin -= surfaceNormal * m_GroundOffset;

        float footHeightFromGround = m_MaxStepHeight;

        if (Physics.SphereCast(raycastOrigin, m_FootRadiusSize, Vector3.down, out RaycastHit hit, m_MaxStepHeight * 2f, m_GroundLayer))
        {
            footHeightFromGround = transform.position.y - hit.point.y;

            ikPosition = hit.point;
            surfaceNormal = hit.normal;

            Vector3 rotationAxis = Vector3.Cross(Vector3.up, hit.normal);
            float rotationAngle = Vector3.Angle(Vector3.up, hit.normal);
            ikRotation = Quaternion.AngleAxis(rotationAngle, rotationAxis);
        }

        isGrounded = footHeightFromGround < m_MaxStepHeight;

        if (!isGrounded)
        {
            ikPosition.y = transform.position.y - m_MaxStepHeight;
            ikRotation = Quaternion.identity;
        }
    }
}
