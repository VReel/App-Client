using UnityEngine;
using System.Collections;            //IEnumerator

public class SelectImage : MonoBehaviour 
{
    public float m_defaultScale = 1.0f;
    public float m_scalingFactor = 0.88f;
    
    [SerializeField] private ImageSkybox m_imageSphereSkybox;
    [SerializeField] private VRStandardAssets.Menu.MenuButton m_menuButton;

    private string m_imageFilePath; // All ImageSphere's have a path (either through the internet, or local to the device) associated with them!
    private string kEmptyString = "emptyString";

    public string GetImageFilePath()
    {
        return m_imageFilePath;
    }

    public void SetImageAndFilePath(Texture texture, string filePath)
    {
        m_imageFilePath = filePath;

        StartCoroutine(AnimateSetTexture(texture));
    }

    public void Hide()
    {
        m_imageFilePath = kEmptyString;

        StartCoroutine(AnimateHide());
    }

    private IEnumerator AnimateSetTexture(Texture texture)
    {        
        const float kMinShrink = 0.05f; // Minimum value you the sphere can shrink to...
        while (transform.localScale.magnitude > kMinShrink)
        {
            transform.localScale = transform.localScale * m_scalingFactor;
            yield return new WaitForEndOfFrame();
        }

        gameObject.GetComponent<MeshRenderer>().material.mainTexture = texture;

        while (transform.localScale.magnitude < m_defaultScale)
        {
            transform.localScale = transform.localScale / m_scalingFactor;
            yield return new WaitForEndOfFrame();
        }

        transform.localScale = new Vector3(m_defaultScale, m_defaultScale, m_defaultScale);
    }

    private IEnumerator AnimateHide()
    {        
        const float kMinShrink = 0.005f; // Minimum value you the sphere can shrink to...
        while (transform.localScale.magnitude > kMinShrink)
        {
            transform.localScale = transform.localScale * m_scalingFactor;
            yield return new WaitForEndOfFrame();
        }
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