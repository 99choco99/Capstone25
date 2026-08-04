using UnityEngine;

public abstract class UIBase : MonoBehaviour
{
    [Header("UI Settings")]
    public UIPanelType panelType;
    public bool IsOpen {  get; private set; }

    public virtual void Init() { }
    public virtual void SetUp(Player localPlayer) { }
    public virtual void Open() { gameObject.SetActive(true); IsOpen = true; }
    public virtual void Close() { gameObject.SetActive(false); IsOpen = false; }
}
