using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class SceneResetter : MonoBehaviour
{
    void Update()
    {
        // Проверяем, подключена ли клавиатура и нажата ли клавиша 
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            ResetScene();
        }
    }

    public void ResetScene()
    {
        // Перезагружаем текущую активную сцену
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
