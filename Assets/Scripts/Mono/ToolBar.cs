using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PointSoundGame
{
    public class ToolBar : MonoBehaviour
    {
        // private GameObject stepModeOwner = null;
        // private GameObject freeModeOwner = null;

        private StepMode.Playground stepPlayground = null;
        private FreeMode.Playground freePlayground = null;

        private PlayPauseButton playPauseButton = null;

        private Button stepModeButton = null;
        private Button freeModeButton = null;

        private bool isPlaying = false;

        // Start is called before the first frame update
        void Start()
        {
            GameObject canvas = GameObject.Find("Canvas");
            // stepModeOwner = canvas.transform.Find("StepMode").gameObject;
            // freeModeOwner = canvas.transform.Find("FreeMode").gameObject;

            stepPlayground = canvas.transform.Find("StepPlayground")?.GetComponent<StepMode.Playground>();
            freePlayground = canvas.transform.Find("FreePlayground")?.GetComponent<FreeMode.Playground>();

            playPauseButton = GameObject.Find("PlayPauseButton").GetComponent<PlayPauseButton>();

            stepModeButton = GameObject.Find("StepMode-Selector").GetComponent<Button>();
            freeModeButton = GameObject.Find("FreeMode-Selector").GetComponent<Button>();

            switch (PointDataManager.Singleton.playMode)
            {
                case PointDataManager.PlayMode.Step:
                    stepPlayground.gameObject.SetActive(true);
                    freePlayground.gameObject.SetActive(false);
                    stepModeButton.Select();
                    break;
                case PointDataManager.PlayMode.Free:
                    stepPlayground.gameObject.SetActive(false);
                    freePlayground.gameObject.SetActive(true);
                    freeModeButton.Select();
                    break;
            }
        }

        // Update is called once per frame
        void Update()
        {
            bool playingNow = false;
            switch (PointDataManager.Singleton.playMode)
            {
                case PointDataManager.PlayMode.Step:
                    {
                        playingNow = stepPlayground.isPlaying;
                    }
                    break;
                case PointDataManager.PlayMode.Free:
                    {
                        playingNow = freePlayground.isPlaying;
                    }
                    break;
            }

            if (playingNow != isPlaying)
            {
                isPlaying = !isPlaying;
                if (isPlaying)
                {
                    playPauseButton.SetState(PlayPauseButton.State.Play);
                }
                else
                {
                    playPauseButton.SetState(PlayPauseButton.State.Pause);
                }
            }
        }

        public void SavePointData()
        {
            PointDataManager.Singleton.SavePointData();
        }

        public void CleanPointData()
        {
            Play(false);

            switch (PointDataManager.Singleton.playMode)
            {
                case PointDataManager.PlayMode.Step:
                    {
                        stepPlayground.CleanPoints();
                        PointDataManager.Singleton.stepPointData.Clear();
                    }
                    break;
                case PointDataManager.PlayMode.Free:
                    {
                        freePlayground.CleanPoints();
                        PointDataManager.Singleton.freePointData.Clear();
                    }
                    break;
            }
        }

        public void Play()
        {
            Play(!isPlaying);
        }

        private void Play(bool play)
        {
            switch (PointDataManager.Singleton.playMode)
            {
                case PointDataManager.PlayMode.Step:
                    {
                        stepPlayground.PlayPoints(play);
                    }
                    break;
                case PointDataManager.PlayMode.Free:
                    {
                        freePlayground.PlayPoints(play);
                    }
                    break;
            }
        }

        public void ChangeStepMode()
        {
            PointDataManager.Singleton.playMode = PointDataManager.PlayMode.Step;
            stepPlayground?.gameObject.SetActive(true);
            freePlayground?.gameObject.SetActive(false);
        }

        public void ChangeFreeMode()
        {
            PointDataManager.Singleton.playMode = PointDataManager.PlayMode.Free;
            stepPlayground?.gameObject.SetActive(false);
            freePlayground?.gameObject.SetActive(true);
        }
    }
}
