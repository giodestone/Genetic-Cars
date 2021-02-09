using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour
{
    [SerializeField] private float Speed = 60f;

    private Vector3 startPos;
    private float startSize;

    void Start()
    {
        startPos = transform.position;
        startSize = GetComponent<Camera>().orthographicSize;
    }

    /// <summary>
    /// Update camera position based on input.
    /// </summary>
    void Update()
    {
        var newPos = new Vector3(transform.position.x + Input.GetAxis("Horizontal") * Speed * Time.deltaTime, transform.position.y + Input.GetAxis("Vertical") * Speed * Time.deltaTime, transform.position.z);
        transform.position = newPos;

        GetComponent<Camera>().orthographicSize += Input.GetAxis("Zoom") * Speed * Time.deltaTime;
        GetComponent<Camera>().orthographicSize = Mathf.Clamp(GetComponent<Camera>().orthographicSize, 0.1f, 1000f);

        if (Input.GetAxis("Reset") > 0.93f)
        {
            GetComponent<Camera>().orthographicSize = startSize;
            transform.position = startPos;
        }
    }
}
