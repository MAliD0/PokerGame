using UnityEngine;
using UnityEngine.Rendering;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public Canvas mainCanvas;
    [SerializeField] SerializedDictionary<string, GameUIElement> gameMenus;
    [SerializeField] string currentActiveMenu;

    private void Awake()
    {
        Instance = this;
    }
    public void LinkMenu(GameUIElement uIElement)
    {
        gameMenus.Add(uIElement.menuName, uIElement);
        if(uIElement.canvas == null)
        {
            uIElement.canvas = mainCanvas;
        }
    }
    public void ToggleMenu(string menuName)
    {
        if (gameMenus.ContainsKey(menuName))
        {
            gameMenus[menuName].isActive = !gameMenus[menuName].isActive;
            
        }
        else
        {
            Debug.LogWarning("Missing menu: " + menuName);
        }
    }
    public void DisableMenu(string menuName)
    {
        if (gameMenus.ContainsKey(menuName))
        {
            gameMenus[menuName].isActive = false;
        }
        else
        {
            Debug.LogWarning("Missing menu: " + menuName);
        }
    }
    public void EnableMenu(string menuName)
    {
        if (gameMenus.ContainsKey(menuName))
        {
            gameMenus[menuName].isActive = true;
        }
        else
        {
            Debug.LogWarning("Missing menu: " + menuName);
        }
    }
}
