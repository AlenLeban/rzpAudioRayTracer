using UnityEngine;
using UnityEngine.InputSystem;

public class RuntimeGismo : MonoBehaviour
{
    [SerializeField] private Material selectionMaterial;
    private GameObject selectedObject;
    private Material selectedObjectMaterial;
    private Camera camera;

    private void Awake()
    {
        camera = GetComponent<Camera>();
    }
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hitInfo;
            if (Physics.Raycast(ray, out hitInfo))
            {
                Debug.Log(hitInfo);
                if (hitInfo.collider.gameObject != null)
                {
                    Unselect();
                    selectedObject = hitInfo.collider.gameObject;
                    Select();
                }
            }
        }
    }

    private void Unselect()
    {
        if (selectedObject != null)
        {
            selectedObject.GetComponent<MeshRenderer>().material = selectedObjectMaterial;
        }
    }

    private void Select()
    {
        if (selectedObject != null)
        {
            selectedObjectMaterial = selectedObject.GetComponent<MeshRenderer>().material;
            selectedObject.GetComponent<MeshRenderer>().material = selectionMaterial;
        }
    }
}
