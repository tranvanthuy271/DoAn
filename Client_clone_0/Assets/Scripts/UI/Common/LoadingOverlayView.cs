using TMPro;
using UnityEngine;

[DisallowMultipleComponent]
public class LoadingOverlayView : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject spinnerRoot;
    [SerializeField] private bool hideStatusWhenEmpty = true;

    public TMP_Text StatusText => statusText;

    private void Awake()
    {
        ResolveReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
    }
#endif

    public void ResolveReferences()
    {
        if (statusText == null)
        {
            statusText = FindNamedComponent<TMP_Text>(transform, "StatusText");
            if (statusText == null)
            {
                statusText = GetComponentInChildren<TMP_Text>(true);
            }
        }

        if (spinnerRoot == null)
        {
            Transform spinnerTransform = FindByName(transform, "SpinnerRoot")
                                         ?? FindByName(transform, "SpinnerImage")
                                         ?? FindByName(transform, "LoadingSpinner");
            if (spinnerTransform != null)
            {
                spinnerRoot = spinnerTransform.gameObject;
            }
        }
    }

    public void SetStatus(string message)
    {
        ResolveReferences();
        if (statusText == null)
        {
            return;
        }

        bool hasMessage = !string.IsNullOrWhiteSpace(message);
        statusText.text = hasMessage ? message : string.Empty;

        if (hideStatusWhenEmpty)
        {
            statusText.gameObject.SetActive(hasMessage);
        }
    }

    public void SetSpinnerVisible(bool visible)
    {
        ResolveReferences();
        if (spinnerRoot != null)
        {
            spinnerRoot.SetActive(visible);
        }
    }

    private static T FindNamedComponent<T>(Transform root, string name) where T : Component
    {
        Transform target = FindByName(root, name);
        return target != null ? target.GetComponent<T>() : null;
    }

    private static Transform FindByName(Transform root, string name)
    {
        if (root == null)
        {
            return null;
        }

        if (root.name == name)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform found = FindByName(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
