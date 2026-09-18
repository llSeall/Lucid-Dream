using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class EntityTriggerItem : MonoBehaviour
{
    [Header("Interaction Settings")]
    public KeyCode interactKey = KeyCode.E;
    public float interactDistance = 2.5f;

    [Header("UI & Outline Settings")]
    public GameObject interactionUI;
    public Outline outlineComponent;

    [Header("Toggle Visibility Settings")]
    public TriggerToggleVisibility toggleVisibility;

    [Header("Spawn Settings")]
    [Range(0f, 100f)]
    public float spawnChance = 100f;
    public EntitySpawner entitySpawner;

    [Header("External Integration Events")]
    public UnityEvent onInteractEvent;

    [Header("Deactivate Delay")]
    public float deactivateDelay = 0.5f;

    private bool isInteractable = true;

    void Start()
    {
        if (entitySpawner == null)
        {
            entitySpawner = FindObjectOfType<EntitySpawner>();
        }

        if (outlineComponent == null)
        {
            outlineComponent = GetComponent<Outline>();
        }

        SetHighlight(false);
    }

    public void SetHighlight(bool active)
    {
        if (!isInteractable) return;

        if (interactionUI != null)
        {
            interactionUI.SetActive(active);
        }

        if (outlineComponent != null)
        {
            outlineComponent.enabled = active;
        }
    }

    public void Interact()
    {
        if (!isInteractable) return;
        isInteractable = false;

        Debug.Log($"[Interact Start] ผู้เล่นกดเก็บไอเท็ม: {gameObject.name}");

        SetHighlight(false);
        HideMeshAndCollider();

        // 1. ทดสอบการทำงาน TriggerToggleVisibility
        if (toggleVisibility != null)
        {
            Debug.Log("[ToggleVisibility] สั่งทำงาน ExecuteToggle()");
            toggleVisibility.ExecuteToggle();
        }
        else
        {
            Debug.LogWarning("[ToggleVisibility] ช่อง toggleVisibility ใน Inspector เป็น null (ยังไม่ได้ลากใส่)");
        }

        // 2. ทดสอบการสั่ง Event ภายนอก
        onInteractEvent?.Invoke();

        // 3. ทดสอบการเกิดของผี (EntitySpawner)
        float randomRoll = Random.Range(0f, 100f);
        if (randomRoll <= spawnChance)
        {
            if (entitySpawner != null)
            {
                Debug.Log("[EntitySpawner] กำลังสั่ง SpawnEntity()");
                entitySpawner.SpawnEntity();
            }
            else
            {
                Debug.LogError("[EntitySpawner] ไม่พบ EntitySpawner ใน Scene!");
            }
        }
        else
        {
            Debug.Log($"[EntitySpawner] สุ่มไม่ติดผี (Roll: {randomRoll} / Chance: {spawnChance})");
        }

        StartCoroutine(DisableObjectRoutine());
    }

    private void HideMeshAndCollider()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            r.enabled = false;
        }

        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }
    }

    private IEnumerator DisableObjectRoutine()
    {
        yield return new WaitForSeconds(deactivateDelay);
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }
}