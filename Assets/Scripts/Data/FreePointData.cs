using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PointSoundGame.FreeMode
{
    [Serializable]
    public class FreePointData
    {
        public List<FreePointDataItem> pointList = new List<FreePointDataItem>();

        public bool IsFull()
        {
            return false;
        }

        public void Append(FreePointDataItem item)
        {
            pointList.Add(item);
        }

        public void Clear()
        {
            pointList.Clear();
        }
    }
}
