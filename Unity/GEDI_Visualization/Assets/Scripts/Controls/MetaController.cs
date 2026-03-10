using UnityEngine;
using GEDIGlobals;

public class MetaController : MonoBehaviour
{
    public Transform vrCamera;
    public Transform eye;
    private Vector3 lastCameraPosition = Vector3.up * 1000f; 
    private float threshold = 100f; // update if movement is larger than 100 meters
    public float maxRenderDistance = 3000f; // only objects within 50000 meters are visible
    public float moveSpeed = 1f;  // Speed for movement on the left joystick
    public float riseSpeed = 0.6f;  // Speed for rise/descent on the right joystick
    public float rotateSpeed = 0.6f; // Rotation speed for turning on the right joystick
    // Handle W, A, S, D movement
    private float speedFactor = 1f;
    void Update()
    {
        speedFactor = 1;
        if (OVRInput.Get(OVRInput.RawButton.LIndexTrigger) || OVRInput.Get(OVRInput.RawButton.RIndexTrigger)) speedFactor = 5.0f;
        
        HandleLeftJoystickMovement();
        HandleRightJoystickMovement();
        
        if ((vrCamera.position - lastCameraPosition).sqrMagnitude > threshold * Params.SCALE)
        {
            lastCameraPosition = vrCamera.position;
            UpdateVisibleObjects();
        }
    }
    
    private void UpdateVisibleObjects()
    {
        GameObject[] objects = GameObject.FindGameObjectsWithTag("footprint");

        Vector2 cameraPos = new Vector2(vrCamera.position.x, vrCamera.position.z);
        foreach (GameObject obj in objects)
        {
            Vector2 objPos = new Vector2(obj.transform.position.x, obj.transform.position.z);

            float dist = Vector2.Distance(cameraPos, objPos);
            bool inView = dist < maxRenderDistance * Params.SCALE;

            obj.GetComponent<Renderer>().enabled = inView;
        }
    }


    // Handle the movement from the left joystick (forward, backward, left, right)
    void HandleLeftJoystickMovement()
    {
        Vector3 forward = eye.forward;
        Vector3 right = eye.right;

        forward.y = 0f;
        right.y = 0f;
        forward.Normalize(); // re-normalize after zeroing y
        right.Normalize();  // re-normalize after zeroing y

        if (OVRInput.Get(OVRInput.RawButton.LThumbstickUp)) vrCamera.position = vrCamera.position + forward * moveSpeed * speedFactor;
        if (OVRInput.Get(OVRInput.RawButton.LThumbstickDown)) vrCamera.position = vrCamera.position - forward * moveSpeed * speedFactor;
        if (OVRInput.Get(OVRInput.RawButton.LThumbstickLeft)) vrCamera.position = vrCamera.position - right * moveSpeed * speedFactor;
        if (OVRInput.Get(OVRInput.RawButton.LThumbstickRight)) vrCamera.position = vrCamera.position + right * moveSpeed * speedFactor;
    }

    // Handle the rise, descent, and rotation from the right joystick
    void HandleRightJoystickMovement()
    {

        if (OVRInput.Get(OVRInput.RawButton.RThumbstickLeft))  vrCamera.Rotate(0, -speedFactor * rotateSpeed, 0);
        if (OVRInput.Get(OVRInput.RawButton.RThumbstickRight)) vrCamera.Rotate(0, speedFactor * rotateSpeed, 0);
        if (OVRInput.Get(OVRInput.Button.Two))   vrCamera.position = vrCamera.position + vrCamera.up * riseSpeed * speedFactor;
        if (OVRInput.Get(OVRInput.Button.One)) vrCamera.position = vrCamera.position - vrCamera.up * riseSpeed * speedFactor;
    }

}
