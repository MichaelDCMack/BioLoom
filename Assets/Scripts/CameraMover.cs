using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CameraMover : MonoBehaviour
{
    Vector3 lastPosition;
    bool dragging;

    static bool PointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    public float mouseDragFactor = 0.005f;
    public float cameraSizeScrollFactor = 1f;

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
            // only pan when the press starts on the grid, not on the UI
            dragging = !PointerOverUI();
            lastPosition = Input.mousePosition;
        }
        if (dragging && Input.GetMouseButton(0))
        {
            Vector3 currentPosition = Input.mousePosition;
            Vector3 delta = currentPosition - lastPosition;
            lastPosition = currentPosition;

            transform.position -= delta * mouseDragFactor * camera.orthographicSize;
        }
        if (Input.GetMouseButtonUp(0))
        {
            dragging = false;
        }

        float s = Input.GetAxis("Mouse ScrollWheel");
        if (s != 0 && !PointerOverUI())
        {
            //Debug.Log("Scroll Value of " + s);

            camera.orthographicSize *= 1f + s * cameraSizeScrollFactor;
            if(camera.orthographicSize < 1)
            {
                camera.orthographicSize = 1;
            }
        }
    }
}
