using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UnityEngine.UI.Extensions
{
    /// <summary>
    /// UI笔画类，使用插值mesh来生成笔画形状
    /// </summary>
    public class UIStroke : MaskableGraphic
    {
        public enum StrokeType  //笔画类型
        {
            Point,
            Line,
            Curve
        }

        public Color MeshColor;

        private StrokeType _strokeType = StrokeType.Curve;

        private float _width = 20f;     //笔画宽度
        private int _smooth = 1;        //关键点之间要生成的段数

        private const int SemiCircleSegment = 10;   //半圆段数
        private Vector2[] _nodeList = null;     //结点列表
        private Vector2[] _curvePoints = null;  //曲线每个矩形边中点;Count永远是顶点数的一半
        private Quaternion[] _tangentQuaternions = null;    //切线列表
        private Vector3[] _vertices = null; //顶点数组
        private int[] _triangles = null;    //三角形数组

        private float _percent = 1;
        private Vector3[] _verticesCurve = null;    //备份曲线顶点数据
        private Vector3[] _verticesSCS = null;  //备份首端半圆数据
        private Vector3[] _verticesSCE = null;  //备份尾端半圆数据
        private int[] _trianglesCurve = null;   //备份曲线部分三角形数据
        private int[] _trianglesSCS = null; //备份首端半圆三角形数据
        private int[] _trianglesSCE = null; //备份尾端半圆三角形数据

        /// <summary>
        /// 绘制mesh
        /// </summary>
        /// <param name="nodeList">节点列表</param>
        /// <param name="width">mesh宽度</param>
        /// <param name="smooth">平滑度，关键点之间要生成的段数</param>
        public void DrawMesh(Vector2[] nodeList, float width, int smooth)
        {
            if (nodeList == null || nodeList.Length < 1)
            {
                Debug.LogError("No node in nodeList.");
                return;
            }
            color = MeshColor;
            _nodeList = nodeList;
            if (_nodeList.Length == 1)
            {
                _strokeType = StrokeType.Point;
            }
            else if (_nodeList.Length == 2)
            {
                _strokeType = StrokeType.Line;
            }
            else
            {
                _strokeType = StrokeType.Curve;
            }
            _width = width;
            if (_strokeType == StrokeType.Line)
            {
                _smooth = 1;
            }
            else
            {
                _smooth = smooth;
            }
            CalculateCurve();
            CalTangent();
            InitVT();
            SetAllDirty();
        }

        private void InitVT()
        {
            ResetVUT();
            if (_strokeType == StrokeType.Point)
            {
                CalCircleMeshData(SemiCircleSegment * 2);
            }
            else
            {
                _verticesCurve = MeshUtils.GetVertices(_curvePoints, _width * 0.5f);
                int curvePolyCount = (_verticesCurve.Length - 2) / 2;   //曲线的四边形数
                for (int i = 0; i < curvePolyCount; i++)
                {
                    int idx = i * 6;
                    int i2 = i * 2;
                    _trianglesCurve[idx] = i2;
                    _trianglesCurve[idx + 1] = i2 + 2;
                    _trianglesCurve[idx + 2] = i2 + 1;
                    _trianglesCurve[idx + 3] = i2 + 1;
                    _trianglesCurve[idx + 4] = i2 + 2;
                    _trianglesCurve[idx + 5] = i2 + 3;
                }
                AddSectorMeshData();
            }
        }

        //更新当前线段比例；percent∈[0,1]
        public void UpdatePercent(float percent)
        {
            _percent = Mathf.Clamp01(percent);
            if (_percent == 0)
            {
                CrossFadeAlpha(0, 0, false);
                return;
            }
            else
            {
                CrossFadeAlpha(1, 0, false);
            }
            if (_strokeType == StrokeType.Point)
            {
            }
            else if (_strokeType == StrokeType.Line)
            {
                Array.Copy(_verticesCurve, _vertices, _verticesCurve.Length);
                Vector3 vES = _curvePoints[1] - _curvePoints[0];
                Vector3 vESP = vES * _percent;
                _vertices[2] = _verticesCurve[0] + vESP;
                _vertices[3] = _verticesCurve[1] + vESP;
                Array.Copy(_verticesSCS, 0, _vertices, _verticesCurve.Length, _verticesSCS.Length);
                Vector3[] tmpArr = new Vector3[_verticesSCE.Length];
                Vector3 vES1P = -vES * (1 - _percent);
                for (int i = 0; i < tmpArr.Length; i++)
                {
                    tmpArr[i] = _verticesSCE[i] + vES1P;
                }
                Array.Copy(tmpArr, 0, _vertices, _verticesCurve.Length + _verticesSCS.Length, _verticesSCE.Length);
                Array.Copy(_trianglesCurve, _triangles, _trianglesCurve.Length);
                Array.Copy(_trianglesSCS, 0, _triangles, _trianglesCurve.Length, _trianglesSCS.Length);
                Array.Copy(_trianglesSCE, 0, _triangles, _trianglesCurve.Length + _trianglesSCS.Length, _trianglesSCE.Length);
            }
            else
            {
                AddSectorMeshDataVer(false);
                Array.Copy(_verticesCurve, _vertices, _verticesCurve.Length);
                Array.Copy(_verticesSCS, 0, _vertices, _verticesCurve.Length, _verticesSCS.Length);
                Array.Copy(_verticesSCE, 0, _vertices, _verticesCurve.Length + _verticesSCS.Length, _verticesSCE.Length);
                int curIdx = (int)(_percent * (_curvePoints.Length - 1));
                int count = ((curIdx + 1) * 2 - 2) * 3;
                Array.Clear(_triangles, 0, _triangles.Length);
                Array.Copy(_trianglesCurve, _triangles, count);
                Array.Copy(_trianglesSCS, 0, _triangles, _trianglesCurve.Length, _trianglesSCS.Length);
                AddSectorMeshDataTri(false);
                Array.Copy(_trianglesSCE, 0, _triangles, _trianglesCurve.Length + _trianglesSCS.Length, _trianglesSCE.Length);
            }
            SetAllDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_vertices == null || _vertices.Length < 1)
            {
                return;
            }
            if (_triangles == null || _triangles.Length < 2)
            {
                return;
            }
            for (int i = 0; i < _vertices.Length; i++)
            {
                UIVertex vert = UIVertex.simpleVert;
                vert.color = color;
                vert.position = _vertices[i];
                vert.uv0 = Vector2.zero;   
                vh.AddVert(vert);
            }
            for (int i = 0; i + 2 < _triangles.Length; i += 3)
            {
                vh.AddTriangle(_triangles[i], _triangles[i + 1], _triangles[i + 2]);
            }
        }

        #region 生成mesh数据
        /// <summary>
        /// 根据nodeList计算Catmul-Rom曲线，得到curvePoints
        /// </summary>
        public void CalculateCurve()
        {
            if (_strokeType == StrokeType.Point)
            {
                _curvePoints = _nodeList;
            }
            else
            {
                int pointCount = _nodeList.Length;
                int segmentCount = pointCount - 1;    //段数为关键点数-1
                List<Vector2> allVertices = new List<Vector2>((_smooth + 1) * segmentCount); //总顶点数
                Vector2[] tempVertices = new Vector2[_smooth + 1];
                float smoothReciprocal = 1f / _smooth;
                for (int i = 0; i < segmentCount; ++i)
                {
                    Vector2 p0, p1, p2, p3; // 获得4个相邻的点以计算p1和p2之间的位置
                    p1 = _nodeList[i];
                    p0 = i == 0 ? p1 : _nodeList[i - 1];
                    p2 = _nodeList[i + 1];
                    p3 = i == segmentCount - 1 ? p2 : _nodeList[i + 2];
                    Vector2 pA = p1;
                    Vector2 pB = 0.5f * (-p0 + p2);
                    Vector2 pC = p0 - 2.5f * p1 + 2f * p2 - 0.5f * p3;
                    Vector2 pD = 0.5f * (-p0 + 3f * p1 - 3f * p2 + p3);
                    float t = 0;
                    for (int j = 0; j <= _smooth; j++)
                    {
                        tempVertices[j] = pA + t * (pB + t * (pC + t * pD));
                        t += smoothReciprocal;
                    }
                    for (int j = allVertices.Count == 0 ? 0 : 1; j < tempVertices.Length; j++)
                    {
                        allVertices.Add(tempVertices[j]);
                    }
                }
                _curvePoints = allVertices.ToArray();
            }
        }

        /// <summary>
        /// 计算切线角度;使用curvePoint来计算
        /// </summary>
        private void CalTangent()
        {
            if (_curvePoints == null)
            {
                return;
            }
            if (_curvePoints.Length < 2)
            {
                return;
            }
            Vector2 vVertical = Vector2.zero;
            _tangentQuaternions = new Quaternion[_curvePoints.Length];
            for (int i = 0; i < _curvePoints.Length; i++)
            {
                if (i == 0)
                {
                    _tangentQuaternions[i] = Quaternion.FromToRotation(Vector3.up, _curvePoints[0] - _curvePoints[1]);
                }
                else
                {
                    Vector2 v = _curvePoints[i] - _curvePoints[i - 1];
                    _tangentQuaternions[i] = Quaternion.FromToRotation(Vector3.up, v);
                    if (_tangentQuaternions[i].eulerAngles.x != 0 || _tangentQuaternions[i].eulerAngles.y != 0)
                    {//用于修正Quaternion.FromToRotation()参数为平行向量时会使得尾端半圆翻面的问题
                        _tangentQuaternions[i] = Quaternion.Euler(0, 0, _tangentQuaternions[i].eulerAngles.z);
                    }
                }
            }
        }

        #region 圆

        /// <summary>
        /// 创建扇形mesh
        /// </summary>
        private void AddSectorMeshData()
        {
            if (_verticesCurve == null || _verticesCurve.Length <= 0)
            {//没有顶点就不生成半圆;
                return;
            }
            if (_curvePoints == null || _tangentQuaternions == null)
            {
                return;
            }
            AddSectorMeshDataVer(true);
            AddSectorMeshDataTri(true);
            AddSectorMeshDataVer(false);
            AddSectorMeshDataTri(false);
        }
        /// <summary>
        /// 添加扇形mesh顶点
        /// </summary>
        /// <param name="isStart"></param>
        private void AddSectorMeshDataVer(bool isStart)
        {
            Vector3 tmpV = Vector3.zero;
            Vector3 pos = Vector3.zero;
            Quaternion rot = Quaternion.identity;
            float radius = _width / 2.0f;
            float angleRad = Mathf.Deg2Rad * 180;
            float angleDelta = angleRad / SemiCircleSegment;
            float angleCur = angleRad;
            Vector3[] lst = null;
            if (isStart)
            {
                lst = _verticesSCS;
                pos = _curvePoints[0];
                rot = _tangentQuaternions[0];
            }
            else
            {
                lst = _verticesSCE;
                int curCurveIdx = (int)(_percent * (_curvePoints.Length - 1));
                pos = _curvePoints[curCurveIdx];
                rot = _tangentQuaternions[curCurveIdx];
            }
            for (int i = 0; i < lst.Length; i++)
            {
                if (i == 0)
                {
                    tmpV.Set(pos.x, pos.y, 0);
                }
                else
                {
                    tmpV.Set(radius * Mathf.Cos(angleCur), radius * Mathf.Sin(angleCur), 0);
                    tmpV = rot * tmpV + pos;
                }
                lst[i] = tmpV;
                angleCur -= angleDelta;
            }
        }

        /// <summary>
        /// 添加扇形mesh三角形
        /// </summary>
        /// <param name="isStart"></param>
        private void AddSectorMeshDataTri(bool isStart)
        {
            int triangleCount = SemiCircleSegment;
            int vCount = 0;
            int svIdx = 0, evIdx = 0;
            int[] trianglesArr = null;
            if (isStart)
            {
                trianglesArr = _trianglesSCS;
                vCount = _verticesCurve.Length;
                svIdx = 0;
                evIdx = 1;
            }
            else
            {
                trianglesArr = _trianglesSCE;
                int curCurveIdx = (int)(_percent * (_curvePoints.Length - 1));
                vCount = _verticesSCS.Length + _verticesCurve.Length;
                int idx2 = curCurveIdx * 2;
                svIdx = idx2 + 1;
                evIdx = idx2;
            }
            for (int i = 0; i < triangleCount; i++)
            {
                int idx = i * 3;
                trianglesArr[idx] = vCount;
                if (i == 0)
                {//首
                    trianglesArr[idx + 1] = vCount + triangleCount - 1;
                    trianglesArr[idx + 2] = svIdx;
                }
                else if (i == triangleCount - 1)
                {//尾
                    trianglesArr[idx + 1] = evIdx;
                    trianglesArr[idx + 2] = vCount + 1;
                }
                else
                {
                    trianglesArr[idx + 1] = vCount + i;
                    trianglesArr[idx + 2] = vCount + i + 1;
                }
            }
        }
        /// <summary>
        ///  创建圆形mesh
        /// </summary>
        /// <param name="segments">段数</param>
        private void CalCircleMeshData(int segments)
        {
            if (_nodeList == null || _nodeList.Length <= 0)
            {
                return;
            }
            ResetVUT();
            float radius = _width / 2.0f;
            Vector2 pos = _nodeList[0];
            float angleRad = Mathf.Deg2Rad * 360;
            float angleCur = angleRad;
            float angledelta = angleRad / segments;
            Vector3 tmpV = Vector3.zero;
            Vector2 tmpUV = Vector2.zero;
            for (int i = 0; i < segments + 1; i++)
            {//顶点数为段数+1
                if (i == 0)
                {
                    tmpV.Set(pos.x, pos.y, 0);
                }
                else
                {
                    tmpV.Set(radius * Mathf.Cos(angleCur) + pos.x, radius * Mathf.Sin(angleCur) + pos.y, 0);
                }
                _vertices[i] = tmpV;
                tmpUV.Set(tmpV.x / radius / 2 + 0.5f, tmpV.z / radius / 2 + 0.5f);
                angleCur -= angledelta;
            }
            //triangles
            for (int i = 0; i < segments; i++)
            {
                int idx = i * 3;
                _triangles[idx] = 0;
                if (i == segments - 1)
                {//尾闭合
                    _triangles[idx + 1] = segments;
                    _triangles[idx + 2] = 1;
                }
                else
                {
                    _triangles[idx + 1] = i + 1;
                    _triangles[idx + 2] = i + 2;
                }
            }
        }
        #endregion

        private void ResetVUT()
        {
            //vt
            int vLen = -1;
            int tLen = -1;
            if (_strokeType == StrokeType.Point)
            {
                int circleSegment = SemiCircleSegment * 2;
                vLen = circleSegment + 1;
                tLen = circleSegment * 3;
            }
            else
            {
                int totalSemiCircleSegment = SemiCircleSegment * 2;
                vLen = _curvePoints.Length * 2 + totalSemiCircleSegment;
                tLen = (vLen - 2) * 3;
            }
            if (_vertices == null || _vertices.Length != vLen)  //|| _uv == null || _uv.Length != vLen
            {
                _vertices = new Vector3[vLen];
            }
            else
            {
                Array.Clear(_vertices, 0, _vertices.Length);
            }
            if (_triangles == null || _triangles.Length != tLen)
            {
                _triangles = new int[tLen];
            }
            else
            {
                Array.Clear(_triangles, 0, _triangles.Length);
            }
            //bak vt
            int cLen = _curvePoints.Length * 2;
            if (_verticesCurve == null || _verticesCurve.Length != cLen)  
            {
                _verticesCurve = new Vector3[cLen];
            }
            else
            {
                Array.Clear(_verticesCurve, 0, _verticesCurve.Length);
            }
            if (_verticesSCS == null || _verticesSCS.Length != SemiCircleSegment || _verticesSCE == null || _verticesSCE.Length != SemiCircleSegment)
            {
                _verticesSCS = new Vector3[SemiCircleSegment];
                _verticesSCE = new Vector3[SemiCircleSegment];
            }
            else
            {
                Array.Clear(_verticesSCS, 0, _verticesSCS.Length);
                Array.Clear(_verticesSCE, 0, _verticesSCE.Length);
            }
            int tcLen = (_curvePoints.Length * 2 - 2) * 3;
            if (_trianglesCurve == null || _trianglesCurve.Length != tcLen)
            {
                _trianglesCurve = new int[tcLen];
            }
            else
            {
                Array.Clear(_trianglesCurve, 0, _trianglesCurve.Length);
            }
            int sctLen = SemiCircleSegment * 3;
            if (_trianglesSCS == null || _trianglesSCS.Length != sctLen || _trianglesSCE == null || _trianglesSCE.Length != sctLen)
            {
                _trianglesSCS = new int[sctLen];
                _trianglesSCE = new int[sctLen];
            }
            else
            {
                Array.Clear(_trianglesSCS, 0, _trianglesSCS.Length);
                Array.Clear(_trianglesSCE, 0, _trianglesSCE.Length);
            }
        }
        #endregion
    }

    public class MeshUtils
    {
        public struct CurveSegment2D
        {//表示曲线每段的结构体
            public Vector2 point1, point2;  //该段起点及终点坐标
            public CurveSegment2D(Vector2 point1, Vector2 point2)
            {
                this.point1 = point1;
                this.point2 = point2;
            }
            public Vector2 SegmentVector
            {//该段向量
                get
                {
                    return point2 - point1;
                }
            }
        }
        /// <summary>
        /// 根据一段折线生成一组顶点
        /// </summary>
        /// <param name="curvePoints">折线数组</param>
        /// <param name="expands">顶点相对于折线点的偏移</param>
        /// <returns></returns>
        public static Vector3[] GetVertices(Vector2[] curvePoints, float expands)
        {
            if (curvePoints == null || curvePoints.Length <= 1)
            {
                return null;
            }
            List<Vector3> combinePoints = new List<Vector3>();
            List<CurveSegment2D> segments = new List<CurveSegment2D>();
            for (int i = 1; i < curvePoints.Length; i++)
            {
                segments.Add(new CurveSegment2D(curvePoints[i - 1], curvePoints[i]));
            }
            List<CurveSegment2D> segments1 = new List<CurveSegment2D>();
            List<CurveSegment2D> segments2 = new List<CurveSegment2D>();
            for (int i = 0; i < segments.Count; i++)
            {
                Vector2 vOffset = new Vector2(-segments[i].SegmentVector.y, segments[i].SegmentVector.x).normalized;
                segments1.Add(new CurveSegment2D(segments[i].point1 + vOffset * expands, segments[i].point2 + vOffset * expands));
                segments2.Add(new CurveSegment2D(segments[i].point1 - vOffset * expands, segments[i].point2 - vOffset * expands));
            }
            List<Vector2> points1 = new List<Vector2>();
            List<Vector2> points2 = new List<Vector2>();
            if (segments1.Count != segments2.Count)
            {
                Debug.LogError("segments1.Count != segments2.Count");
                return null;
            }
            for (int i = 0; i < segments1.Count; i++)
            {
                if (i == 0)
                {
                    points1.Add(segments1[0].point1);
                    points2.Add(segments2[0].point1);
                }
                else
                {
                    Vector2 crossPoint;
                    if (!CalcLinesIntersection(segments1[i - 1], segments1[i], out crossPoint, 0.1f))
                    {
                        crossPoint = segments1[i].point1;
                    }
                    points1.Add(crossPoint);
                    if (!CalcLinesIntersection(segments2[i - 1], segments2[i], out crossPoint, 0.1f))
                    {
                        crossPoint = segments2[i].point1;
                    }
                    points2.Add(crossPoint);
                }
                if (i == segments1.Count - 1)
                {
                    points1.Add(segments1[i].point2);
                    points2.Add(segments2[i].point2);
                }
            }
            for (int i = 0; i < curvePoints.Length; i++)
            {
                combinePoints.Add(points1[i]);
                combinePoints.Add(points2[i]);
            }
            return combinePoints.ToArray();
        }

        //计算两线交点
        private static bool CalcLinesIntersection(CurveSegment2D segment1, CurveSegment2D segment2, out Vector2 intersection, float angleLimit)
        {
            intersection = Vector2.zero;
            Vector2 p1 = segment1.point1;
            Vector2 p2 = segment1.point2;
            Vector2 p3 = segment2.point1;
            Vector2 p4 = segment2.point2;
            float denominator = (p2.y - p1.y) * (p4.x - p3.x) - (p1.x - p2.x) * (p3.y - p4.y);
            if (denominator == 0)
            {//如果分母为0，则表示平行
                return false;
            }
            float angle = Vector2.Angle(segment1.SegmentVector, segment2.SegmentVector);    // 检查段之间的角度
            if (angle < angleLimit || (180f - angle) < angleLimit)
            {//如果两个段之间的角度太小，我们将它们视为平行
                return false;
            }
            float x = ((p2.x - p1.x) * (p4.x - p3.x) * (p3.y - p1.y) + (p2.y - p1.y) * (p4.x - p3.x) * p1.x - (p4.y - p3.y) * (p2.x - p1.x) * p3.x) / denominator;
            float y = -((p2.y - p1.y) * (p4.y - p3.y) * (p3.x - p1.x) + (p2.x - p1.x) * (p4.y - p3.y) * p1.y - (p4.x - p3.x) * (p2.y - p1.y) * p3.y) / denominator;
            intersection.Set(x, y);
            return true;
        }
    }
}