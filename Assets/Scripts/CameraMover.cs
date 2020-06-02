using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMover : MonoBehaviour
{
    Vector3 lastPosition;

    public float mouseDragFactor = 0.005f;
    public float cameraSizeScrollFactor = 5f;

    Vector3 initialPosition;
    float initialSize;

    public void ResetCamera()
    {
        transform.position = initialPosition;

        Camera c = GetComponent<Camera>();
        Debug.Assert(c != null);

        c.orthographicSize = initialSize;
    }

    // Start is called before the first frame update
    void Start()
    {
        initialPosition = transform.position;

        Camera c = GetComponent<Camera>();
        Debug.Assert(c != null);

        initialSize = c.orthographicSize;
    }

    // Update is called once per frame
    void Update()
    {
        Camera camera = GetComponent<Camera>();
        Debug.Assert(camera != null);

        if(Input.GetMouseButtonDown(0))
        {
            Debug.Log("Button 0 Down.");

            lastPosition = Input.mousePosition;
        }
        if (Input.GetMouseButton(0))
        {
            //Debug.Log("Button 0.");
            Vector3 currentPosition = Input.mousePosition;
            Vector3 delta = currentPosition - lastPosition;
            lastPosition = currentPosition;

            transform.position -= delta * mouseDragFactor * camera.orthographicSize;
        }
        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log("Button 0 Up.");


        }

        float s = Input.GetAxis("Mouse ScrollWheel");
        if (s != 0)
        {
            //Debug.Log("Scroll Value of " + s);

            camera.orthographicSize += s * cameraSizeScrollFactor;
            if(camera.orthographicSize < 1)
            {
                camera.orthographicSize = 1;
            }
        }
    }
}
