using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RPG.Saving
{
    [ExecuteAlways]
    public class SavingSystem : MonoBehaviour
    {
        public IEnumerator LoadLastScene(string saveFile)
        {
            Dictionary<string,object> state = LoadFile(saveFile);
            if (state.ContainsKey("lastScene"))
            {
                yield return SceneManager.LoadSceneAsync((int)state["lastScene"]);
            }
            RestoreState(state);
        }

        public void Save(string saveFile)
        {
            Dictionary<string, object> state = LoadFile(saveFile); //Load current game state so I can update it
            CaptureState(state); //Update entities in these scene
            SaveFile(saveFile,state); //Save updates in file
        }

        

        public void Load(string saveFile)
        {
            RestoreState(LoadFile(saveFile));
        }

        private void SaveFile(string saveFile, object state)
        {
            string path = GetPathFromSaveFile(saveFile);
            print("Saving to " + path);
            using (FileStream stream = File.Open(path, FileMode.Create))
            {
               BinaryFormatter formatter = new BinaryFormatter();
               formatter.Serialize(stream, state);
            }
        }

        public void Delete(string saveFile)
        {
            string path = GetPathFromSaveFile(saveFile);
            print("Deleting " + path);
            File.Delete(path);
        }

        private Dictionary<string, object> LoadFile(string saveFile)
        {
            string path = GetPathFromSaveFile(saveFile);
            print("Load from " + path);
            if(!File.Exists(path)) return new Dictionary<string, object>();
            using (FileStream stream = File.Open(path, FileMode.Open))
            {
                BinaryFormatter formatter = new BinaryFormatter();
                return (Dictionary<string, object>)formatter.Deserialize(stream);
            }
        }

        private void CaptureState(Dictionary<string, object> state) // Creates or updates entries in dictionary
        {
            foreach (SaveableEntity entity in FindObjectsOfType<SaveableEntity>())
            {
                state[entity.GetUniqueIdentifier()] = entity.CaptureState(); //if key is not present, automatically adds a new key/value pair
            }

            state["lastScene"] = SceneManager.GetActiveScene().buildIndex;
        }

        private void RestoreState(Dictionary<string, object> state)
        {
            foreach (SaveableEntity entity in FindObjectsOfType<SaveableEntity>())
            {
                if (state.ContainsKey(entity.GetUniqueIdentifier())) {
                    entity.RestoreState(state[entity.GetUniqueIdentifier()]);
                }
            }
        }

        private string GetPathFromSaveFile(string saveFile)
        {
            return Path.Combine(Application.persistentDataPath, saveFile + ".sav");
        }

    }
}