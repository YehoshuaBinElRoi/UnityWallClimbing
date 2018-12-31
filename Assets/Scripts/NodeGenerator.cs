
﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;


public class NodeGenerator : MonoBehaviour
{

    public float verticalPlacementDistance;//Ideal, half the vertical jump distance
    public float horizontalPlacementDistance;//Ideal, half the horizontal jump distance

    /*public*/
    GameObject climbingSurface;
    public GameObject nodePrefab;

    public LayerMask climbingSurfaceLayer;
    [SerializeField]
    LayerMask ledgeLayer;
    [SerializeField]
    Material bridgeMaterial;
    [SerializeField]
    int bridgeBuildPrecision;
    [SerializeField]
    float nodePlacementStartHeight;

    Bounds climbingSurfaceBounds;
    Vector3 rearLeftMultiplier = new Vector3(-1.0f, 0.0f, -1.0f);
    Vector3 rearRightMultiplier = new Vector3(1.0f, 0.0f, -1.0f);
    Vector3 frontLeftMultiplier = new Vector3(-1.0f, 0.0f, 1.0f);
    Vector3 frontRightMultiplier = new Vector3(1.0f, 0.0f, 0.0f);

    Bounds nodeBounds;

    Vector3 rearLeft;
    Vector3 rearRight;
    Vector3 frontLeft;
    Vector3 frontRight;

    bool generated = false;

    List<List<RaycastHit>> allEdgeLoops = new List<List<RaycastHit>>();
    List<List<EdgePoint>> allSelectedPoints = new List<List<EdgePoint>>();



