using UnityEngine;
using UnityEngine.SceneManagement;

public class ShopMenu : MonoBehaviour
{
    public void Return()
    {
        SceneManager.LoadSceneAsync(0);
    }

    public void BuyTest()
    {
        // Buy Test
    }
}