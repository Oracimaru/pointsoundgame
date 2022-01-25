using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PointSoundGame.StepMode
{
    [Serializable]
    public class StepPointData
    {
        public List<StepPointDataItem> pointList = new List<StepPointDataItem>();

        public bool IsFull()
        {
            return false;
        }

        public void Append(StepPointDataItem item)
        {
            pointList.Add(item);
        }

        public void Clear()
        {
            pointList.Clear();
        }
    }
}
