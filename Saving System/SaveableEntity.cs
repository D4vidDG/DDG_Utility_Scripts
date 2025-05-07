using RPG.Core;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace RPG.Saving
{
    [ExecuteAlways]
    public class SaveableEntity : MonoBehaviour
    {
        [SerializeField] string uniqueIdentifier;
        Dictionary<string, SaveableEntity> globalLookUp = new Dictionary<string, SaveableEntity>();

        public string GetUniqueIdentifier()
        {
            return uniqueIdentifier;
        }

        public void SetUniqueIdentifier(string identifier)
        {
            this.uniqueIdentifier = identifier;
        }

        public object CaptureState()
        {
            Dictionary<string, object> state = new Dictionary<string, object>();
            foreach (ISaveable saveable in GetComponents<ISaveable>())
            {
                state[saveable.GetType().ToString()] = saveable.CaptureState();
            }
            return state;
        }

        public void RestoreState(object state)
        {
            Dictionary<string, object> myState = (Dictionary<string, object>)state;
            foreach (ISaveable saveable in GetComponents<ISaveable>())
            {
                string typeString = saveable.GetType().ToString();
                if (myState.ContainsKey(typeString))
                {
                    saveable.RestoreState(myState[typeString]);
                }
            }
        }

#if UNITY_EDITOR //Avoid bugs when building game
        private void Update()
        {
            if (Application.IsPlaying(gameObject)) return; //Don't do it in playmode
            if (string.IsNullOrEmpty(gameObject.scene.path)) return; // Don't do it in the prefab edit view

            SerializedObject obj = new SerializedObject(this); //Only way to warn unity of changes in Edit Mode
            SerializedProperty property = obj.FindProperty("uniqueIdentifier");

            if (string.IsNullOrEmpty(uniqueIdentifier) || !IsUnique(property.stringValue))
            {
                property.stringValue = System.Guid.NewGuid().ToString();
                obj.ApplyModifiedProperties(); // Apply changes to scene so they are saved in the scene file
            }

            globalLookUp[property.stringValue] = this; //Register to dictionary of UUID's
        }

        public bool IsUnique(string candidate)
        {
            if (!globalLookUp.ContainsKey(candidate)) return true;

            if (globalLookUp[candidate] == this) return true;

            if(globalLookUp[candidate] == null) //GameObject was deleted, so let's remove it from the dictionary
            {
                globalLookUp.Remove(candidate);
                return true; //Since there's no longer this UUID in the dict, it's unique
            }

            if (globalLookUp[candidate].GetUniqueIdentifier() != candidate) // Dictionary entry is out of date because I changed the UUID manually
            {
                globalLookUp.Remove(candidate);
                return true;
            }

            return false;
        }
#endif
    }

}