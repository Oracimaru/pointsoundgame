using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

namespace PointSoundGame
{
    public class AudioSourcePlay : MonoBehaviour
    {
        [FMODUnity.EventRef]
        public string eventPath;

        private FMOD.Studio.EventInstance eventInstance;

        // public bool clickToPlay = true;

        private bool audioPlaying = false;
        private float audioAmplitude = 1.0f;
        public float audioPitchInOctave = 0.0f;

        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        // void Update()
        // {

        // }

        public void StartPlayAudio()
        {
            eventInstance = FMODUnity.RuntimeManager.CreateInstance(eventPath);
            AmplifyPlayingAudio(audioAmplitude);
            PitchPlayingAudio(audioPitchInOctave);
            eventInstance.start();
            // Debug.Log("Play Start ~" + eventInstance + audioPitchInOctave);
        }

        public void StopPlayAudio()
        {
            eventInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
            eventInstance.release();
            // Debug.Log("Play Stop ~" + eventInstance);
        }

        public float GetAmplitude()
        {
            return audioAmplitude;
        }

        public void AmplifyPlayingAudio(float amplitude)
        {
            audioAmplitude = amplitude;
            if (eventInstance.isValid())
            {
                // Debug.Log("AmplifyPlayingAudio: " + amplitude);
                eventInstance.setVolume(audioAmplitude);
            }
        }

        public void PitchPlayingAudio(float pitchInOctave)
        {
            // Debug.Log("PitchPlayingAudio: " + pitchInOctave);
            audioPitchInOctave = pitchInOctave;
            if (eventInstance.isValid())
            {
                eventInstance.setPitch(Mathf.Pow(2.0f, audioPitchInOctave));
            }
        }
    }
}
