using System.Collections.Generic;
using UnityEngine;

namespace RoomPuzzle
{
    public class TriggerToggleVisibility : MonoBehaviour
    {
        [Header("Target Settings")]
        [SerializeField] private List<GameObject> targetObjects = new List<GameObject>();

        [Header("Trigger Settings")]
        [SerializeField] private string playerTag = "Player";

        public enum ToggleType { Toggle, TurnOn, TurnOff }
        [SerializeField] private ToggleType actionType = ToggleType.Toggle;
        [SerializeField] private bool triggerOnce = false;
        [SerializeField] private bool hideMeshOnly = false;

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(playerTag))
            {
                ExecuteToggle();

                if (triggerOnce)
                {
                    Collider col = GetComponent<Collider>();
                    if (col != null) col.enabled = false;
                }
            }
        }

        private void ExecuteToggle()
        {
            if (targetObjects == null || targetObjects.Count == 0) return;

            foreach (GameObject obj in targetObjects)
            {
                if (obj == null) continue;

                if (hideMeshOnly)
                {
                    Renderer rend = obj.GetComponent<Renderer>();
                    if (rend != null)
                    {
                        switch (actionType)
                        {
                            case ToggleType.Toggle: rend.enabled = !rend.enabled; break;
                            case ToggleType.TurnOn: rend.enabled = true; break;
                            case ToggleType.TurnOff: rend.enabled = false; break;
                        }
                    }
                }
                else
                {
                    switch (actionType)
                    {
                        case ToggleType.Toggle: obj.SetActive(!obj.activeSelf); break;
                        case ToggleType.TurnOn: obj.SetActive(true); break;
                        case ToggleType.TurnOff: obj.SetActive(false); break;
                    }
                }
            }
        }
    }
}