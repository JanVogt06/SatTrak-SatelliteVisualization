//===========================================================================//
//                       FreeFlyCamera (Version 1.2)                         //
//                        (c) 2019 Sergey Stafeyev                           //
//===========================================================================//

using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FreeFlyCamera : MonoBehaviour
{
    #region UI

    [Space]
    private Vector3 _movementInput = Vector3.zero;

    [SerializeField]
    [Tooltip("The script is currently active")]
    private bool _active = true;

    [Space]

    [SerializeField]
    [Tooltip("Camera rotation by mouse movement is active")]
    private bool _enableRotation = true;

    [SerializeField]
    [Tooltip("Sensitivity of mouse rotation")]
    private float _mouseSense = 1.8f;

    [Space]

    [SerializeField]
    [Tooltip("Camera zooming in/out by 'Mouse Scroll Wheel' is active")]
    private bool _enableTranslation = true;

    [SerializeField]
    [Tooltip("Velocity of camera zooming in/out")]
    private float _translationSpeed = 55f;

    [Space]

    [SerializeField]
    [Tooltip("Camera movement by 'W','A','S','D','Q','E' keys is active")]
    private bool _enableMovement = true;

    [SerializeField]
    [Tooltip("Camera movement speed")]
    private float _movementSpeed = 100f;

    [SerializeField]
    [Tooltip("Speed of the quick camera movement when holding the 'Left Shift' key")]
    private float _boostedSpeed = 200f;

    [SerializeField]
    [Tooltip("Boost speed")]
    private KeyCode _boostSpeed = KeyCode.LeftShift;

    [SerializeField]
    [Tooltip("Move up")]
    private KeyCode _moveUp = KeyCode.E;

    [SerializeField]
    [Tooltip("Move down")]
    private KeyCode _moveDown = KeyCode.Q;

    [Space]

    [SerializeField]
    [Tooltip("Acceleration at camera movement is active")]
    private bool _enableSpeedAcceleration = true;

    [SerializeField]
    [Tooltip("Rate which is applied during camera movement")]
    private float _speedAccelerationFactor = 1.5f;

    [Space]

    [SerializeField]
    [Tooltip("This keypress will move the camera to initialization position")]
    private KeyCode _initPositonButton = KeyCode.R;

    #endregion UI

    private CursorLockMode _wantedMode;

    private float _currentIncrease = 1;
    private float _currentIncreaseMem = 0;

    private Vector3 _initPosition;
    private Vector3 _initRotation;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_boostedSpeed < _movementSpeed)
            _boostedSpeed = _movementSpeed;
    }
#endif

    private void Start()
    {
        _initPosition = transform.position;
        _initRotation = transform.eulerAngles;

        // Initialer Kamera-Modus
        _cursorLocked = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // Apply requested cursor state
    private bool _cursorLocked = true; // Startmodus = Kamera-Modus

    private void SetCursorState()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _cursorLocked = !_cursorLocked;

            Cursor.lockState = _cursorLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !_cursorLocked;
        }
    }

    private void CalculateCurrentIncrease(bool moving)
    {
        _currentIncrease = Time.deltaTime;

        if (!_enableSpeedAcceleration || _enableSpeedAcceleration && !moving)
        {
            _currentIncreaseMem = 0;
            return;
        }

        _currentIncreaseMem += Time.deltaTime * (_speedAccelerationFactor - 1);
        _currentIncrease = Time.deltaTime + Mathf.Pow(_currentIncreaseMem, 3) * Time.deltaTime;
    }

    private void Update()
    {
        if (!_active)
            return;

        SetCursorState();

        // Wenn Cursor freigegeben → UI-Modus → kein Kamerasteuern!
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        // --- Eingabe sammeln ---
        _movementInput = Vector3.zero;

        if (_enableMovement)
        {
            if (Input.GetKey(KeyCode.W)) _movementInput.z += 1;
            if (Input.GetKey(KeyCode.S)) _movementInput.z -= 1;
            if (Input.GetKey(KeyCode.A)) _movementInput.x -= 1;
            if (Input.GetKey(KeyCode.D)) _movementInput.x += 1;
            if (Input.GetKey(_moveUp)) _movementInput.y += 1;
            if (Input.GetKey(_moveDown)) _movementInput.y -= 1;
        }

        if (_enableTranslation)
        {
            transform.Translate(Vector3.forward * Input.mouseScrollDelta.y * Time.deltaTime * _translationSpeed, Space.Self);
        }

        if (_enableRotation)
        {
            float mouseX = Input.GetAxis("Mouse X") * _mouseSense;
            float mouseY = Input.GetAxis("Mouse Y") * _mouseSense;

            _initRotation.y += mouseX;
            _initRotation.x -= mouseY;
            _initRotation.x = Mathf.Clamp(_initRotation.x, -90f, 90f);

            transform.rotation = Quaternion.Euler(_initRotation.x, _initRotation.y, 0f);
        }

        if (Input.GetKeyDown(_initPositonButton))
        {
            transform.position = _initPosition;
            transform.eulerAngles = _initRotation;
        }
    }


    private void FixedUpdate()
    {
        if (!_active)
            return;

        if (_movementInput != Vector3.zero)
        {
            float currentSpeed = Input.GetKey(_boostSpeed) ? _boostedSpeed : _movementSpeed;
            CalculateCurrentIncrease(true);

            transform.Translate(_movementInput.normalized * currentSpeed * _currentIncrease, Space.Self);
        }
        else
        {
            CalculateCurrentIncrease(false);
        }
    }
}
