using System.Collections;
using System.Collections.Generic;
using System.IO;
using PointSoundGame.StepMode;
using PointSoundGame.FreeMode;
using UnityEngine;

namespace PointSoundGame
{
    public class PointDataManager
    {
        public const string BLUE_COLOR = "#0E20E2";
        public const string YELLOW_COLOR = "#F9E350";
        public const string RED_COLOR = "#DB4B4A";

        public enum PlayMode
        {
            None, 
            Step, // 进行模式
            Free, // 自由模式 
        }
        public PlayMode playMode {
            get { return mPlayMode; }
            set {
                Debug.Log("PointDataManager.ChangeMode " + value);
                mPlayMode = value;
            }
        }
        
        private PlayMode mPlayMode = PlayMode.Step; 

        private static string STORAGE_PATH = Application.persistentDataPath + "/com.igetcool.unity.pointsoundgame";
        private static string STEP_STORAGE_PATH = STORAGE_PATH + "/steppointdata.json";
        private static string FREE_STORAGE_PATH = STORAGE_PATH + "/freepointdata.json";

        public StepPointData stepPointData = new StepPointData();
        public FreePointData freePointData = new FreePointData();
        public void SavePointData()
        {
            switch (playMode)
            {
                case PlayMode.Step:
                    {
                        string json = JsonUtility.ToJson(stepPointData);
                        Directory.CreateDirectory(STORAGE_PATH);
                        File.WriteAllText(STEP_STORAGE_PATH, json);
                        Debug.Log("PointDataManager:SavePointData Step " + STEP_STORAGE_PATH);
                    }
                    break;
                case PlayMode.Free:
                    {
                        string json = JsonUtility.ToJson(freePointData);
                        Directory.CreateDirectory(STORAGE_PATH);
                        File.WriteAllText(FREE_STORAGE_PATH, json);
                        Debug.Log("PointDataManager:SavePointData Free " + FREE_STORAGE_PATH);
                    }
                    break;
            }
        }

        private static PointDataManager s_singleton = null;
        public static PointDataManager Singleton
        {
            get
            {
                if (s_singleton == null)
                {
                    s_singleton = new PointDataManager(PointColor.None);
                }
                return s_singleton;
            }
        }

        public enum PointColor
        {
            None,
            Blue,
            Yellow,
            Red,
        };
        public string PointColorHexString {
            get {
                switch (currentPointSelectorColor)
                {
                    case PointColor.Blue: return BLUE_COLOR;
                    case PointColor.Yellow: return YELLOW_COLOR;
                    case PointColor.Red: return RED_COLOR;
                    default: return "#000000";
                }
            }
        }

        public PointDataManager(PointColor currentPointSelectorColor)
        {
            Debug.Log("STORAGE_PATH: " + STORAGE_PATH);
            
            this.playMode = PlayMode.Free;

            this.currentPointSelectorColor = currentPointSelectorColor;

            if (File.Exists(STEP_STORAGE_PATH))
            {
                string json = File.ReadAllText(STEP_STORAGE_PATH);
                Debug.Log("STEP json: " + json);
                if (json != null)
                {
                    stepPointData = JsonUtility.FromJson<StepPointData>(json);
                }
            }
            if (stepPointData == null)
            {
                stepPointData = new StepPointData();
            }

            if (File.Exists(FREE_STORAGE_PATH))
            {
                string json = File.ReadAllText(FREE_STORAGE_PATH);
                Debug.Log("FREE json: " + json);
                if (json != null)
                {
                    freePointData = JsonUtility.FromJson<FreePointData>(json);
                }
            }
            if (freePointData == null)
            {
                freePointData = new FreePointData();
            }
        }
        public PointColor currentPointSelectorColor { get; set; }

        public GameObject createStepPoint(StepPointDataItem itemData)
        {
            if (itemData == null)
            {
                return null;
            }

            GameObject go = createStepPoint(
                (PointDataManager.PointColor)System.Enum.Parse(typeof(PointDataManager.PointColor), itemData.color),
                itemData);
            return go;
        }

        public GameObject createStepPoint(PointColor color, StepPointDataItem itemData)
        {
            Debug.Log("createPoint: " + color);

            if (color == PointColor.None)
            {
                return null;
            }

            string prefab = "Prefabs/" + color.ToString() + "Point";
            Debug.Log("createPoint:prefab " + prefab);

            GameObject point = GameObject.Instantiate(Resources.Load<GameObject>(prefab));
            if (itemData != null)
            {
                point.GetComponent<StepPoint>().itemData = itemData;
            }
            else
            {
                var item = new StepPointDataItem();
                item.color = color.ToString();
                item.amplitude = StepPoint.AMPLIFY_STANDARD;
                item.numOfBeatTempo = 0;
                item.pitchInOctave = 0;
                point.GetComponent<StepPoint>().itemData = item;
            }

            return point;
        }

        public GameObject createBlueStepPoint(StepPointDataItem itemData)
        {
            return createStepPoint(PointColor.Blue, itemData);
        }
        public GameObject createRedStepPoint(StepPointDataItem itemData)
        {
            return createStepPoint(PointColor.Red, itemData);
        }
        public GameObject createYellowStepPoint(StepPointDataItem itemData)
        {
            return createStepPoint(PointColor.Yellow, itemData);
        }

        public GameObject createCurrentStepPoint(StepPointDataItem itemData)
        {
            return createStepPoint(currentPointSelectorColor, itemData);
        }
    }
}