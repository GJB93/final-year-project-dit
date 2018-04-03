using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Following tutorial: http://catlikecoding.com/unity/tutorials/procedural-grid/

public enum Orientation
{
    Left,
    Right
};

[RequireComponent (typeof(MeshFilter), typeof(MeshRenderer))]
public class GenerateMesh : MonoBehaviour {

    private const int SAMPLE_SIZE = 2048;

    public float zInterval;
    public float xInterval;
    public Orientation orientation;
    public float risingVisualiserSmoothSpeed = 0.5f;
    public float fallingVisualiserSmoothSpeed = 0.5f;

    private Vector3[] vertices;
    private float[] eqBandPreviousY;
    private Mesh mesh;
    private Camera mainCamera;
    private float roadLength;
    private float meshDepth;
    private float height;
    private float previousY = 0;
    private float[] spectrum;
    private float[] samples;
    private AudioSource source;
    private List<float> bands;

    private void Awake()
    {
        source = Camera.main.GetComponent<AudioSource>();
        spectrum = new float[SAMPLE_SIZE];
        Generate();
        eqBandPreviousY = new float[(int)meshDepth];
        bands = new List<float>();
    }

    private void Update()
    {
        Mesh mesh = GetComponent<MeshFilter>().mesh;
        vertices = mesh.vertices;
        source.GetSpectrumData(spectrum, 0, FFTWindow.BlackmanHarris);
        bands = AudioAnalyser.GetBandAverages(spectrum, source.clip.frequency, SAMPLE_SIZE);
        int vertexCount = 0;
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
                    float risingInterpolater = 0;
                    float fallingInterpolator = 0;
                    float interpolation = 0;
                    float scaleY = bands.ElementAt(((int)meshDepth - 1) - (int)x) * 100;
                    risingInterpolater += Time.deltaTime * risingVisualiserSmoothSpeed;
                    fallingInterpolator += Time.deltaTime * fallingVisualiserSmoothSpeed;
                    interpolation = scaleY > previousY ? Mathf.Lerp(eqBandPreviousY[((int)meshDepth - 1) - (int)x], scaleY, risingInterpolater) : Mathf.Lerp(eqBandPreviousY[((int)meshDepth - 1) - (int)x], eqBandPreviousY[((int)meshDepth - 1) - (int)x] * 0.8f, fallingInterpolator);
                    height = interpolation;
                    eqBandPreviousY[((int)meshDepth - 1) - (int)x] = interpolation;
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
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private void Generate()
    {
        roadLength = 50;
        meshDepth = 8;
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
                    height = 0;
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
