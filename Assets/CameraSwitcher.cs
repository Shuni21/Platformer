using UnityEngine;

public class CameraSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject fpsCamera;
    [SerializeField] private GameObject tpsCamera;

    private bool isTPS = true;

    private void Start()
    {
        SetTPS(true);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            isTPS = !isTPS;
            SetTPS(isTPS);
        }
    }

    private void SetTPS(bool state)
    {
        fpsCamera.SetActive(!state);
        tpsCamera.SetActive(state);
    }
}