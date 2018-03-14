using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Following tutorial: http://catlikecoding.com/unity/tutorials/procedural-grid/

public enum Orientation
{
    Left,
    Right
};

[RequireComponent (typeof(MeshFilter), typeof(MeshRenderer))]
public class GenerateMesh : MonoBehaviour {

    public float zInterval;
    public float xInterval;
    public Orientation orientation;

    private Vector3[] vertices;
    private Mesh mesh;
    private float roadLength;
    private float meshDepth;
    private float height;

    private void Awake()
    {
        Generate();
    }

    private void Generate()
    {
        roadLength = 50;
        meshDepth = 7;
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Grid";

        int vertexCount = 0;
        vertices = new Vector3[((Mathf.CeilToInt(roadLength / zInterval) + 1) * (Mathf.CeilToInt((int) meshDepth / xInterval) + 1))];
        for (float x = 0; x <= meshDepth; x += xInterval)
        {
            for (float z = 0; z <= roadLength; z += zInterval)
            {
                if (x == 0 || x == meshDepth || z == 0 || z == roadLength)
                {
                    height = 0;
                }
                else
                {
                    height = Random.value;
                }

                if (orientation == Orientation.Left)
                {
                    vertices[vertexCount] = new Vector3(-x, height, z);
                }
                else
                {
                    vertices[vertexCount] = new Vector3(x, height, z);
                }
                vertexCount += 1;
            }
        }
        mesh.vertices = vertices;
        int[] triangles = new int[Mathf.CeilToInt(roadLength / zInterval) * Mathf.CeilToInt((int)meshDepth / xInterval) * 6];

        for (int tri = 0, ver = 0, x = 0; x < Mathf.CeilToInt((int)meshDepth / xInterval); x += 1, ver += 1)
        {
            for(int z = 0; z < Mathf.CeilToInt(roadLength / zInterval); z += 1, tri += 6, ver += 1)
            {
                if (orientation == Orientation.Left)
                {
                    triangles[tri] = ver;
                    triangles[tri + 3] = triangles[tri + 2] = ver + 1;
                    triangles[tri + 4] = triangles[tri + 1] = ver + Mathf.CeilToInt(roadLength / zInterval) + 1;
                    triangles[tri + 5] = ver + Mathf.CeilToInt(roadLength / zInterval) + 2;
                }
                else
                {
                    triangles[tri] = ver + Mathf.CeilToInt(roadLength / zInterval) + 2;
                    triangles[tri + 3] = triangles[tri + 2] = ver + 1;
                    triangles[tri + 4] = triangles[tri + 1] = ver + Mathf.CeilToInt(roadLength / zInterval) + 1;
                    triangles[tri + 5] = ver;
                }
            }
        }

        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }
}
