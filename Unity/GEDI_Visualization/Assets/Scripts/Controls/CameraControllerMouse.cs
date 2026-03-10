using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Required for UI components
using GEDIGlobals;

public class CameraControllerMouse : MonoBehaviour
{
    public float moveSpeed = 8f;        // Speed for moving the camera
    public float rotationSpeed = 15.0f;   // Speed for rotating the camera
    public float verticalSpeed = 1f;     // Speed for rising and descending (Y-axis)
    public Slider speedSlider; // Assign in the inspector

    public WaveformVisualizer visualizer;

    private float speedScale = 1f;
    private Vector3 cameraOffset;        // Camera offset from the target point
    private Vector3 lastMousePosition;   // Last mouse position for detecting movement
    private bool isRotating = false;     // To check if the right mouse button is held down
    private Vector3 lastCameraPosition = Vector3.up * 1000f;
    private float threshold = 100f; // update if movement is larger than 100 meters
    public float maxRenderDistance = 5000f; // only objects within 50000 meters are visible
    void Start()
    {
        // Initialize camera offset
        cameraOffset = transform.position;
        speedSlider.onValueChanged.AddListener(SetScale);
    }

    private void SetScale(float scale)
    {
        speedScale = (0.1f + 0.9f * scale) * 10f;
    }

    public void Update()
    {
        HandleMovement();
        HandleRotation();
        
        if ((transform.position - lastCameraPosition).sqrMagnitude > threshold * threshold * Params.SCALE * Params.SCALE)
        {
            lastCameraPosition = transform.position;
            UpdateVisibleObjects();
        }
    }

    private void UpdateVisibleObjects()
    {
        GameObject[] footprints = GameObject.FindGameObjectsWithTag("footprint");
        GameObject[] subclusters = GameObject.FindGameObjectsWithTag("subcluster");
        GameObject[] clusters = GameObject.FindGameObjectsWithTag("cluster");

        int viz_scale = this.visualizer.GetVizScale();

        Vector2 cameraPos = new Vector2(transform.position.x, transform.position.z);
        float maxRenderDistanceSq = Params.SCALE * maxRenderDistance * maxRenderDistance;

        if (viz_scale==0)
        {
            foreach (GameObject obj in footprints)
            {
                Vector3 p = obj.transform.position;
                float dx = cameraPos.x - p.x;
                float dz = cameraPos.y - p.z;
                bool inView = (dx * dx + dz * dz) < maxRenderDistanceSq;
                if (obj.GetComponent<Renderer>().enabled != inView)
                    obj.GetComponent<Renderer>().enabled = inView;
            }    
        }
        
        if (viz_scale==1)
        {
            foreach (GameObject obj in clusters)
            {
                Vector3 p = obj.transform.position;
                float dx = cameraPos.x - p.x;
                float dz = cameraPos.y - p.z;
                bool inView = (dx * dx + dz * dz) < maxRenderDistanceSq*100;
                if (obj.GetComponent<Renderer>().enabled != inView)
                    obj.GetComponent<Renderer>().enabled = inView;
            }
        }

        if (viz_scale==2)
        {
            foreach (GameObject obj in subclusters)
            {
                Vector3 p = obj.transform.position;
                float dx = cameraPos.x - p.x;
                float dz = cameraPos.y - p.z;
                bool inView = (dx * dx + dz * dz) < maxRenderDistanceSq*10;
                if (obj.GetComponent<Renderer>().enabled != inView)
                    obj.GetComponent<Renderer>().enabled = inView;
            }
        }
        

    }


    // Handle W, A, S, D movement
    void HandleMovement()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal"); // A, D keys for left-right movement
        float verticalInput = Input.GetAxisRaw("Vertical");     // W, S keys for forward-backward movement
        
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize(); // re-normalize after zeroing y
        right.Normalize();  // re-normalize after zeroing y

        // calc movement direction on horiz plane
        Vector3 desiredPlanarMove = (forward * verticalInput + right * horizontalInput).normalized;

        // calc displacement for planar movement
        Vector3 planarDisplacement = desiredPlanarMove * moveSpeed * speedScale * Time.deltaTime;


        // Vertical control: Space for rise, Ctrl for descend
        float riseInput = 0f;
        if (Input.GetKey(KeyCode.Space))  // Space key to move up
        {
            riseInput = 5f;
        }
        if (Input.GetKey(KeyCode.LeftShift))  // Ctrl key to move down
        {
            riseInput = -5f;
        }

        // Create a movement vector using horizontal, vertical, and rise inputs
        // Vector3 movement = new Vector3(horizontalInput, riseInput * verticalSpeed, verticalInput) * moveSpeed * speedScale * Time.deltaTime;
        Vector3 verticalDisplacement = Vector3.up * riseInput * verticalSpeed * speedScale * Time.deltaTime;
        transform.position += planarDisplacement + verticalDisplacement;

        // Translate the camera using this movement vector
        // transform.Translate(movement, Space.Self);
    }

    // Handle mouse rotation (Right Mouse Button to rotate)
    void HandleRotation()
    {
        if (Input.GetMouseButtonDown(1))  // Right mouse button pressed
        {
            isRotating = true;
            lastMousePosition = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(1))    // Right mouse button released
        {
            isRotating = false;
        }

        if (isRotating)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;

            // Rotate the camera around the Y-axis (horizontal rotation) and X-axis (vertical rotation)
            float yaw = mouseDelta.x * speedScale * 0.1f * rotationSpeed * Time.deltaTime;
            float pitch = -mouseDelta.y * speedScale * 0.1f * rotationSpeed * Time.deltaTime;

            transform.RotateAround(transform.position, Vector3.up, yaw);   // Horizontal rotation
            transform.RotateAround(transform.position, transform.right, pitch); // Vertical rotation

            lastMousePosition = Input.mousePosition;
        }

        else 
        {
            float horizontalRotation = 0f;
            if (Input.GetKey(KeyCode.L))  // spin left
            {
                horizontalRotation = 2f;
            }
            if (Input.GetKey(KeyCode.J))  // spin right
            {
                horizontalRotation = -2f;
            }
            float verticalRotation = 0f;
            if (Input.GetKey(KeyCode.K))  // roll up
            {
                verticalRotation = 2f;
            }
            if (Input.GetKey(KeyCode.I))  // roll down
            {
                verticalRotation = -2f;
            }

            // Rotate the camera around the Y-axis (horizontal rotation) and X-axis (vertical rotation)
            float yaw = horizontalRotation * rotationSpeed * 0.5f *speedScale * Time.deltaTime;
            float pitch = verticalRotation * rotationSpeed * 0.5f *speedScale * Time.deltaTime;

            transform.RotateAround(transform.position, Vector3.up, yaw);   // Horizontal rotation
            transform.RotateAround(transform.position, transform.right, pitch);   // Vertical rotation
        }
    }

}
