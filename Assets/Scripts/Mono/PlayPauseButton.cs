using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PointSoundGame
{
    public class PlayPauseButton : MonoBehaviour
    {
        public enum State
        {
            Pause,
            Play,
        }
        // Start is called before the first frame update
        void Start()
        {
            SetState(State.Pause);
        }

        public void SetState(State state)
        {
            switch (state)
            {
                case State.Play:
                    GetComponent<Image>().sprite = Resources.Load<Sprite>("Meterial/Images/topause-button");
                    break;
                case State.Pause:
                    GetComponent<Image>().sprite = Resources.Load<Sprite>("Meterial/Images/toplay-button");
                    break;
                default:
                    break;
            }
        }
    }
}