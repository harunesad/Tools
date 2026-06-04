using System.Collections.Generic;
using UnityEngine;

namespace MotionFlow.Runtime
{
    [System.Serializable]
    public class Waypoint
    {
        public Vector3 Position;
        public Vector3 TangentIn = new Vector3(-2, 0, 0);
        public Vector3 TangentOut = new Vector3(2, 0, 0);

        public Waypoint(Vector3 position)
        {
            Position = position;
        }
    }

    [AddComponentMenu("MotionFlow/MotionFlow Path")]
    public class MotionFlowPath : MonoBehaviour
    {
        [SerializeField] private List<Waypoint> waypoints = new List<Waypoint>();
        [SerializeField] private bool loop = false;

        public List<Waypoint> Waypoints => waypoints;
        public bool Loop => loop;

        private void Reset()
        {
            // Set up default waypoints for quickstart
            waypoints.Clear();
            waypoints.Add(new Waypoint(Vector3.zero));
            waypoints.Add(new Waypoint(new Vector3(5, 2, 5)));
            waypoints.Add(new Waypoint(new Vector3(10, 0, 10)));
        }

        public Vector3 GetPoint(float t)
        {
            if (waypoints == null || waypoints.Count == 0) return transform.position;
            if (waypoints.Count == 1) return transform.TransformPoint(waypoints[0].Position);

            int segmentsCount = loop ? waypoints.Count : waypoints.Count - 1;
            if (segmentsCount <= 0) return transform.position;

            t = Mathf.Clamp01(t);
            float scaledT = t * segmentsCount;
            int startIndex = Mathf.FloorToInt(scaledT);
            int endIndex = startIndex + 1;

            if (startIndex >= segmentsCount)
            {
                startIndex = segmentsCount - 1;
                endIndex = loop ? 0 : startIndex;
            }

            if (endIndex >= waypoints.Count && loop)
            {
                endIndex = 0;
            }

            float localT = scaledT - startIndex;

            Waypoint startPt = waypoints[startIndex];
            Waypoint endPt = waypoints[endIndex];

            Vector3 p0 = transform.TransformPoint(startPt.Position);
            Vector3 p1 = transform.TransformPoint(startPt.Position + startPt.TangentOut);
            Vector3 p2 = transform.TransformPoint(endPt.Position + endPt.TangentIn);
            Vector3 p3 = transform.TransformPoint(endPt.Position);

            return CalculateBezierPoint(localT, p0, p1, p2, p3);
        }

        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
        {
            float u = 1f - t;
            float tt = t * t;
            float uu = u * u;
            float uuu = uu * u;
            float ttt = tt * t;

            Vector3 p = uuu * p0; // (1-t)^3 * p0
            p += 3f * uu * t * p1; // 3 * (1-t)^2 * t * p1
            p += 3f * u * tt * p2; // 3 * (1-t) * t^2 * p2
            p += ttt * p3; // t^3 * p3

            return p;
        }

        private void OnDrawGizmos()
        {
            if (waypoints == null || waypoints.Count < 2) return;

            Gizmos.color = Color.green;
            Vector3 previousPoint = GetPoint(0f);
            int resolution = waypoints.Count * 20;

            for (int i = 1; i <= resolution; i++)
            {
                float t = i / (float)resolution;
                Vector3 currentPoint = GetPoint(t);
                Gizmos.DrawLine(previousPoint, currentPoint);
                previousPoint = currentPoint;
            }

            // Draw waypoints nodes
            Gizmos.color = Color.yellow;
            foreach (var wp in waypoints)
            {
                Gizmos.DrawSphere(transform.TransformPoint(wp.Position), 0.15f);
            }
        }
    }
}
