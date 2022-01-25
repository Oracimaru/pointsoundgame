using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PointSoundGame
{
    public class PointSelector : MonoBehaviour
    {
        static GameObject s_currentSelectedPointSelector = null;
        public PointDataManager.PointColor colorEnumName;
        // Start is called before the first frame update

        void Start()
        {
            this.markSelect(false);
        }

        private void markSelect(bool select)
        {
            if (select)
            {
                GetComponent<Image>().color = new Color(1f, 1f, 1f);
                GetComponent<RectTransform>().sizeDelta = new Vector2(90, 90);
            }
            else
            {
                GetComponent<Image>().color = new Color(0.9f, 0.9f, 0.9f);
                GetComponent<RectTransform>().sizeDelta = new Vector2(56, 56);
            }
            GetComponent<PointSoundGame.StepMode.StepPoint>().enabled = select;
            GetComponent<AudioSourcePlay>().enabled = select;
        }

        public void Select()
        {
            if (s_currentSelectedPointSelector == this.gameObject)
            {
                s_currentSelectedPointSelector.GetComponent<PointSelector>().markSelect(false);
                s_currentSelectedPointSelector = null;
                PointDataManager.Singleton.currentPointSelectorColor = PointDataManager.PointColor.None;

                Debug.Log("DeSelect: None");
                this.markSelect(false);
            }
            else
            {
                if (s_currentSelectedPointSelector)
                {
                    s_currentSelectedPointSelector.GetComponent<PointSelector>().markSelect(false);
                }
                s_currentSelectedPointSelector = this.gameObject;
                PointDataManager.Singleton.currentPointSelectorColor = colorEnumName;

                Debug.Log("Select: " + this.gameObject + colorEnumName + " " + PointDataManager.Singleton.currentPointSelectorColor);
                this.markSelect(true);
            }
        }
    }
}