using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TextureGenerator : MonoBehaviour
{
    public ComputeShader encino_spectrum;
    private int kernel_spectrum_init;
    private int kernel_spectrum_update;
    public ComputeShader encino_IFFT;
    private int kernel_IFFT_X = 0;
    private int kernel_IFFT_Y = 1;
    public ComputeShader combination;
    private int kernel_build;
    public ComputeShader build;

    private ComputeBuffer initialVertexBuffer;
    private Vector3[] initalVertexArray;
    private ComputeBuffer vertexBuffer;
    private Vector3[] vertexArray;

    private RenderTexture H0;

    private RenderTexture H_dx;
    private RenderTexture H_dy;
    private RenderTexture H_dz;

    private RenderTexture initial;
    private Texture2D butterfly_texture;
    private RenderTexture ping_pong1;
    private RenderTexture final_dx;
    private RenderTexture final_dy;
    private RenderTexture final_dz;

    public RenderTexture displacement_map;
    public RenderTexture normal_map;

    public Vector3 displacementMag;

    public Material waterMat;

    public int gridResolution = 100;
    public int domainSize = 256;
    public float choppiness = 0.5f;

    public Mesh mesh;

    private void RunShader()
    {
        // Spectrum
        {
            kernel_spectrum_init = encino_spectrum.FindKernel("SpectrumInit");

            H0 = new RenderTexture(domainSize, domainSize, 0, RenderTextureFormat.ARGBFloat);
            H0.enableRandomWrite = true;
            H0.Create();
        }
        // Time Dependent Spectrum
        {
            kernel_spectrum_update = encino_spectrum.FindKernel("SpectrumUpdate");

            H_dy = new RenderTexture(domainSize, domainSize, 0, RenderTextureFormat.ARGBFloat);
            H_dy.enableRandomWrite = true;
            H_dy.Create();

            H_dx = new RenderTexture(domainSize, domainSize, 0, RenderTextureFormat.ARGBFloat);
            H_dx.enableRandomWrite = true;
            H_dx.Create();

            H_dz = new RenderTexture(domainSize, domainSize, 0, RenderTextureFormat.ARGBFloat);
            H_dz.enableRandomWrite = true;
            H_dz.Create();
        }
        // IFFT
        {             
            initial = new RenderTexture(domainSize, domainSize, 1);
            initial.enableRandomWrite = true;
            initial.format = RenderTextureFormat.ARGBFloat;
            initial.Create();
            
            ping_pong1 = new RenderTexture(domainSize, domainSize, 1);
            ping_pong1.enableRandomWrite = true;
            ping_pong1.format = RenderTextureFormat.ARGBFloat;
            ping_pong1.Create();

            final_dy = new RenderTexture(domainSize, domainSize, 1);
            final_dy.enableRandomWrite = true;
            final_dy.format = RenderTextureFormat.RFloat;
            final_dy.Create();

            final_dx = new RenderTexture(domainSize, domainSize, 1);
            final_dx.enableRandomWrite = true;
            final_dx.format = RenderTextureFormat.RFloat;
            final_dx.Create();

            final_dz = new RenderTexture(domainSize, domainSize, 1);
            final_dz.enableRandomWrite = true;
            final_dz.format = RenderTextureFormat.RFloat;
            final_dz.Create();

            {
                int log2Size = Mathf.RoundToInt(Mathf.Log(domainSize, 2));

                Vector2[] butterfly_data = new Vector2[domainSize * log2Size];

                int offset_x = 1;
                int num_terations = domainSize >> 1;

                for (int row_index = 0; row_index < log2Size; ++row_index)
                {
                    int offset_y = row_index * domainSize;

                    // Weights
                    {
                        int start = 0;
                        int end = 2 * offset_x;

                        for (int i = 0; i < num_terations; i++)
                        {
                            float j = 0.0f;

                            for (int k = 0; k < end; k += 2)
                            {
                                float phase = 2.0f * Mathf.PI * j * num_terations / domainSize;
                                float phase_cos = Mathf.Cos(phase);
                                float phase_sin = Mathf.Sin(phase);

                                butterfly_data[offset_y + k / 2].x = phase_cos;
                                butterfly_data[offset_y + k / 2].y = -phase_sin;

                                butterfly_data[offset_y + k / 2 + offset_x].x = -phase_cos;
                                butterfly_data[offset_y + k / 2 + offset_x].y = phase_sin;

                                j += 1.0f;
                            }
                            start += 4 * offset_x;
                            end = start + 2 * offset_x;
                        }
                    }

                    num_terations >>= 1;
                    offset_x <<= 1;
                }

                byte[] butterflyBytes = new byte[butterfly_data.Length * sizeof(ushort) * 2];
                for (uint i = 0; i < butterfly_data.Length; i++)
                {
                    uint byteOffset = i * sizeof(ushort) * 2;
                    HalfHelper.SingleToHalf(butterfly_data[i].x, butterflyBytes, byteOffset);
                    HalfHelper.SingleToHalf(butterfly_data[i].y, butterflyBytes, byteOffset + sizeof(ushort));
                }

                butterfly_texture = new Texture2D(domainSize, log2Size, TextureFormat.RGHalf, false);
                butterfly_texture.filterMode = FilterMode.Point;
                butterfly_texture.LoadRawTextureData(butterflyBytes);
                butterfly_texture.Apply(false, true);
            }

            // Kernel offset
            {
                int baseLog2Size = Mathf.RoundToInt(Mathf.Log(domainSize, 2));
                int log2Size = Mathf.RoundToInt(Mathf.Log(domainSize, 2));
                kernel_IFFT_X = (log2Size - baseLog2Size) * 2;
                kernel_IFFT_Y = kernel_IFFT_X + 1;
            }

            displacement_map = new RenderTexture(domainSize, domainSize, 1);
            displacement_map.name = "Displacement Map";
            displacement_map.enableRandomWrite = true;
            displacement_map.format = RenderTextureFormat.ARGBFloat;
            displacement_map.filterMode = FilterMode.Trilinear;
            displacement_map.antiAliasing = 8;
            displacement_map.wrapMode = TextureWrapMode.Repeat;
            displacement_map.Create();

            normal_map = new RenderTexture(domainSize, domainSize, 1);
            normal_map.name = "Normal Map";
            normal_map.enableRandomWrite = true;
            normal_map.format = RenderTextureFormat.ARGBFloat;
            normal_map.filterMode = FilterMode.Trilinear;
            normal_map.antiAliasing = 8;
            normal_map.wrapMode = TextureWrapMode.Repeat;
            normal_map.Create();
        }
        // Spectrum
        {
            encino_spectrum.SetInt("domainSize", domainSize);
            encino_spectrum.SetFloat("gravity", 9.81f);
            encino_spectrum.SetFloats("windDirection", 1, 1);
            encino_spectrum.SetFloat("windSpeed", 5.0f);

            encino_spectrum.SetTexture(kernel_spectrum_init, "outputH0", H0);

            encino_spectrum.Dispatch(kernel_spectrum_init, domainSize / 8, domainSize / 8, 1);
        }
        // Build Initial Plane
        {
            kernel_build = build.FindKernel("Build");

            var vert = new Vector3[(gridResolution + 1) * (gridResolution + 1)];
            var uvs = new Vector2[vert.Length];

            for (int i = 0, x = 0; x <= gridResolution; ++x)
            {
                for(int y = 0; y <= gridResolution; ++y, ++i)
                {
                    vert[i] = new Vector3(x / (float)gridResolution - 0.5f, 0, y / (float)gridResolution - 0.5f);
                    uvs[i] = new Vector2(x / (float)gridResolution, y / (float)gridResolution);
                }
            }

            var tris = new int[gridResolution * gridResolution * 6];

            for(int x = 0, t = 0, v = 0; x < gridResolution; ++x)
            {
                for(int y = 0; y < gridResolution; ++y)
                {
                    tris[t] = v;
                    tris[t + 3] = tris[t + 1] = v + 1;
                    tris[t + 5] = tris[t + 2] = v + gridResolution + 1;
                    tris[t + 4] = v + gridResolution + 2;

                    ++v;
                    t += 6;
                }
                ++v;
            }

            mesh = new Mesh();

            mesh.vertices = vert;
            mesh.uv = uvs;
            mesh.triangles = tris;
            mesh.MarkDynamic();
            mesh.RecalculateBounds();

            vertexArray = new Vector3[vert.Length];

            GetComponent<MeshFilter>().sharedMesh = mesh;

            vertexBuffer = new ComputeBuffer(vert.Length, sizeof(float) * 3);
            initialVertexBuffer = new ComputeBuffer(vert.Length, sizeof(float) * 3);

            initalVertexArray = vert;

            vertexBuffer.SetData(initalVertexArray);
            initialVertexBuffer.SetData(initalVertexArray);

            build.SetBuffer(kernel_build, "initialVertexBuffer", initialVertexBuffer);
            build.SetBuffer(kernel_build, "vertexBuffer", vertexBuffer);
            build.SetTexture(kernel_build, "displacement", displacement_map);
        }
    }

    private void UpdateShader()
    {
        // Time Dependent Spectrum
        {
            encino_spectrum.SetInt("domainSize", domainSize);
            encino_spectrum.SetFloat("gravity", 9.81f);
            encino_spectrum.SetFloats("windDirection", 1, 1);
            encino_spectrum.SetFloat("windSpeed", 5.0f);
            encino_spectrum.SetFloat("time", Time.time);

            encino_spectrum.SetTexture(kernel_spectrum_update, "inputH0", H0);
            encino_spectrum.SetTexture(kernel_spectrum_update, "outputDy", H_dy);
            encino_spectrum.SetTexture(kernel_spectrum_update, "outputDx", H_dx);
            encino_spectrum.SetTexture(kernel_spectrum_update, "outputDz", H_dz);

            encino_spectrum.Dispatch(kernel_spectrum_update, domainSize / 8, domainSize / 8, 1);
        }
        // IFFT dy
        {
            {
                Graphics.CopyTexture(H_dy, initial);

                encino_IFFT.SetTexture(kernel_IFFT_X, "input", initial);
                encino_IFFT.SetTexture(kernel_IFFT_X, "inputButterfly", butterfly_texture);
                encino_IFFT.SetTexture(kernel_IFFT_X, "output", ping_pong1);
                encino_IFFT.Dispatch(kernel_IFFT_X, 1, domainSize, 1);
                encino_IFFT.SetTexture(kernel_IFFT_Y, "input", ping_pong1);
                encino_IFFT.SetTexture(kernel_IFFT_Y, "inputButterfly", butterfly_texture);
                encino_IFFT.SetTexture(kernel_IFFT_Y, "output", final_dy);
                encino_IFFT.Dispatch(kernel_IFFT_Y, domainSize, 1, 1);
            }
            {
                Graphics.CopyTexture(H_dx, initial);

                encino_IFFT.SetTexture(kernel_IFFT_X, "input", initial);
                encino_IFFT.SetTexture(kernel_IFFT_X, "inputButterfly", butterfly_texture);
                encino_IFFT.SetTexture(kernel_IFFT_X, "output", ping_pong1);
                encino_IFFT.Dispatch(kernel_IFFT_X, 1, domainSize, 1);
                encino_IFFT.SetTexture(kernel_IFFT_Y, "input", ping_pong1);
                encino_IFFT.SetTexture(kernel_IFFT_Y, "inputButterfly", butterfly_texture);
                encino_IFFT.SetTexture(kernel_IFFT_Y, "output", final_dx);
                encino_IFFT.Dispatch(kernel_IFFT_Y, domainSize, 1, 1);
            }
            {
                Graphics.CopyTexture(H_dz, initial);

                encino_IFFT.SetTexture(kernel_IFFT_X, "input", initial);
                encino_IFFT.SetTexture(kernel_IFFT_X, "inputButterfly", butterfly_texture);
                encino_IFFT.SetTexture(kernel_IFFT_X, "output", ping_pong1);
                encino_IFFT.Dispatch(kernel_IFFT_X, 1, domainSize, 1);
                encino_IFFT.SetTexture(kernel_IFFT_Y, "input", ping_pong1);
                encino_IFFT.SetTexture(kernel_IFFT_Y, "inputButterfly", butterfly_texture);
                encino_IFFT.SetTexture(kernel_IFFT_Y, "output", final_dz);
                encino_IFFT.Dispatch(kernel_IFFT_Y, domainSize, 1, 1);
            }
            {
                int kernelHandle = combination.FindKernel("CSMain");

                combination.SetTexture(kernelHandle, "final_dy", final_dy);
                combination.SetTexture(kernelHandle, "final_dx", final_dx);
                combination.SetTexture(kernelHandle, "final_dz", final_dz);

                combination.SetTexture(kernelHandle, "displacement", displacement_map);
                combination.SetTexture(kernelHandle, "normal", normal_map);

                combination.SetInt("domainSize", domainSize);
                combination.SetFloat("choppiness", choppiness);

                combination.Dispatch(kernelHandle, domainSize / 8, domainSize / 8, 1);
            }
        }
    }

    private void Awake()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;

        RunShader();
    }

    private void Update()
    {
        UpdateShader();
        
        waterMat.SetTexture("_DisplacementMap", displacement_map);
        waterMat.SetTexture("_NormalMap", normal_map);

        build.SetVector("displacementMagnitude", displacementMag);

        build.Dispatch(kernel_build, Mathf.CeilToInt(100 / 8.0f), Mathf.CeilToInt(100 / 8.0f), 1);

        vertexBuffer.GetData(vertexArray);
        mesh.vertices = vertexArray;

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.MarkModified();
        mesh.RecalculateBounds();
    }
}
