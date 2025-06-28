using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private PlayerInput m_PlayerInput;

    private InputActionMap m_CurrentActionMap;
    private InputAction m_MoveAction;
    private InputAction m_LookAction;
    private InputAction m_RunAction;
    private InputAction m_JumpAction;
    private InputAction m_InteractAction;
    private InputAction m_InventoryAction;

    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }

    private void Awake()
    {
        InitializeComponents();
        InitializeCurrentActionMap();
        FindActions();
        SetUpVectorActions();
    }

    private void OnEnable()
    {
        m_CurrentActionMap.Enable();
    }

    private void OnDisable()
    {
        m_CurrentActionMap.Disable();
    }

    private void InitializeComponents()
    {
        m_PlayerInput = GetComponent<PlayerInput>();
    }

    private void InitializeCurrentActionMap()
    {
        m_CurrentActionMap = m_PlayerInput.currentActionMap;
    }

    private void FindActions()
    {
        m_MoveAction = m_CurrentActionMap.FindAction("Move");
        m_LookAction = m_CurrentActionMap.FindAction("Look");
        m_RunAction = m_CurrentActionMap.FindAction("Run");
        m_JumpAction = m_CurrentActionMap.FindAction("Jump");
        m_InteractAction = m_CurrentActionMap.FindAction("Interact");
        m_InventoryAction = m_CurrentActionMap.FindAction("Inventory");
    }

    private void SetUpVectorActions()
    {
        m_MoveAction.performed += ctx => Move = ctx.ReadValue<Vector2>();
        m_MoveAction.canceled += ctx => Move = Vector2.zero;

        m_LookAction.performed += ctx => Look = ctx.ReadValue<Vector2>();
        m_LookAction.canceled += ctx => Look = Vector2.zero;
    }

    public bool OnRunPressed()
    {
        return m_RunAction.WasPressedThisFrame();
    }

    public bool OnRunHeld()
    {
        return m_RunAction.IsPressed();
    }

    public bool OnRunFinished()
    {
        return m_JumpAction.WasReleasedThisFrame();
    }

    public bool OnJumpPressed()
    {
        return m_JumpAction.WasPressedThisFrame();
    }

    public bool OnJumpHeld()
    {
        return m_JumpAction.IsPressed();
    }

    public bool OnJumpFinished()
    {
        return m_JumpAction.WasReleasedThisFrame();
    }

    public bool OnInteractPressed()
    {
        return m_InteractAction.WasPressedThisFrame();
    }

    public bool OnInteractHeld()
    {
        return m_InteractAction.IsPressed();
    }

    public bool OnInteractFinished()
    {
        return m_InteractAction.WasReleasedThisFrame();
    }

    public bool OnInventoryPressed()
    {
        return m_InventoryAction.WasPressedThisFrame();
    }

    public bool OnInventoryHeld()
    {
        return m_InventoryAction.IsPressed();
    }

    public bool OnInventoryFinished()
    {
        return m_InventoryAction.WasReleasedThisFrame();
    }
}
