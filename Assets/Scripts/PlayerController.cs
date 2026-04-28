using UnityEngine;
using System;
using System.Collections;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float maxspeed = 10f;  // Tốc độ chạy nhanh nhất
    [SerializeField] private float rotationSpeed = 360f;    // Tốc độ xoay người (độ/giây)

    [SerializeField] private float accelerationFactor = 5f; // Độ tăng tốc (nhấn nút thì đạt tốc độ cao sau bao lâu)
    [SerializeField] private float decelerationFactor = 10f; // Độ giảm tốc (thả nút thì dừng lại sau bao lâu)

    [SerializeField] private float gravity = -9.81f; // Lực hút trái đất giả lập

    [Header("Animation")]
    [SerializeField] private Animator anim;

    [Header("Dash")]
    [SerializeField] private float dashingCooldown = 1.5f; // Thời gian chờ để lướt lần tiếp theo
    [SerializeField] private float dasingTime = 0.2f;   // Thời gian thực hiện cú lướt
    [SerializeField] private float dashingSpeed = 7f;   // Tốc độ khi đang lướt

    private bool _canDash;  // Có đang sẵn sàng để lướt không?
    private bool _isDasing;     // Có đang trong trạng thái lướt không?

    private bool _dashInput;    // Biến lưu trạng thái người chơi có nhấn nút lướt không

    private Vector3 _velocity;  // Vận tốc tổng hợp (chủ yếu dùng cho trục Y - trọng lực)
    private float _currentSpeed;    // Tốc độ hiện tại của nhân vật (thay đổi từ 0 đến maxspeed)
    private InputSystem_Actions _playerInputActions;
    private Vector3 _input;     // Hướng nhấn nút (X và Z)
    private CharacterController _CharacterController;

    private void Awake()
    {
        // Khởi tạo đầu vào và lấy thành phần CharacterController từ Object
        _playerInputActions = new InputSystem_Actions();
        _CharacterController = GetComponent<CharacterController>();

        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
        }

        _canDash = true;
    }
    private void OnEnable()
    {
        _playerInputActions.Player.Enable();    // Bật khi Object hoạt động
    }
    private void OnDisable()
    {
        _playerInputActions.Player.Disable();   // Tắt khi Object bị ẩn
    }

    private void Update()
    {
        // 1. XỬ LÝ TRỌNG LỰC
        bool isGrouded = _CharacterController.isGrounded;   // Kiểm tra chân có chạm đất không
        if (isGrouded && _velocity.y < 0)
        {
            _velocity.y = -2f; // Nếu chạm đất thì giữ một lực đè nhẹ để nhân vật không bị nảy
        }

        if (!isGrouded)
        {
            // Nếu đang ở trên không, cộng dồn trọng lực vào vận tốc trục Y theo thời gian
            _velocity.y += gravity * Time.deltaTime;
        }

        // 2. CÁC HÀM XỬ LÝ LOGIC DI CHUYỂN
        GatherInput();          // Đọc nút bấm

        Look();                 // Xoay mặt theo hướng đi
        CalculareSpeed();       // Tính toán tốc độ (tăng/giảm tốc)

        UpdateAnimation();

        Move();                 // Thực hiện di chuyển thực tế

        // 3. KÍCH HOẠT LƯỚT (DASH)
        // Nếu nhấn nút + đủ điều kiện + đang có hướng di chuyển thì bắt đầu lướt
        if (_dashInput && _canDash && _input != Vector3.zero)
        {
            StartCoroutine(Dash());
        }
    }

    private void UpdateAnimation()
    {
        if (anim == null) return;

        // Nếu tốc độ hiện tại lớn hơn một chút (để tránh nhiễu), thì bật isRunning
        bool isMoving = _currentSpeed > 0.1f && _input != Vector3.zero;
        anim.SetBool("isRunning", isMoving);
    }

    // Coroutine xử lý thời gian lướt và hồi chiêu
    private IEnumerator Dash()
    {
        _canDash = false;               // Khóa lướt (để không lướt liên tục được)
        _isDasing = true;               // Bật trạng thái đang lướt
        yield return new WaitForSeconds(dasingTime);        // Đợi trong thời gian lướt (0.2s)
        _isDasing = false;              // Tắt trạng thái đang lướt
        yield return new WaitForSeconds(dashingCooldown);   // Đợi thời gian hồi chiêu (1.5s)
        _canDash = true;                // Cho phép lướt lại
    }

    // Tính toán tốc độ mượt mà (độ quán tính)
    private void CalculareSpeed()
    {
        if (_input == Vector3.zero && _currentSpeed > 0)
        {
            // Nếu không nhấn nút, trừ dần tốc độ (phanh lại)
            _currentSpeed -= decelerationFactor * Time.deltaTime;
        }
        else if (_input != Vector3.zero && _currentSpeed < maxspeed)
        {
            // Nếu có nhấn nút, cộng dần tốc độ (đạp ga)
            _currentSpeed += accelerationFactor * Time.deltaTime;
        }

        // Đảm bảo tốc độ luôn nằm trong khoảng từ 0 đến maxspeed
        _currentSpeed = Mathf.Clamp( _currentSpeed,0, maxspeed );
    }

    private void Look()
    {
        if( _input == Vector3.zero) return; // Không nhấn nút thì không xoay

        // Tạo một ma trận xoay 45 độ để hướng đi khớp với góc camera chéo
        Matrix4x4 isometricMatric = Matrix4x4.Rotate(Quaternion.Euler(0, 45, 0));
        // Nhân hướng nhấn nút với ma trận 45 độ này
        Vector3 multipliedMatrix = isometricMatric.MultiplyPoint3x4( _input );

        // Tạo góc xoay dựa trên hướng đã biến đổi
        Quaternion rotation = Quaternion.LookRotation( multipliedMatrix, Vector3.up);
        // Xoay nhân vật dần dần từ góc hiện tại sang góc mới (tạo cảm giác mượt)
        transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, rotationSpeed * Time.deltaTime);
    }

    private void Move()
    {
        if ( _isDasing)
        {
            // Nếu đang lướt: di chuyển theo hướng mặt + cộng trọng lực
            _CharacterController.Move(transform.forward * dashingSpeed * Time.deltaTime + _velocity * Time.deltaTime);
            return;
        }
        // Nếu đi bộ: Lấy hướng mặt nhìn * tốc độ hiện tại
        Vector3 moveDirection = transform.forward * _currentSpeed * _input.magnitude;

        // Tổng hợp: (Hướng đi ngang + Trọng lực dọc) * thời gian
        _CharacterController.Move((moveDirection + _velocity) * Time.deltaTime);
    }

    private void GatherInput()
    {
        // Đọc giá trị Vector2 (X, Y) từ Input System
        Vector2 input = _playerInputActions.Player.Move.ReadValue<Vector2>();
        // Chuyển sang Vector3 (X, 0, Z) vì trong 3D nhân vật đi trên mặt phẳng XZ
        _input = new Vector3(input.x, 0, input.y);
        // Kiểm tra nút Sprint (Shift/nút lướt) có đang được nhấn không
        _dashInput = _playerInputActions.Player.Sprint.IsPressed();
    }
}
