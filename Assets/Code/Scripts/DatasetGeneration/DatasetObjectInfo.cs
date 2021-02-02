using System.Collections.Generic;
using System.Diagnostics.Contracts;
using UnityEngine;

public class DatasetObjectInfo : MonoBehaviour
{
    [Header("Boundaries settings")]
    [SerializeField]
    public bool isWaterContainer;
    [SerializeField]
    public bool isCamera;
    [SerializeField]
    public bool includeInDataset;
    [SerializeField]
    public Vector3 center = Vector3.zero;
    [SerializeField]
    public Vector3 size = new Vector3(1,1,1);
    //[ReadOnly]
    public Vector3 BoundarySize;
    [SerializeField]
    public string className;
    [SerializeField]
    public string typeName;
    [SerializeField]
    public int testPointsNum = 1000;

    public List<Vector3> testPoints;
    public List<bool> visiblePoints;
    private Rigidbody rb = new Rigidbody();

    public void GenerateTestPointsNonUniform(int n)
    {
        testPoints?.Clear();
        visiblePoints?.Clear();

        testPoints = new List<Vector3>();
        visiblePoints = new List<bool>();

        MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;
            GameObject currentObject = meshFilter.gameObject;
            int len = mesh.triangles.Length;

            for (int j = 0; j < n / meshFilters.Length; j++)
            {
                int i = Random.Range(0, len / 3) * 3;

                Vector3 p1 = mesh.vertices[mesh.triangles[i + 0]];
                Vector3 p2 = mesh.vertices[mesh.triangles[i + 1]];
                Vector3 p3 = mesh.vertices[mesh.triangles[i + 2]];

                float t = Random.value;
                float u = Random.value;
                if (t + u >= 1)
                {
                    t = 1 - t;
                    u = 1 - u;
                }

                Vector3 pointOnMesh = p1 + t * (p2 - p1) + u * (p3 - p1);
                pointOnMesh.Scale(currentObject.transform.lossyScale);
                pointOnMesh = currentObject.transform.rotation * pointOnMesh;
                pointOnMesh += currentObject.transform.position;

                pointOnMesh = transform.InverseTransformPoint(pointOnMesh);

                testPoints.Add(pointOnMesh);
                visiblePoints.Add(false);
            }
            
        }
        Debug.Log("done generatin points");
    }

    public void GenerateTestPointsUniform(int n)
    {
        testPoints?.Clear();
        visiblePoints?.Clear();
        testPoints = new List<Vector3>();
        visiblePoints = new List<bool>();

        MeshFilter[] meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter meshFilter in meshFilters)
        {
            Mesh mesh = meshFilter.sharedMesh;
            GameObject currentObject = meshFilter.gameObject;
            int len = mesh.triangles.Length;
            double[] triangleArea = new double[len/3];
            double totalArea = 0;

            for(int i = 0; i < len/3; i++)
            {
                Vector3 p1 = mesh.vertices[mesh.triangles[i*3 + 0]];
                Vector3 p2 = mesh.vertices[mesh.triangles[i*3 + 1]];
                Vector3 p3 = mesh.vertices[mesh.triangles[i*3 + 2]];
                Vector3 p12 = p2 - p1;
                Vector3 p13 = p3 - p1;
                double area = Vector3.Cross(p12, p13).magnitude;
                triangleArea[i] = area;
                totalArea += area;
            }
            //TODO optymalizacja macierzami 4x4
            for (int j = 0; j < n / meshFilters.Length; j++)
            {
                double randomArea = Random.value * totalArea;

                for (int i = 0; i < len/3; i++)
                {
                    randomArea -= triangleArea[i];
                    if (randomArea <= 0)
                    {
                        Vector3 p1 = mesh.vertices[mesh.triangles[i*3 + 0]];
                        Vector3 p2 = mesh.vertices[mesh.triangles[i*3 + 1]];
                        Vector3 p3 = mesh.vertices[mesh.triangles[i*3 + 2]];

                        float t = Random.value;
                        float u = Random.value;
                        if (t + u >= 1)
                        {
                            t = 1 - t;
                            u = 1 - u;
                        }

                        Vector3 pointOnMesh = p1 + t * (p2 - p1) + u * (p3 - p1);
                        pointOnMesh.Scale(currentObject.transform.lossyScale);
                        pointOnMesh = currentObject.transform.rotation * pointOnMesh;
                        pointOnMesh += currentObject.transform.position;
                        pointOnMesh = transform.InverseTransformPoint(pointOnMesh);

                        testPoints.Add(pointOnMesh);
                        visiblePoints.Add(false);
                        break;
                    }
                }
            }
        }
    }

    public VisibilityInfo CheckVisibility(Camera camera)
    {
        VisibilityInfo visibilityInfo = new VisibilityInfo
        {
            className = className,
            max = Vector3.zero,
            min = new Vector3(camera.pixelWidth, camera.pixelHeight),
            visible = false,
            visibleInFrame = false,
            gameObject = this.gameObject,
            includeInDataset = this.includeInDataset
        };
        if (isCamera || isWaterContainer) return visibilityInfo;

        visibilityInfo.distance = (camera.gameObject.transform.position - gameObject.transform.position).magnitude;

        for (int i = 0; i < testPoints.Count; i++)
        {
            visiblePoints[i] = false;
            Vector3 bPos = camera.WorldToScreenPoint(transform.TransformPoint(testPoints[i]));
            if (bPos.x < 0f || bPos.x > camera.pixelWidth || bPos.y < 0f || bPos.y > camera.pixelHeight || bPos.z < 0) continue;
            visibilityInfo.min = Vector2.Min(visibilityInfo.min, bPos);
            visibilityInfo.max = Vector2.Max(visibilityInfo.max, bPos);
            visibilityInfo.visibleInFrame = true;
            visiblePoints[i] = true;
        }
        visibilityInfo.min.x /= (float)Camera.main.pixelWidth;
        visibilityInfo.min.y /= (float)Camera.main.pixelHeight;
        visibilityInfo.max.x /= (float)Camera.main.pixelWidth;
        visibilityInfo.max.y /= (float)Camera.main.pixelHeight;

        visibilityInfo.fill = (visibilityInfo.max - visibilityInfo.min).x * (visibilityInfo.max - visibilityInfo.min).y;

        float offset = Settings.config.datasetOptions.objectFillOffset; 
        visibilityInfo.min.x = Mathf.Max(visibilityInfo.min.x - offset, 0.0001f);
        visibilityInfo.min.y = Mathf.Max(visibilityInfo.min.y - offset, 0.0001f);
        visibilityInfo.max.x = Mathf.Min(visibilityInfo.max.x + offset, 0.9999f);
        visibilityInfo.max.y = Mathf.Min(visibilityInfo.max.y + offset, 0.9999f);

        if (visibilityInfo.visibleInFrame)
        {
            if (RenderSettings.fog)
            {
                visibilityInfo.colorPercentVisible = 1f / Mathf.Exp(visibilityInfo.distance * RenderSettings.fogDensity);
                if (Settings.config.datasetOptions.checkFogVisibility)
                    visibilityInfo.visible = visibilityInfo.colorPercentVisible >= Settings.config.datasetOptions.minObjectColorPercentVisible;
                else
                    visibilityInfo.visible = true;
            }   
            else
            {
               visibilityInfo.colorPercentVisible = 1f;
               visibilityInfo.visible = true;
            }
        }
        //TODO optimalization
        return visibilityInfo;
    }

    void OnDrawGizmosSelected()
    {
        BoundarySize = GetMaxBoundaryPoint() - GetMinBoundaryPoint();
        for (int i = 0; i < testPoints.Count; i++)
        {
            if (visiblePoints[i]) Gizmos.color = Color.white;
            else Gizmos.color = Color.black;
            //Gizmos.DrawLine(transform.TransformPoint(testPoints[i]), transform.TransformPoint(testPoints[i]) + new Vector3(0.05f,0.05f,0.05f));
            Gizmos.DrawCube(transform.TransformPoint(testPoints[i]), new Vector3(0.02f,0.02f,0.02f));
        }

        Gizmos.color = Color.magenta;
        Gizmos.DrawLine(transform.TransformPoint(center + new Vector3(size.x/2f, size.y/2f, size.z/2f)), transform.TransformPoint(center + new Vector3(-size.x/2f, size.y/2f, size.z/2f)));
        Gizmos.DrawLine(transform.TransformPoint(center + new Vector3(size.x/2f, size.y/2f, size.z/2f)), transform.TransformPoint(center + new Vector3(size.x/2f, -size.y/2f, size.z/2f)));
        Gizmos.DrawLine(transform.TransformPoint(center + new Vector3(size.x/2f, size.y/2f, size.z/2f)), transform.TransformPoint(center + new Vector3(size.x/2f, size.y/2f, -size.z/2f)));
        
        Gizmos.DrawLine(transform.TransformPoint(center + new Vector3(-size.x/2f, -size.y/2f, -size.z/2f)), transform.TransformPoint(center + new Vector3(size.x/2f, -size.y/2f, -size.z/2f)));
        Gizmos.DrawLine(transform.TransformPoint(center + new Vector3(-size.x/2f, -size.y/2f, -size.z/2f)), transform.TransformPoint(center + new Vector3(-size.x/2f, size.y/2f, -size.z/2f)));
        Gizmos.DrawLine(transform.TransformPoint(center + new Vector3(-size.x/2f, -size.y/2f, -size.z/2f)), transform.TransformPoint(center + new Vector3(-size.x/2f, -size.y/2f, size.z/2f)));

        Gizmos.DrawLine(transform.TransformPoint(center + new Vector3(-size.x/2f, size.y/2f, -size.z/2f)), transform.TransformPoint(center + new Vector3(size.x/2f, size.y/2f, -size.z/2f)));
        Gizmos.DrawLine(transform.TransformPoint(center + new Vector3(-size.x/2f, size.y/2f, -size.z/2f)), transform.TransformPoint(center + new Vector3(-size.x/2f, size.y/2f, size.z/2f)));
        Gizmos.DrawLine(transform.TransformPoint(center + new Vector3(-size.x/2f, size.y/2f, size.z/2f)), transform.TransformPoint(center + new Vector3(-size.x/2f, -size.y/2f, size.z/2f)));
        Gizmos.DrawLine(transform.TransformPoint(center + new Vector3(-size.x/2f, -size.y/2f, size.z/2f)), transform.TransformPoint(center + new Vector3(size.x/2f, -size.y/2f, size.z/2f)));
        Gizmos.DrawLine(transform.TransformPoint(center + new Vector3(size.x/2f, size.y/2f, -size.z/2f)), transform.TransformPoint(center + new Vector3(size.x/2f, -size.y/2f, -size.z/2f)));
        Gizmos.DrawLine(transform.TransformPoint(center + new Vector3(size.x/2f, -size.y/2f, -size.z/2f)), transform.TransformPoint(center + new Vector3(size.x/2f, -size.y/2f, size.z/2f)));
        
        Gizmos.color = Color.green;
        DrawBoundary();
    }

    public Vector3 GetMinBoundaryPoint()
    {
        return Vector3.Min(
            transform.TransformPoint(center + new Vector3(size.x / 2f, size.y / 2f, size.z / 2f)),
            Vector3.Min(
                transform.TransformPoint(center + new Vector3(size.x / 2f, size.y / 2f, -size.z / 2f)),
                Vector3.Min(
                    transform.TransformPoint(center + new Vector3(size.x / 2f, -size.y / 2f, size.z / 2f)),
                    Vector3.Min(
                        transform.TransformPoint(center + new Vector3(size.x / 2f, -size.y / 2f, -size.z / 2f)),
                        Vector3.Min(
                            transform.TransformPoint(center + new Vector3(-size.x / 2f, size.y / 2f, size.z / 2f)),
                            Vector3.Min(
                            transform.TransformPoint(center + new Vector3(-size.x / 2f, size.y / 2f, -size.z / 2f)),
                                Vector3.Min(
                                    transform.TransformPoint(center + new Vector3(-size.x / 2f, -size.y / 2f, size.z / 2f)),
                                    transform.TransformPoint(center + new Vector3(-size.x / 2f, -size.y / 2f, -size.z / 2f))
                                )
                            )
                        )
                    )
                )
            )
        );
    }

    public Vector3 GetMaxBoundaryPoint()
    {
        return Vector3.Max(
            transform.TransformPoint(center + new Vector3(size.x / 2f, size.y / 2f, size.z / 2f)),
            Vector3.Max(
                transform.TransformPoint(center + new Vector3(size.x / 2f, size.y / 2f, -size.z / 2f)),
                Vector3.Max(
                    transform.TransformPoint(center + new Vector3(size.x / 2f, -size.y / 2f, size.z / 2f)),
                    Vector3.Max(
                        transform.TransformPoint(center + new Vector3(size.x / 2f, -size.y / 2f, -size.z / 2f)),
                        Vector3.Max(
                            transform.TransformPoint(center + new Vector3(-size.x / 2f, size.y / 2f, size.z / 2f)),
                            Vector3.Max(
                            transform.TransformPoint(center + new Vector3(-size.x / 2f, size.y / 2f, -size.z / 2f)),
                                Vector3.Max(
                                    transform.TransformPoint(center + new Vector3(-size.x / 2f, -size.y / 2f, size.z / 2f)),
                                    transform.TransformPoint(center + new Vector3(-size.x / 2f, -size.y / 2f, -size.z / 2f))
                                )
                            )
                        )
                    )
                )
            )
        );
    }

    public Vector3 GetBoundarySize()
    {           
        return GetMaxBoundaryPoint() - GetMinBoundaryPoint();
    }

    public void DrawBoundary()
    {
        Gizmos.DrawWireCube((GetMaxBoundaryPoint() + GetMinBoundaryPoint()) / 2f, GetMaxBoundaryPoint() - GetMinBoundaryPoint());
    }

    public static bool AreColliding(GameObject elementA, GameObject elementB)
    {//TODO CHECK
        DatasetObjectInfo infoA = elementA.GetComponent<DatasetObjectInfo>();
        DatasetObjectInfo infoB = elementB.GetComponent<DatasetObjectInfo>();

        Vector3 aMin = infoA.GetMinBoundaryPoint();
        Vector3 aMax = infoA.GetMaxBoundaryPoint();
        Vector3 bMin = infoB.GetMinBoundaryPoint();
        Vector3 bMax = infoB.GetMaxBoundaryPoint();

        return (aMin.x <= bMax.x && aMax.x >= bMin.x) &&
            (aMin.y <= bMax.y && aMax.y >= bMin.y) &&
            (aMin.z <= bMax.z && aMax.z >= bMin.z);
    }

    public static bool BoundaryIsColliding(GameObject boundaryObject, GameObject pointObject)
    {
        DatasetObjectInfo boundaryInfo = boundaryObject.GetComponent<DatasetObjectInfo>();
        DatasetObjectInfo pointInfo = pointObject.GetComponent<DatasetObjectInfo>();
        Vector3 aMin = boundaryInfo.GetMinBoundaryPoint();
        Vector3 aMax = boundaryInfo.GetMaxBoundaryPoint();

        foreach(var pointRaw in pointInfo.testPoints)
        {
            var point = pointObject.transform.TransformPoint(pointRaw);
            if ( (aMin.x <= point.x && aMax.x >= point.x) &&
             (aMin.y <= point.y && aMax.y >= point.y) &&
             (aMin.z <= point.z && aMax.z >= point.z)) return true;
        }
        return false;
    }

    public static Vector3 GetRandomWorldPosistionInBoundary(GameObject element, GameObject container)
    {
        DatasetObjectInfo elementInfo = element.GetComponent<DatasetObjectInfo>();
        DatasetObjectInfo containterInfo = container.GetComponent<DatasetObjectInfo>();
        var elementSize = elementInfo.GetBoundarySize();
        var containerMin = containterInfo.GetMinBoundaryPoint();
        var containerMax = containterInfo.GetMaxBoundaryPoint();

        Vector3 transformedCenterOffset = element.transform.TransformDirection(elementInfo.center);
        float x = Random.Range(containerMin.x + elementSize.x / 2f - transformedCenterOffset.x, containerMax.x - elementSize.x / 2f - transformedCenterOffset.x);
        float y = Random.Range(containerMin.y + elementSize.y / 2f - transformedCenterOffset.y, containerMax.y - elementSize.y / 2f - transformedCenterOffset.y);
        float z = Random.Range(containerMin.z + elementSize.z / 2f - transformedCenterOffset.z, containerMax.z - elementSize.z / 2f - transformedCenterOffset.z);
        if (-(containerMin.y + elementSize.y / 2f - transformedCenterOffset.y) + (containerMax.y - elementSize.y / 2f - transformedCenterOffset.y) < 0 )
            y = containerMin.y + elementSize.y / 2f - transformedCenterOffset.y;
        return new Vector3(x,y,z);
    }

    public static Vector3 GetRandomWorldPosistionInBoundary(GameObject element, GameObject container, GameObject target)
    {
        DatasetObjectInfo elementInfo = element.GetComponent<DatasetObjectInfo>();
        DatasetObjectInfo containterInfo = container.GetComponent<DatasetObjectInfo>();
        var elementSize = elementInfo.GetBoundarySize();
        var containerMin = containterInfo.GetMinBoundaryPoint();
        var containerMax = containterInfo.GetMaxBoundaryPoint();

        Vector3 transformedCenterOffset = element.transform.TransformDirection(elementInfo.center);
        List<Vector3> positions = new List<Vector3>(10);
        List<float> distancesToElement = new List<float>(positions.Count);

        for (int i = 0; i < positions.Capacity; i++)
        {
            float x = Random.Range(containerMin.x + elementSize.x / 2f - transformedCenterOffset.x, containerMax.x - elementSize.x / 2f - transformedCenterOffset.x);
            float y = Random.Range(containerMin.y + elementSize.y / 2f - transformedCenterOffset.y, containerMax.y - elementSize.y / 2f - transformedCenterOffset.y);
            float z = Random.Range(containerMin.z + elementSize.z / 2f - transformedCenterOffset.z, containerMax.z - elementSize.z / 2f - transformedCenterOffset.z);
            if (-(containerMin.y + elementSize.y / 2f - transformedCenterOffset.y) + (containerMax.y - elementSize.y / 2f - transformedCenterOffset.y) < 0)
                y = containerMin.y + elementSize.y / 2f - transformedCenterOffset.y;
            positions.Add(new Vector3( x, y, z ));
            distancesToElement.Add((target.transform.position - positions[positions.Count-1]).sqrMagnitude);
        }

        int index = -1;
        float smallestDistance = Mathf.Infinity;
        for(int i = 0; i < distancesToElement.Count; i++)
        {
            if(distancesToElement[i] < smallestDistance)
            {
                index = i;
                smallestDistance = distancesToElement[i];
            }
        }

        return positions[index];
    }

    public static Vector3 GetRandomWorldPosistionInBoundaryClosest(GameObject element, GameObject container, GameObject target)
    {
        DatasetObjectInfo elementInfo = element.GetComponent<DatasetObjectInfo>();
        DatasetObjectInfo containterInfo = container.GetComponent<DatasetObjectInfo>();
        DatasetObjectInfo targetInfo = target.GetComponent<DatasetObjectInfo>();
        var elementSize = elementInfo.GetBoundarySize();
        var containerMin = containterInfo.GetMinBoundaryPoint();
        var containerMax = containterInfo.GetMaxBoundaryPoint();

        Vector3 transformedCenterOffset = element.transform.TransformDirection(elementInfo.center);
        Vector3 currentElementPosition = element.transform.position;

        float x = Random.Range(containerMin.x + elementSize.x / 2f - transformedCenterOffset.x, containerMax.x - elementSize.x / 2f - transformedCenterOffset.x);
        float y = Random.Range(containerMin.y + elementSize.y / 2f - transformedCenterOffset.y, containerMax.y - elementSize.y / 2f - transformedCenterOffset.y);
        float z = Random.Range(containerMin.z + elementSize.z / 2f - transformedCenterOffset.z, containerMax.z - elementSize.z / 2f - transformedCenterOffset.z);
        if (-(containerMin.y + elementSize.y / 2f - transformedCenterOffset.y) + (containerMax.y - elementSize.y / 2f - transformedCenterOffset.y) < 0)
            y = containerMin.y + elementSize.y / 2f - transformedCenterOffset.y;

        Vector3 endPoint = target.transform.TransformPoint(targetInfo.testPoints[Random.Range(0, targetInfo.testPoints.Count)]);
        float t = 0f;

        float p = 0f, k = 1f, s;
        for (int i = 0; i < 10; i++)//Bin search
        {
            s = (p + k) / 2f;
            element.transform.position = Vector3.Lerp(new Vector3(x, y, z), endPoint, s);
            if (!BoundaryIsColliding(element, target) && IsInside(element, container))
            {
                p = s;
                t = s;
            }
            else
            {
                k = s;
                t = p;
            }
        }
        float offset = Settings.config.datasetOptions.closestMaxDistanceOffset;

        float dt = 1f / (new Vector3(x, y, z) - endPoint).magnitude * offset * Random.value;

        element.transform.position = Vector3.Lerp(new Vector3(x, y, z), endPoint, t-dt);
        if (!BoundaryIsColliding(element, target) && IsInside(element, container))
        {
            element.transform.position = currentElementPosition;
            return Vector3.Lerp(new Vector3(x, y, z), endPoint, t - dt);
        }
        else
        {
            element.transform.position = currentElementPosition;
            return Vector3.Lerp(new Vector3(x, y, z), endPoint, t);
        }
    }


    public static bool IsInside(GameObject element, GameObject container)
    {
        DatasetObjectInfo elementInfo = element.GetComponent<DatasetObjectInfo>();
        DatasetObjectInfo containterInfo = container.GetComponent<DatasetObjectInfo>();
        var elementSize = elementInfo.GetBoundarySize();
        var containerMin = containterInfo.GetMinBoundaryPoint();
        var containerMax = containterInfo.GetMaxBoundaryPoint();

        Vector3 transformedCenterOffset = element.transform.TransformDirection(elementInfo.center);
        Vector3 currentElementPosition = element.transform.position;

        float x = element.transform.position.x;
        float y = element.transform.position.y;
        float z = element.transform.position.z;

        float minX = containerMin.x + elementSize.x / 2f - transformedCenterOffset.x;
        float maxX = containerMax.x - elementSize.x / 2f - transformedCenterOffset.x;
        float minY = containerMin.y + elementSize.y / 2f - transformedCenterOffset.y;
        float maxY = containerMax.y - elementSize.y / 2f - transformedCenterOffset.y;
        float minZ = containerMin.z + elementSize.z / 2f - transformedCenterOffset.z;
        float maxZ = containerMax.z - elementSize.z / 2f - transformedCenterOffset.z;
       
        return x>=minX && x<=maxX &&  y>=minY && y<=maxY &&  z>=minZ && z<=maxZ;
    }
}

public class VisibilityInfo
{
    public bool visible;
    public bool visibleInFrame;
    public Vector3 min, max;
    public float fill;
    public string className;
    public GameObject gameObject;
    public float distance;
    public float colorPercentVisible;
    public bool includeInDataset;

    public string GetTextInfo()
    {
        int nr = 0;
        for (int i = 0; i < Settings.config.datasetOptions.classNames.Count; i++) if (className == Settings.config.datasetOptions.classNames[i]) { nr = i; break; }
        return (nr +" " + (min.x + max.x)/2f + " " + (1f-(min.y + max.y) / 2f) + " " + (-min.x + max.x) + " " + (-min.y + max.y)).Replace(",", ".");
    }
}
