using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;

namespace PointSoundGame.FreeMode
{
    public class Playground : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        private PointDataManager.PointColor currentColor = PointDataManager.PointColor.None;

        FreeStroke blueFreeStroke = null;
        FreeStroke yellowFreeStroke = null;
        FreeStroke redFreeStroke = null;

        FreeStroke currentFreeStroke = null;
        private bool mIsPlaying = false;

        private float drawingStartTime = 0;

        internal bool isPlaying
        {
            get
            {
                if (currentFreeStroke != null) return currentFreeStroke.isPlaying;
                return mIsPlaying;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            blueFreeStroke = transform.Find("BlueStroke").gameObject.GetComponent<FreeStroke>();
            yellowFreeStroke = transform.Find("YellowStroke").gameObject.GetComponent<FreeStroke>();
            redFreeStroke = transform.Find("RedStroke").gameObject.GetComponent<FreeStroke>();
        }

        // Update is called once per frame
        void Update()
        {
            if (currentColor != PointDataManager.Singleton.currentPointSelectorColor)
            {
                currentColor = PointDataManager.Singleton.currentPointSelectorColor;
                SelectStroke(currentColor);
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log("FreeMode.Playground:OnPointerDown: " + eventData.position + eventData.pointerCurrentRaycast.gameObject.name);

            if (currentFreeStroke != null)
            {
                currentFreeStroke.CleanPoints();

                currentFreeStroke.isDrawing = true;
                drawingStartTime = Time.time;
                currentFreeStroke.DrawStroke(Background.ConvertScreenPosition(eventData.position), drawingStartTime);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Debug.Log("FreeMode.Playground:OnPointerUp: " + eventData.position + eventData?.pointerCurrentRaycast.gameObject?.name);

            if (currentFreeStroke != null)
            {
                currentFreeStroke.DrawStroke(Background.ConvertScreenPosition(eventData.position), Time.time);
                currentFreeStroke.isDrawing = false;
                drawingStartTime = 0;
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            Debug.Log("FreeMode.Playground:OnPointerExit: " + eventData.position);

            if (currentFreeStroke != null)
            {
                currentFreeStroke.isDrawing = false;
                drawingStartTime = 0;
            }
        }

        internal void CleanPoints()
        {
            blueFreeStroke?.CleanPoints();
            yellowFreeStroke?.CleanPoints();
            redFreeStroke?.CleanPoints();
        }

        internal void PlayPoints(bool play)
        {
            if (currentFreeStroke != null)
            {
                currentFreeStroke.PlayPoints(play);
            }
            else
            {
                if (play)
                {
                    mIsPlaying = true;
                    blueFreeStroke?.PlayPoints(true, () =>
                    {
                        yellowFreeStroke?.PlayPoints(true, () =>
                        {
                            redFreeStroke?.PlayPoints(true, () =>
                            {
                                mIsPlaying = false;
                            });
                        });
                    });
                }
                else
                {
                    blueFreeStroke.PlayPoints(false);
                    yellowFreeStroke.PlayPoints(false);
                    redFreeStroke.PlayPoints(false);
                    mIsPlaying = false;
                }
            }
        }

        private void SelectStroke(PointDataManager.PointColor color)
        {
            switch (color)
            {
                case PointDataManager.PointColor.Blue:
                    currentFreeStroke = blueFreeStroke;
                    break;
                case PointDataManager.PointColor.Yellow:
                    currentFreeStroke = yellowFreeStroke;
                    break;
                case PointDataManager.PointColor.Red:
                    currentFreeStroke = redFreeStroke;
                    break;
                default:
                    currentFreeStroke = null;
                    break;
            }
            ActiveStrokes();
        }

        private void ActiveStrokes()
        {
            blueFreeStroke.gameObject.SetActive(blueFreeStroke.hasDrawnData || currentFreeStroke == blueFreeStroke);
            yellowFreeStroke.gameObject.SetActive(yellowFreeStroke.hasDrawnData || currentFreeStroke == yellowFreeStroke);
            redFreeStroke.gameObject.SetActive(redFreeStroke.hasDrawnData || currentFreeStroke == redFreeStroke);
        }

    }
}