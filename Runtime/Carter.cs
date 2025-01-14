using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;

namespace Carter {
    [System.Serializable]
    public class ApiResponse {
        public ForcedBehaviour[] forced_behaviours;
        public string input;
        public Output output;
    }

    [System.Serializable]
    public class ForcedBehaviour {
        public string name;
    }

    [System.Serializable]
    public class Output {
        public string audio;
        public string text;
    }

    [System.Serializable]
    public class SpeakOutput {
        public string file_url;
    }

    [System.Serializable]
    public class Agent : MonoBehaviour
    {
        public string url;
        public string key;
        public string user_id;

        public delegate void MessageEventHandler(ApiResponse response, bool audio);
        public event MessageEventHandler onMessage;
        
        Listener listener;
        AudioSource audioSource2;

        [System.Serializable]
        public class Message {
            public string key;
            public string user_id;
            public string text;
            public bool speak;
        }

        [System.Serializable]
        public class AudioMessage {
            public string key;
            public string user_id;
            public string audio;
            public bool speak;
        }

        public void StartListening()
        {
            listener = gameObject.AddComponent<Listener>();
        }

        private void HandleApiResponse(ApiResponse apiResponse, bool audio) {
            onMessage?.Invoke(apiResponse, audio);
        }

        public void Interact(string message, bool speak = true) {
            Debug.Log("Sending message: " + message);
            StartCoroutine(SendJsonRequest(message, false, speak));
        }

        IEnumerator SendJsonRequest(string message, bool audio = false, bool speak = true) {
            string json = CreateMessageJson(message, audio, speak);
            
            using (UnityWebRequest www = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST)) {
                www.uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json));
                www.downloadHandler = new DownloadHandlerBuffer();
                www.SetRequestHeader("Content-Type", "application/json");

                yield return www.SendWebRequest();

                if (www.result != UnityWebRequest.Result.Success) {
                    Debug.Log(www.error);
                } else {
                    try {
                        ApiResponse apiResponse = JsonUtility.FromJson<ApiResponse>(www.downloadHandler.text);
                        HandleApiResponse(apiResponse, audio);
                    } catch (Exception e) {
                        Debug.Log("Error parsing response: " + e);
                        Debug.Log("Response: " + www.downloadHandler.text);
                    }
                }
            }
        }

        private string CreateMessageJson(string message, bool audio, bool speak) {
            if (audio) {
                AudioMessage msg = new AudioMessage {
                    key = this.key,
                    user_id = this.user_id,
                    audio = message,
                    speak = speak
                };
                return JsonUtility.ToJson(msg);
            } else {
                Message msg = new Message {
                    key = this.key,
                    user_id = this.user_id,
                    text = message,
                    speak = speak
                };
                return JsonUtility.ToJson(msg);
            }
        }


        //audio stuff
        public void listen(){
            listener.StartListening();
        }

        public string stopListening(){
            return listener.StopListening();
        }


        public void say(string toSay, string gender = "female"){
            StartCoroutine(PlayAudio(toSay, gender));
        }

        public void sendAudio(bool speak = true){

            string base64 = stopListening();

            if(base64 != null){
                Debug.Log("Sending audio");
                Debug.Log("Sending message: " + base64);
                StartCoroutine(SendJsonRequest(base64, true, speak));
            }
        }

        public IEnumerator PlayAudio(string toSay, string gender = "female")
        {
            using (UnityWebRequest www2 = UnityWebRequestMultimedia.GetAudioClip(toSay, AudioType.MPEG))
            {
                if(audioSource2 == null)
                {
                    audioSource2 = gameObject.AddComponent<AudioSource>();
                }

                yield return www2.SendWebRequest();

                if (www2.result == UnityWebRequest.Result.ConnectionError)
                {
                    Debug.Log(www2.error);
                }
                else
                {
                    audioSource2.Stop();
                    AudioClip myClip = DownloadHandlerAudioClip.GetContent(www2);
                    audioSource2.clip = myClip;
                    audioSource2.Play();
                    www2.Dispose();
                }
            }     
        }
    }
      
    public class Listener : MonoBehaviour
    {
        int frameRate = 16000;
        bool isRecording = false;
        AudioSource audioSource;
        List<float> tempRecording = new List<float>();
        bool microphoneAccess;

        public void Initialize(){

            // if class Microhpone exists, set microphoneAccess to True
            if (typeof(Microphone).IsClass)
            {
                microphoneAccess = true;
            }
            else
            {
                microphoneAccess = false;
            }
            

        }

        void Start(){
            Initialize();
            if(!microphoneAccess)
            {
                Debug.Log("Microphone access not granted");
                return;
            }
            Debug.Log("Listening...");
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = Microphone.Start(null, true, 1, 16000);
            audioSource.Play();
            Invoke("ResizeRecording", 1);
            
        }

        public void StartListening()
        {
            if(!microphoneAccess)
            {
                Debug.Log("Microphone access not granted");
                return;
            }
            if(!isRecording)
            {                     
                isRecording = true;
                audioSource.Stop();
                tempRecording.Clear();
                Microphone.End(null);
                audioSource.clip = Microphone.Start(null, true, 20, frameRate);
                Invoke("ResizeRecording", 1);
            } 
        }

        public string StopListening(){
            if(!microphoneAccess)
            {
                Debug.Log("Microphone access not granted");
                return null;
            }

            if (isRecording)
            {
                isRecording = false;
                Microphone.End(null);
                byte[] bytes = WavUtility.FromAudioClip(audioSource.clip);
                string base64 = Convert.ToBase64String(bytes);
                return base64;
            }

            return null;
        }

        void ResizeRecording()
        {
            if(!microphoneAccess)
            {
                Debug.Log("Microphone access not granted");
                return;
            }
            if (isRecording)
            {
                int length = frameRate;
                float[] clipData = new float[length];
                audioSource.clip.GetData(clipData, 0);
                tempRecording.AddRange(clipData);
                Invoke("ResizeRecording", 1);
            }
        }

    

    }
}