    // Use this for initialization
    void Start()
    {
        climbingSurface = gameObject;
        climbingSurfaceBounds = climbingSurface.GetComponent<Renderer>().bounds;

        nodeBounds = nodePrefab.GetComponentInChildren<Renderer>().bounds;

        rearLeft = climbingSurfaceBounds.center + Vector3.Scale(climbingSurfaceBounds.extents, rearLeftMultiplier);
        rearRight = climbingSurfaceBounds.center + Vector3.Scale(climbingSurfaceBounds.extents, rearRightMultiplier);
        frontLeft = climbingSurfaceBounds.center + Vector3.Scale(climbingSurfaceBounds.extents, frontLeftMultiplier);
        frontRight = climbingSurfaceBounds.center + Vector3.Scale(climbingSurfaceBounds.extents, frontRightMultiplier);

        GenerateNodes();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void GenerateNodes()
    {
        float yPosition = climbingSurfaceBounds.center.y - climbingSurfaceBounds.extents.y + nodePlacementStartHeight;
        List<RaycastHit> edgeLoop = new List<RaycastHit>();

        //Scan from all four corners of the bounds, all the way up
        //To identify the edge loop at each level

        while (yPosition < climbingSurfaceBounds.center.y + climbingSurfaceBounds.extents.y)
        {
            yPosition += verticalPlacementDistance;
            GetEdgeLoop(yPosition);
        }

        Debug.Log(allEdgeLoops.Count);
        GeneratePath(0);

    }

    //List<RaycastHit> GetEdgeLoop(float p_height)
    void GetEdgeLoop(float p_height)
    {
        //Finds an edge loop at height p_height
        RaycastHit hitDetails;
        Vector3 referenceRay = new Vector3();
        List<RaycastHit> edgeLoop = new List<RaycastHit>();
        Vector3 centre = new Vector3();
        Vector3 pointOnCircumference = new Vector3(); //First ray will be cast to this point
        Vector3 startPoint = new Vector3();
        Vector3 endPoint = new Vector3();
        EdgeDirection firstPointSpot = (EdgeDirection)Random.Range(0, System.Enum.GetValues(typeof(EdgeDirection)).Length);
        centre = climbingSurfaceBounds.center;
        centre.y = p_height;

        pointOnCircumference = frontLeft + frontLeftMultiplier;
        pointOnCircumference.y = p_height;

        Quaternion q;
        Vector3 d;

        referenceRay = pointOnCircumference - centre;

        float i = 0.0f;

        Debug.DrawRay(centre,
                referenceRay,
                Color.black,
                3.0f);

        while (i <= 360.0f)
        {
            //Find the Quaternion corresponding to rotating anything i degree around Y-Axis
            q = Quaternion.AngleAxis(i, Vector3.up);
            //Apply the rotation to the scanner Ray
            d = q * referenceRay;
            i += 0.01f;
            startPoint = centre + d;
            endPoint = centre;
            hitDetails = new RaycastHit();
            //Perform a raycast to find a point and it's normal
            if (Physics.Raycast(startPoint, (endPoint - startPoint), out hitDetails, d.magnitude, climbingSurfaceLayer))
            {
                edgeLoop.Add(hitDetails);
            }
        }

        allEdgeLoops.Add(edgeLoop);
    }


    List<EdgePoint> FindEquiDistancedPoints(int p_layerIndex, int p_lastSelectedPoint = -1)
    {
        List<EdgePoint> selectedHits = new List<EdgePoint>();
        List<RaycastHit> allHits = allEdgeLoops[p_layerIndex];

        //RaycastHit lastSelectedHit = new RaycastHit();
        EdgePoint lastSelectedHit = new EdgePoint();

        //RaycastHit prevHit = new RaycastHit();
        EdgePoint prevHit = new EdgePoint();


        float angleBetweenPoints;
        int currentIndex = 0;
        int lastSelectedHitIndex = 0;

        //For all rows other than the bottom one, we need to select a start point in proximity of any point from previous row
        if (p_lastSelectedPoint == -1)
        {
            lastSelectedHit.index = 0;
            lastSelectedHit.hit = allHits[0];
            selectedHits.Add(lastSelectedHit);

        }
        else
        {
            prevHit = new EdgePoint();
            prevHit.index = 0;
            prevHit.hit = allHits[0];
            foreach (RaycastHit hit in allHits)
            {

                if (Vector3.Distance(hit.point, allEdgeLoops[p_layerIndex - 1][p_lastSelectedPoint].point) >= verticalPlacementDistance)
                {
                    selectedHits.Add(prevHit);
                    lastSelectedHit = prevHit;
                    break;

                }
                prevHit = new EdgePoint();
                prevHit.index = currentIndex;
                prevHit.hit = hit;
                currentIndex++;
            }
        }

        currentIndex = 0;
        foreach (RaycastHit hit in allHits)
        {

            if (Vector3.Distance(hit.point, lastSelectedHit.hit.point) >= horizontalPlacementDistance)
            {
                selectedHits.Add(prevHit);
                lastSelectedHit = prevHit;
            }
            prevHit = new EdgePoint();
            prevHit.index = currentIndex;
            prevHit.hit = hit;
            currentIndex++;
        }

        return selectedHits;
    }


    void GeneratePath(int p_layerIndex = 0, int p_startPosition = 0)
    {
        //List<RaycastHit> currentLayer = allEdgeLoops[p_layerIndex];
        List<EdgePoint> selectedPoints;
        EdgePoint lastSelectedPoint = new EdgePoint();
        float angleBetweenPoints;
        bool skipBlock = false;
        bool nextLine = false;
        int index;

        if (p_layerIndex == allEdgeLoops.Count - 1)
        {
            //If we have reached the last layer, STOP
            return;
        }

        selectedPoints = FindEquiDistancedPoints(p_layerIndex);

        index = 0;
        //foreach(RaycastHit hit in currentLayer)
        foreach (EdgePoint hit in selectedPoints)
        {
            //Go through nodes in the current layer and select nodes to be used as a part of the path

            if (skipBlock)
            {
                //If a block was skipped last time,
                //Do not skip again this time
                skipBlock = false;
            }
            else
            {
                //IF LAST ACTION != SKIP 1 BLOCK
                //  Decide if 1 block needs to be skipped (Randomly)
                skipBlock = Random.Range(1, 100) % 2 == 0 ? true : false;

            }

            if (!skipBlock)
            {
                //If the prev hit and current hit are pointing in completely different direction (deviating over 20 degrees)
                angleBetweenPoints = Vector3.Angle(hit.hit.normal, lastSelectedPoint.hit.normal);
                if (angleBetweenPoints >= 20.0f)
                {
                    Debug.DrawRay(hit.hit.point,
                                    hit.hit.normal,
                                    Color.blue,
                                    3.0f);

                    //InstantiateBridge(p_layerIndex, lastSelectedPoint.index, hit.index);
                    InstantiateBridge_UniquePoints(p_layerIndex, lastSelectedPoint.index, hit.index);
                }
                else
                {
                    Debug.DrawRay(hit.hit.point,
                                    hit.hit.normal,
                                    Color.green,
                                    3.0f);
                }

                //Instantiate a node at this point
                InstantiateNode(hit.hit.point, hit.hit.normal);
                lastSelectedPoint = hit;

            }


            //if this is the last node of this level
            if (index == selectedPoints.Count - 1)
            {
                //  GO to next level
                GeneratePath(p_layerIndex + 1);
                break;
            }
            else
            {
                //  Decide if we need to go to to next level
                nextLine = Random.Range(1, 999) % 7.5 == 0 ? true : false;
                if (nextLine)
                {
                    GeneratePath(p_layerIndex + 1);
                    break;
                }
            }
            index++;

        }

    }


    void InstantiateBridge_UniquePoints(int p_layerIndex, int p_start, int p_end)
    {
        List<RaycastHit> hits = allEdgeLoops[p_layerIndex];

        Vector3 frontPoint;
        Vector3 topPoint;
        Vector3 bottomPoint;
        Vector3 topRearPoint;
        Vector3 bottomRearPoint;

        

        List<Vector3> frontFacing_frontPoints = new List<Vector3>();
        int frontFacing_frontPointsArrayIndexDisplacement = 0;

        List<Vector3> frontFacing_topPoints = new List<Vector3>();
        int frontFacing_topPointsArrayIndexDisplacement = 1;

        List<Vector3> frontFacing_bottomPoints = new List<Vector3>();
        int frontFacing_bottomPointsArrayIndexDisplacement = 2;

        List<Vector3> topFacing_frontPoints = new List<Vector3>();
        int topFacing_frontPointsArrayIndexDisplacement = 3;

        List<Vector3> topFacing_topRearPoints = new List<Vector3>();
        int topFacing_topRearPointsArrayIndexDisplacement = 4;

        List<Vector3> bottomFacing_bottomPoints = new List<Vector3>();
        int bottomFacing_bottomPointsArrayIndexDisplacement = 5;

        List<Vector3> bottomFacing_bottomRearPoints = new List<Vector3>();
        int bottomFacing_bottomRearPointsArrayIndexDisplacement = 6;

        int pointsCount_singleRow;

        //We need 10 points to generate one unit of ledge

        int frontFacing_topRight;
        int frontFacing_topLeft;
        int frontFacing_frontRight;
        int frontFacing_frontLeft;
        int frontFacing_bottomRight;
        int frontFacing_bottomLeft;

        int topFacing_topRight;
        int topFacing_topLeft;
        int topFacing_topRearRight;
        int topFacing_topRearLeft;

        int bottomFacing_bottomRight;
        int bottomFacing_bottomLeft;
        int bottomFacing_bottomRearRight;
        int bottomFacing_bottomRearLeft;

        RaycastHit rearPointHit;
        List<Vector3> vertices = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        GameObject bridge = new GameObject("Bridge");
        MeshRenderer bridgeRenderer = bridge.AddComponent<MeshRenderer>();
        MeshCollider bridgeCollider = bridge.AddComponent<MeshCollider>();
        bridge.layer = nodePrefab.layer;
        bridgeRenderer.material = bridgeMaterial;// gameObject.GetComponent<MeshRenderer>().material;
        MeshFilter bridgeMeshFilter = bridge.AddComponent<MeshFilter>();
        Mesh bridgeMesh = new Mesh();
        bridgeMesh.Clear(false);
        bridgeMeshFilter.mesh = bridgeMesh;
        bridgeMesh.name = "BridgeMesh";
        Vector3[] meshVertices;
        int[] meshTriangles;
        Vector2[] meshUV;

        for (int i = p_start; i < p_end; i+= bridgeBuildPrecision)
        {
            frontPoint = hits[i].point + (0.5f * hits[i].normal);
            topPoint = frontPoint + new Vector3(0.0f, 0.2f, 0.0f);

            if (Physics.Raycast(topPoint, -hits[i].normal, out rearPointHit, 10.0f /*As opposed to infinity*/, climbingSurfaceLayer))
            {
                //Find a rear point on the surface 
                topRearPoint = rearPointHit.point;
            }
            else
            {
                //If not found, find a point close enough
                topRearPoint = topPoint + (-0.5f * hits[i].normal);
            }

            bottomPoint = frontPoint - new Vector3(0.0f, 0.2f, 0.0f);

            if (Physics.Raycast(bottomPoint, -hits[i].normal, out rearPointHit, 10.0f /*As opposed to infinity*/, climbingSurfaceLayer))
            {
                //Find a rear point on the surface 
                bottomRearPoint = rearPointHit.point;
            }
            else
            {
                //If not found, find a point close enough
                bottomRearPoint = bottomPoint + (-0.5f * hits[i].normal);
            }

            frontFacing_frontPoints.Add(frontPoint);
            //normals.Add(hits[i].normal);

            frontFacing_topPoints.Add(topPoint);
            //normals.Add(hits[i].normal);

            frontFacing_bottomPoints.Add(bottomPoint);
            //normals.Add(hits[i].normal);

            topFacing_frontPoints.Add(topPoint);
            //normals.Add(Vector3.up);

            topFacing_topRearPoints.Add(topRearPoint);
            //normals.Add(Vector3.up);


            bottomFacing_bottomPoints.Add(bottomPoint);
            //normals.Add(-Vector3.up);

            bottomFacing_bottomRearPoints.Add(bottomRearPoint);
            //normals.Add(-Vector3.up);

        }

        //Add all identified points to verticces
        vertices.AddRange(frontFacing_frontPoints);
        vertices.AddRange(frontFacing_topPoints);
        vertices.AddRange(frontFacing_bottomPoints);
        vertices.AddRange(topFacing_frontPoints);
        vertices.AddRange(topFacing_topRearPoints);
        vertices.AddRange(bottomFacing_bottomPoints);
        vertices.AddRange(bottomFacing_bottomRearPoints);

        meshVertices = vertices.ToArray();


        //Debug.Log(frontPoints.Count);
        pointsCount_singleRow = frontFacing_frontPoints.Count;

        for (int i = 0; i < pointsCount_singleRow - 1; i++)
        {
            Debug.DrawLine(frontFacing_frontPoints[i], frontFacing_frontPoints[i + 1], Color.red, 3.0f);
            Debug.DrawLine(topFacing_frontPoints[i], topFacing_frontPoints[i + 1], Color.red, 3.0f);
            Debug.DrawLine(topFacing_topRearPoints[i], topFacing_topRearPoints[i + 1], Color.red, 3.0f);
            Debug.DrawLine(bottomFacing_bottomPoints[i], bottomFacing_bottomPoints[i + 1], Color.red, 3.0f);
            Debug.DrawLine(bottomFacing_bottomRearPoints[i], bottomFacing_bottomRearPoints[i + 1], Color.red, 3.0f);

        }

        for (int i = 0; i < pointsCount_singleRow - 1; i++)
        {
            //Identify points composing each triangle, for every inch
            frontFacing_frontRight = i + (pointsCount_singleRow * frontFacing_frontPointsArrayIndexDisplacement);
            frontFacing_frontLeft = frontFacing_frontRight + 1;

            frontFacing_topRight = i + (pointsCount_singleRow * frontFacing_topPointsArrayIndexDisplacement);
            frontFacing_topLeft = frontFacing_topRight + 1;

            frontFacing_bottomRight = i + (pointsCount_singleRow * frontFacing_bottomPointsArrayIndexDisplacement);
            frontFacing_bottomLeft = frontFacing_bottomRight + 1;

            topFacing_topRight = i + (pointsCount_singleRow * topFacing_frontPointsArrayIndexDisplacement);
            topFacing_topLeft = topFacing_topRight + 1;

            topFacing_topRearRight = i + (pointsCount_singleRow * topFacing_topRearPointsArrayIndexDisplacement);
            topFacing_topRearLeft = topFacing_topRearRight + 1;

            bottomFacing_bottomRight = i + (pointsCount_singleRow * bottomFacing_bottomPointsArrayIndexDisplacement);
            bottomFacing_bottomLeft = bottomFacing_bottomRight + 1;
            bottomFacing_bottomRearRight = i + (pointsCount_singleRow * bottomFacing_bottomRearPointsArrayIndexDisplacement);
            bottomFacing_bottomRearLeft = bottomFacing_bottomRearRight + 1;

            /*if (frontRight >= meshVertices.Length ||
                 frontLeft >= meshVertices.Length ||
                 topRight >= meshVertices.Length ||
                 topLeft >= meshVertices.Length ||
                 bottomRight >= meshVertices.Length ||
                 bottomLeft >= meshVertices.Length ||
                 topRearRight >= meshVertices.Length ||
                 topRearLeft >= meshVertices.Length ||
                 bottomRearRight >= meshVertices.Length ||
                 bottomRearLeft >= meshVertices.Length)
            {
                Debug.Log(i + "," +
                    frontRight + "," +
                    frontLeft + "," +
                    topRight + "," +
                    topLeft + "," +
                    bottomRight + "," +
                    bottomLeft + "," +
                    topRearRight + "," +
                    topRearLeft + "," +
                    bottomRearRight + "," +
                    bottomRearLeft);
            }*/
            //Construct the mesh one triangle/vertex at a time

            //Top facing, right triangle
            //triangles.Add(topFacing_topRight); triangles.Add(topFacing_topRearRight); triangles.Add(topFacing_topRearLeft);
            //Top facing, left triangle
            //triangles.Add(topFacing_topRight); triangles.Add(topFacing_topRearLeft); triangles.Add(topFacing_topLeft);

            //Top facing, right triangle
            triangles.Add(topFacing_topRearLeft); triangles.Add(topFacing_topRearRight); triangles.Add(topFacing_topRight);
            //Top facing, left triangle
            triangles.Add(topFacing_topRearLeft); triangles.Add(topFacing_topRight); triangles.Add(topFacing_topLeft);

            //Front facing, right triangle, for top half
            //triangles.Add(frontFacing_frontRight); triangles.Add(frontFacing_topRight); triangles.Add(frontFacing_topLeft);
            //Front facing, left triangle, for top half
            //triangles.Add(frontFacing_frontRight); triangles.Add(frontFacing_topLeft); triangles.Add(frontFacing_frontLeft);
            //Front facing, right triangle, for top half
            triangles.Add(frontFacing_topLeft); triangles.Add(frontFacing_topRight); triangles.Add(frontFacing_frontRight);
            //Front facing, left triangle, for top half
            triangles.Add(frontFacing_topLeft); triangles.Add(frontFacing_frontRight);  triangles.Add(frontFacing_frontLeft);


            //Front facing, right triangle, for bottom half
            //triangles.Add(frontFacing_bottomRight); triangles.Add(frontFacing_frontRight); triangles.Add(frontFacing_frontLeft);
            //Front facing, left triangle, for bottom half
            //triangles.Add(frontFacing_bottomRight); triangles.Add(frontFacing_frontLeft); triangles.Add(frontFacing_bottomLeft);

            //Front facing, right triangle, for bottom half
            triangles.Add(frontFacing_frontLeft); triangles.Add(frontFacing_frontRight); triangles.Add(frontFacing_bottomRight);
            //Front facing, left triangle, for bottom half
            triangles.Add(frontFacing_frontLeft); triangles.Add(frontFacing_bottomRight);  triangles.Add(frontFacing_bottomLeft);

            //Bottom facing, right triangle
            //triangles.Add(bottomFacing_bottomRight); triangles.Add(bottomFacing_bottomRearLeft); triangles.Add(bottomFacing_bottomRearRight);
            //Bottom facing, left triangle
            //triangles.Add(bottomFacing_bottomRight); triangles.Add(bottomFacing_bottomLeft); triangles.Add(bottomFacing_bottomRearLeft);
            //Bottom facing, right triangle
            triangles.Add(bottomFacing_bottomRight); triangles.Add(bottomFacing_bottomRearRight); triangles.Add(bottomFacing_bottomRearLeft);
            //Bottom facing, left triangle
            triangles.Add(bottomFacing_bottomRight); triangles.Add(bottomFacing_bottomRearLeft); triangles.Add(bottomFacing_bottomLeft); 

        }
        meshTriangles = triangles.ToArray();

        for (int i = 0; i < meshVertices.Length; i++)
        {
            uv.Add(new Vector2(0.0f, 0.0f));
        }

        meshUV = uv.ToArray();
        bridgeMesh.vertices = meshVertices;
        //bridgeMesh.normals = normals.ToArray();

        bridgeMesh.triangles = meshTriangles;
        bridgeMesh.uv = meshUV;
        bridgeMesh.RecalculateNormals();
        bridgeCollider.convex = true;
        //bridgeMesh.Optimize();

        bridgeCollider.sharedMesh = bridgeMesh;

    }


    void InstantiateBridge(int p_layerIndex, int p_start, int p_end)
    {
        List<RaycastHit> hits = allEdgeLoops[p_layerIndex];
        List<Vector3> topRearPoints = new List<Vector3>();
        Vector3 topRearPoint;
        int topRearPointsArrayIndexDisplacement = 2;
        List<Vector3> topPoints = new List<Vector3>();
        Vector3 topPoint;
        int topPointsArrayIndexDisplacement = 1;
        List<Vector3> frontPoints = new List<Vector3>();
        Vector3 frontPoint;
        int frontPointsArrayIndexDisplacement = 0;
        List<Vector3> bottomPoints = new List<Vector3>();
        Vector3 bottomPoint;
        int bottomPointsArrayIndexDisplacement = 3;
        List<Vector3> bottomRearPoints = new List<Vector3>();
        Vector3 bottomRearPoint;
        int bottomRearPointsArrayIndexDisplacement = 4;


        //We need 10 points to generate one unit of ledge
        int topRearRight;
        int topRearLeft;
        int topRight;
        int topLeft;
        int frontRight;
        int frontLeft;
        int bottomRight;
        int bottomLeft;
        int bottomRearRight;
        int bottomRearLeft;

        RaycastHit rearPointHit;
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uv = new List<Vector2>();

        GameObject bridge = new GameObject("Bridge");
        MeshRenderer bridgeRenderer = bridge.AddComponent<MeshRenderer>();
        MeshCollider bridgeCollider = bridge.AddComponent<MeshCollider>();
        bridge.layer = nodePrefab.layer;
        bridgeRenderer.material = gameObject.GetComponent<MeshRenderer>().material;
        MeshFilter bridgeMeshFilter = bridge.AddComponent<MeshFilter>();
        Mesh bridgeMesh = new Mesh();
        bridgeMesh.Clear(false);
        bridgeMeshFilter.mesh = bridgeMesh;
        bridgeMesh.name = "BridgeMesh";
        Vector3[] meshVertices;
        int[] meshTriangles;
        Vector2[] meshUV;

        for (int i = p_start; i < p_end; i++)
        {
            frontPoint = hits[i].point + (0.5f * hits[i].normal);
            topPoint = frontPoint + new Vector3(0.0f, 0.2f, 0.0f);

            if (Physics.Raycast(topPoint, -hits[i].normal, out rearPointHit, 10.0f /*As opposed to infinity*/, climbingSurfaceLayer))
            {
                //Find a rear point on the surface 
                topRearPoint = rearPointHit.point;
            }
            else
            {
                //If not found, find a point close enough
                topRearPoint = topPoint + (-0.5f * hits[i].normal);
            }

            bottomPoint = frontPoint - new Vector3(0.0f, 0.2f, 0.0f);

            if (Physics.Raycast(bottomPoint, -hits[i].normal, out rearPointHit, 10.0f /*As opposed to infinity*/, climbingSurfaceLayer))
            {
                //Find a rear point on the surface 
                bottomRearPoint = rearPointHit.point;
            }
            else
            {
                //If not found, find a point close enough
                bottomRearPoint = bottomPoint + (-0.5f * hits[i].normal);
            }

            frontPoints.Add(frontPoint);
            topPoints.Add(topPoint);
            topRearPoints.Add(topRearPoint);
            bottomPoints.Add(bottomPoint);
            bottomRearPoints.Add(bottomRearPoint);
        }

        //Add all identified points to verticces
        vertices.AddRange(frontPoints);
        vertices.AddRange(topPoints);
        vertices.AddRange(topRearPoints);
        vertices.AddRange(bottomPoints);
        vertices.AddRange(bottomRearPoints);
        meshVertices = vertices.ToArray();


        //Debug.Log(frontPoints.Count);

        for (int i = 0; i < frontPoints.Count - 1; i++)
        {
            Debug.DrawLine(frontPoints[i], frontPoints[i + 1], Color.red, 3.0f);
            Debug.DrawLine(topPoints[i], topPoints[i + 1], Color.red, 3.0f);
            Debug.DrawLine(topRearPoints[i], topRearPoints[i + 1], Color.red, 3.0f);
            Debug.DrawLine(bottomPoints[i], bottomPoints[i + 1], Color.red, 3.0f);
            Debug.DrawLine(bottomRearPoints[i], bottomRearPoints[i + 1], Color.red, 3.0f);


        }

        for (int i = 0; i < frontPoints.Count - 1; i++)
        {
            //Identify points composing each triangle, for every inch
            frontRight = i + (frontPoints.Count * frontPointsArrayIndexDisplacement);//frontPoints[i];
            frontLeft = frontRight + 1;//frontPoints[i+1];
            topRight = i + (frontPoints.Count * topPointsArrayIndexDisplacement);//topPoints[i];
            topLeft = topRight + 1;//topPoints[i+1];
            bottomRight = i + (frontPoints.Count * bottomPointsArrayIndexDisplacement);//bottomPoints[i];
            bottomLeft = bottomRight + 1;//bottomPoints[i+1];
            topRearRight = i + (frontPoints.Count * topRearPointsArrayIndexDisplacement);//topRearPoints[i]; 
            topRearLeft = topRearRight + 1;//topRearPoints[i+1];
            bottomRearRight = i + (frontPoints.Count * bottomRearPointsArrayIndexDisplacement);//bottomRearPoints[i];
            bottomRearLeft = bottomRearRight + 1;// bottomRearPoints[i+1];

            if (frontRight >= meshVertices.Length ||
                 frontLeft >= meshVertices.Length ||
                 topRight >= meshVertices.Length ||
                 topLeft >= meshVertices.Length ||
                 bottomRight >= meshVertices.Length ||
                 bottomLeft >= meshVertices.Length ||
                 topRearRight >= meshVertices.Length ||
                 topRearLeft >= meshVertices.Length ||
                 bottomRearRight >= meshVertices.Length ||
                 bottomRearLeft >= meshVertices.Length)
            {
                Debug.Log(i + "," +
                    frontRight + "," +
                    frontLeft + "," +
                    topRight + "," +
                    topLeft + "," +
                    bottomRight + "," +
                    bottomLeft + "," +
                    topRearRight + "," +
                    topRearLeft + "," +
                    bottomRearRight + "," +
                    bottomRearLeft);
            }
            //Construct the mesh one triangle/vertex at a time

            //Top facing, right triangle
            triangles.Add(topRight); triangles.Add(topLeft); triangles.Add(topRearRight);
            //Top facing, left triangle
            triangles.Add(topLeft); triangles.Add(topRearLeft); triangles.Add(topRearRight);
            //Front facing, right triangle, for top half
            triangles.Add(topLeft); triangles.Add(topRight); triangles.Add(frontRight);
            //Front facing, left triangle, for top half
            triangles.Add(topLeft); triangles.Add(frontRight); triangles.Add(frontLeft);
            //Front facing, right triangle, for bottom half
            triangles.Add(frontLeft); triangles.Add(frontRight); triangles.Add(bottomRight);
            //Front facing, left triangle, for bottom half
            triangles.Add(frontLeft); triangles.Add(bottomRight); triangles.Add(bottomLeft);
            //Bottom facing, right triangle
            triangles.Add(bottomRight); triangles.Add(bottomLeft); triangles.Add(bottomRearRight);
            //Bottom facing, left triangle
            triangles.Add(bottomLeft); triangles.Add(bottomRearLeft); triangles.Add(bottomRearRight);

        }
        meshTriangles = triangles.ToArray();

        for (int i = 0; i < meshVertices.Length; i++)
        {
            uv.Add(new Vector2(0.0f, 0.0f));
        }

        meshUV = uv.ToArray();
        bridgeMesh.vertices = meshVertices;
        bridgeMesh.triangles = meshTriangles;
        bridgeMesh.uv = meshUV;
        bridgeMesh.RecalculateNormals();
        bridgeCollider.convex = true;
        //bridgeMesh.Optimize();

        bridgeCollider.sharedMesh = bridgeMesh;

    }

    void InstantiateBridge(RaycastHit p_hit)
    {
        //Push all points in between 0.5 units (min ledge size) in direction of normal
        GameObject bridgeNode = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bridgeNode.transform.position = p_hit.point;
        bridgeNode.transform.forward = p_hit.normal;
        bridgeNode.transform.localScale = new Vector3(0.5f, 0.1f, 0.5f);
    }

    void InstantiateNode(Vector3 p_position, Vector3 p_normal)
    {
        GameObject node;

        node = (GameObject)Instantiate(nodePrefab, p_position, Quaternion.identity);
        node.transform.forward = p_normal;
        node.transform.parent = climbingSurface.transform;
        node.GetComponent<NodeControl>().ledgeType = LedgeType.Plateau;//(LedgeType)System.Enum.ToObject(typeof(LedgeType), Random.Range(0, System.Enum.GetNames(typeof(LedgeType)).Length));
        //node.transform.localScale = Vector3.one;
        //Debug.Log(p_normal);
        //Debug.Log(node.transform.localScale);
    }

    int GetLayerFromLayerMask(LayerMask p_layerMask)
    {
        int layerNumber = 0;
        int layer = p_layerMask.value;
        while (layer > 0)
        {
            layer = layer >> 1;
            layerNumber++;
        }
        return layerNumber - 1;
    }
}