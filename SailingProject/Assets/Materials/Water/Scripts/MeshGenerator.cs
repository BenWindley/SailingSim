using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    public Material waterMat;
    public Mesh mesh;

    [HideInInspector]
    public List<Vector3> vertices;
    [HideInInspector]
    public List<int> indices;

    public int segments = 5;
    public int rings = 2;

    [Range(0.05f, 1.0f)]
    public float radiusIncreaseOffset = 0.1f;

	// Use this for initialization
	void Awake ()
    {
        mesh = new Mesh();

        GetComponent<MeshFilter>().sharedMesh = mesh;

        UpdateMesh();
    }

    private void LoadMesh()
    {
        vertices.Clear();
        indices.Clear();

        // Iterate the radius of the next set of vertices, based on the length of the segment, to create 'squares' for each segment
        float radius = 0.0f;
        for (int i = 0; i < rings + 1; ++i)
        {
            CreateCirclePoints(radius);

            radius += 2 * Mathf.PI * radius / segments;
            radius += radiusIncreaseOffset;
        }
        // Create sets of indices to form triangles based on the number of segments
        for (int j = 0; j < rings; j++)
        {
            for (int i = 0; i < segments - 1; ++i)
            {
                indices.Add((j * segments) + i + 1);
                indices.Add((j * segments) + i);
                indices.Add((j * segments) + i + segments + 1);

                indices.Add((j * segments) + i);
                indices.Add((j * segments) + i + segments);
                indices.Add((j * segments) + i + segments + 1);
            }
            // create the final square connecting the beginning and end of the strip of triangles
            indices.Add((j * segments) + 0);
            indices.Add((j * segments) + segments - 1);
            indices.Add((j * segments) + segments);

            indices.Add((j * segments) + segments - 1);
            indices.Add((j * segments) + segments * 2 - 1);
            indices.Add((j * segments) + segments);
        }
    }

    private void CreateCirclePoints(float radius)
    {
        // Create a ring of points around a circle
        for(int i = 0; i < segments; ++i)
        {
            vertices.Add(new Vector3(
                radius * Mathf.Sin(i * Mathf.PI * 2 / segments), 
                transform.position.y, 
                radius * Mathf.Cos(i * Mathf.PI * 2/ segments)));
        }
    }

    private void CalculateUVs()
    {
        Bounds bounds = mesh.bounds;
        Vector3[] _vertices = mesh.vertices;

        Vector2[] uvs = new Vector2[_vertices.Length];
        for (int i = 0; i < _vertices.Length; i++)
        {
            uvs[i] = new Vector2(_vertices[i].x / bounds.size.x, _vertices[i].z / bounds.size.z);
        }

        mesh.uv = uvs;
    }

    public void UpdateMesh()
    {
        if (!mesh)
        {
            mesh = new Mesh();

            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        mesh.Clear();

        LoadMesh();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = indices.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.bounds = new Bounds(mesh.bounds.center, mesh.bounds.size / 2);
        CalculateUVs();
        
        GetComponent<MeshRenderer>().material = waterMat;
    }
}
