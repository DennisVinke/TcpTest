using System;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Voxon
{
    public class Component : VoxieHelper
    {
        private Renderer rend;

        private bool isSkinnedMesh = false;

        // Mesh Structure Structure
        private Mesh Umesh;         // Objects mesh
        private Material[] Umaterials; // Object's Materials

        private poltex_t[] vt;   // List of vertices
        private int vtn;        // Number of vertices

        private int submesh_n = 0;      // Count of Submeshes part of mesh
        private int[][] tris;
        private int[] tri_n;

        private int draw_flags = 2 | 1 << 3; // 2 - Fill, and Draw from Texture buffer
        private int[] cols;

        // Texture
        tiletype[] textures;
        Texture2D[] reordered_textures;

        // Shader operations
        private ComputeShader cshader_main;
        private ComputeBuffer cbufferI_vertices;
        private ComputeBuffer cbufferI_uvs;
        private ComputeBuffer cbufferO_poltex;
        private Matrix4x4 _transform;

        // Use this for initialization
        void Start()
        {
            rend = GetComponent<Renderer>();
            if (!rend)
            {
                Debug.Log("(" + name + ") Failed to load Renderer");
                Destroy(this);
                return;
            }

            rend.enabled = true;

            if (GetComponent<SkinnedMeshRenderer>())
            {
                isSkinnedMesh = true;
                Umesh = new Mesh();

                // We currently take a single set of materials (may need to get mats per child in future).
                GetComponent<SkinnedMeshRenderer>().BakeMesh(Umesh);
                Umaterials = GetComponent<SkinnedMeshRenderer>().materials;

                GetComponent<SkinnedMeshRenderer>().updateWhenOffscreen = true;

            }
            else
            {
                Umesh = GetComponent<MeshFilter>().sharedMesh;
                Umaterials = GetComponent<MeshRenderer>().materials;
            }

            if (Umesh == null)
            {
                Debug.Log("(" + name + ") Mesh: FAILED TO LOAD");
                return;
            }

            if (Umaterials.Length != Umesh.subMeshCount)
            {
                Debug.Log("ERROR: " + name + " has mismatching materials and submesh count. These need to be equal! Submesh past material count will be assigned first material");
                return;
            }

            submesh_n = Umesh.subMeshCount;

            // Mesh Parameters
            vtn = Umesh.vertexCount;    // Number of Vertices
            vt = new poltex_t[vtn]; // Vertice Buffer

            tris = new int[submesh_n][];
            tri_n = new int[submesh_n];
            cols = new int[submesh_n];

            // Converting Triangles for Voxon Dll
            translate_triangles();

            // Build our compute shaders parameters
            build_shader();

            // Load textures
            load_textures();

        }

        // Use destroy to free gpu data
        void OnDestroy()
        {
            if (cbufferI_uvs != null)
            {
                cbufferI_uvs.Release();
            }
            if (cbufferI_vertices != null)
            {
                cbufferI_vertices.Release();
            }
            if (cbufferO_poltex != null)
            {
                cbufferO_poltex.Release();
            }
        }

        /// <summary>  
        ///  Draw the drawable mesh; Uses Capture Volume's transform to determine if play space has changed
        ///  Animated meshes are set to redraw every frame while statics only redrawn on them or the volume
        ///  changing transform.
        ///  </summary>
        public void DrawMesh(ref voxie_frame_t vf, Transform voxieCameraTransform)
        {
            if (isSkinnedMesh)
            {
                gameObject.GetComponent<SkinnedMeshRenderer>().BakeMesh(Umesh);
            }

            if (isSkinnedMesh || voxieCameraTransform.hasChanged || transform.hasChanged)
            {
                BuildMesh(voxieCameraTransform);
            }


            for (int mesh = 0; mesh < submesh_n; mesh++)
            {

                if (Umaterials[mesh].mainTexture)
                {
                    voxie_drawmeshtex(ref vf, ref textures[mesh], vt, vtn, tris[mesh], tri_n[mesh], draw_flags, cols[mesh]);
                }
                else
                {
                    voxie_drawmeshtex(ref vf, 0, vt, vtn, tris[mesh], tri_n[mesh], draw_flags, cols[mesh]);
                }
            }

        }

        /// <summary>  
        ///  Generates relevant transform for mesh type against capture volume transform
        ///  Passes to the Compute Shader for processing
        ///  </summary>
        private void BuildMesh(Transform voxieCameraTransform)
        {

            // Set Model View transform
            // Vector3 vert;
            Matrix4x4 Matrix;
            Matrix = transform.localToWorldMatrix;

            Matrix = voxieCameraTransform.worldToLocalMatrix * Matrix;
            Matrix = Matrix4x4.Scale(new Vector3(2.0f, 0.8f, 2.0f)) * Matrix;

            compute_transform(Matrix);

            transform.hasChanged = false;
        }

        /// <summary>  
        ///  Sets up the buffers and loads our compute shader
        ///  </summary>
        private void build_shader()
        {   
            if (!Resources.Load("VCS"))
                Debug.Log("Failed to load VCS");

            cshader_main = (ComputeShader)Resources.Load("VCS");


            /* OUTPUT Buffer */

            cbufferO_poltex = new ComputeBuffer(vtn, sizeof(float) * 5 + sizeof(int), ComputeBufferType.Default);
            cbufferO_poltex.SetData(vt);

            /* INPUT Buffers */

            // Out mesh may not have uvs; default to 0,0 to ensure we have values
            cbufferI_uvs = new ComputeBuffer(vtn, sizeof(float) * 2, ComputeBufferType.Default);
            List<Vector2> tmp_uvs = new List<Vector2>();
            Umesh.GetUVs(0, tmp_uvs);

            if (tmp_uvs.Count < Umesh.vertexCount)
            {
                tmp_uvs.AddRange(Enumerable.Repeat(Vector2.zero, Umesh.vertexCount - tmp_uvs.Count));
            }

            cbufferI_uvs.SetData(tmp_uvs.ToArray());

            // Do not assign mesh details here; we will load them per compute (as they could be rebaked)
            cbufferI_vertices = new ComputeBuffer(vtn, sizeof(float) * 3, ComputeBufferType.Default);
        }


        /// <summary>  
        ///  Compute Shader call. Set up Kernel, define tranform values and dispatches GPU threads
        ///  Currently only sends thin batches; should see to increase this in future.
        ///  </summary>
        private void compute_transform(Matrix4x4 Transform)
        {
            int kernelHandle = cshader_main.FindKernel("CSMain");

            cbufferI_vertices.SetData(Umesh.vertices);

            cshader_main.SetFloats("_transform_0", Transform.GetRow(0).x, Transform.GetRow(0).y, Transform.GetRow(0).z, Transform.GetRow(0).w);
            cshader_main.SetFloats("_transform_1", Transform.GetRow(1).x, Transform.GetRow(1).y, Transform.GetRow(1).z, Transform.GetRow(1).w);
            cshader_main.SetFloats("_transform_2", Transform.GetRow(2).x, Transform.GetRow(2).y, Transform.GetRow(2).z, Transform.GetRow(2).w);
            cshader_main.SetFloats("_transform_3", Transform.GetRow(3).x, Transform.GetRow(3).y, Transform.GetRow(3).z, Transform.GetRow(3).w);

            cshader_main.SetBuffer(kernelHandle, "in_vertices", cbufferI_vertices);
            cshader_main.SetBuffer(kernelHandle, "in_uvs", cbufferI_uvs);
            cshader_main.SetBuffer(kernelHandle, "output", cbufferO_poltex);
            int blocks = (vtn + 256 - 1) / 256;

            cshader_main.Dispatch(kernelHandle, blocks, 1, 1);

            cbufferO_poltex.GetData(vt);

        }


        private void translate_triangles()
        {
            for (int submesh = 0; submesh < submesh_n; submesh++)
            {
                /* Triangles are 3 idx and our array requires -1 delimiting, 
                /  So we need to make room for all tris (count) + a -1 at the end of each (count / 3)
                */

                tri_n[submesh] = Umesh.GetTriangles(submesh).Length + (Umesh.GetTriangles(submesh).Length / 3); // Number of Poly Indices
                if (Umesh.GetTriangles(submesh).Length % 3 != 0)
                {
                    Debug.Log("Error: Invalid triangle count. Expect factor of 3. tri_n = " + tri_n[submesh]);
                }

                tris[submesh] = new int[tri_n[submesh]];  // Poly Indices buffer

                // Set up indices
                int shift = 0;
                for (int i = 0; i < Umesh.GetTriangles(submesh).Length; i += 3)
                {
                    tris[submesh][i + 0 + shift] = Umesh.GetTriangles(submesh)[i + 0];
                    tris[submesh][i + 1 + shift] = Umesh.GetTriangles(submesh)[i + 1];
                    tris[submesh][i + 2 + shift] = Umesh.GetTriangles(submesh)[i + 2];

                    tris[submesh][i + 3 + shift] = -1;
                    shift++;
                }
                // Define Mesh Colour
                if (Umaterials.Length > submesh)
                {
                    cols[submesh] = System.Int32.Parse(ColorUtility.ToHtmlStringRGB(Umaterials[submesh].color), System.Globalization.NumberStyles.AllowHexSpecifier);
                }
                else
                {
                    cols[submesh] = cols[0];
                }

            }
        }

        private void load_textures()
        {
            textures = new tiletype[submesh_n];
            reordered_textures = new Texture2D[submesh_n];
            for (int submesh = 0; submesh < submesh_n; submesh++)
            {
                if (Umaterials[submesh].mainTexture)
                {
                    // We aren't getting the order we require so; 
                    // Load into the format we need (ARGB32)
                    reordered_textures[submesh] = new Texture2D((Umaterials[submesh].mainTexture as Texture2D).width, (Umaterials[submesh].mainTexture as Texture2D).height, TextureFormat.BGRA32, false);
                    Color32[] t_col = (Umaterials[submesh].mainTexture as Texture2D).GetPixels32();
                    reordered_textures[submesh].SetPixels32(t_col);

                    textures[submesh] = new tiletype();

                    textures[submesh].height = reordered_textures[submesh].height;
                    textures[submesh].width = reordered_textures[submesh].width;
                    textures[submesh].pitch = textures[submesh].width * Marshal.SizeOf(typeof(Color32));
                    textures[submesh].first_pixel = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(byte)) * reordered_textures[submesh].GetRawTextureData().Length);
                    Marshal.Copy(reordered_textures[submesh].GetRawTextureData(), 0, textures[submesh].first_pixel, reordered_textures[submesh].GetRawTextureData().Length);

                }
            }
        }

    }
}