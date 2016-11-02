using UnityEngine;
using System.Collections;            //IEnumerator

public class SelectImage : MonoBehaviour 
{
    public float m_scalingFactor = 0.88f;
    
    [SerializeField] private ImageSkybox m_imageSphereSkybox;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

    private string m_imageFilePath; // All ImageSphere's have a path (either through the internet, or local to the device) associated with them!

    public string GetImageFilePath()
    {
        return m_imageFilePath;
    }

    public void SetImageAndPath(Texture texture, string filePath)
    {
        m_imageFilePath = filePath;

        StartCoroutine(AnimateTexture(texture));
    }

    private IEnumerator AnimateTexture(Texture texture)
    {
        Vector3 originalScale = transform.localScale;

        const float kMinShrink = 0.05f; // Minimum value you the sphere can shrink to...
        while (transform.localScale.magnitude > kMinShrink)
        {
            transform.localScale = transform.localScale * m_scalingFactor;
            yield return new WaitForEndOfFrame();
        }

        gameObject.GetComponent<MeshRenderer>().material.mainTexture = texture;

        while (transform.localScale.magnitude < originalScale.magnitude)
        {
            transform.localScale = transform.localScale / m_scalingFactor;
            yield return new WaitForEndOfFrame();
        }

        transform.localScale = originalScale;
    }

    private void OnEnable ()
    {
        m_menuButton.OnButtonSelected += OnButtonSelected;
    }

    private void OnDisable ()
    {
        m_menuButton.OnButtonSelected -= OnButtonSelected;
    }        

    private void OnButtonSelected(VRStandardAssets.Menu.MenuButton button)
    {
        SetSkybox();
    }

    private void SetSkybox()
    {    
        if (m_imageSphereSkybox != null)
        {
            Texture myTexture = gameObject.GetComponent<MeshRenderer>().material.mainTexture;
            m_imageSphereSkybox.SetImageAndPath(myTexture, m_imageFilePath);
        }
    }
}