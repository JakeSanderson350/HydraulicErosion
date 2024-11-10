using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float movementSpeed = 10f;
    public float rotationSpeed = 5f;

    private Vector3 movementDirection;
    private float horizontalRotation;
    private float verticalRotation;
    private bool isRotating;

    void Update()
    {
        HandleMovementInput();
        HandleRotationInput();

        MoveCamera();
        RotateCamera();
    }

    void HandleMovementInput()
    {
        movementDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
        {
            movementDirection += transform.forward;
        }
        if (Input.GetKey(KeyCode.S))
        {
            movementDirection -= transform.forward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            movementDirection -= transform.right;
        }
        if (Input.GetKey(KeyCode.D))
        {
            movementDirection += transform.right;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            movementDirection -= transform.up;
        }
        if (Input.GetKey(KeyCode.E))
        {
            movementDirection += transform.up;
        }
        if (Input.GetKey(KeyCode.LeftShift))
        {
            movementSpeed = 50f;
        }
        else
        {
            movementSpeed = 10f;
        }
    }

    void HandleRotationInput()
    {
        if (Input.GetMouseButtonDown(1))
        {
            isRotating = true;
        }
        if (Input.GetMouseButtonUp(1))
        {
            isRotating = false;
        }

        if (isRotating)
        {
            horizontalRotation = Input.GetAxis("Mouse X") * rotationSpeed;
            verticalRotation = -Input.GetAxis("Mouse Y") * rotationSpeed;
        }
    }

    void MoveCamera()
    {
        transform.position += movementDirection * movementSpeed * Time.deltaTime;
    }

    void RotateCamera()
    {
        if (isRotating)
        {
            transform.Rotate(Vector3.up, horizontalRotation, Space.World);
            transform.Rotate(Vector3.right, verticalRotation, Space.Self);
        }
    }
}
