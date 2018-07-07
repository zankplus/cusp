/** Copyright 2018 Clayton Cooper
 *	
 *	This file is part of CuSp.
 *
 *	gengen2 is free software: you can redistribute it and/or modify
 *	it under the terms of the GNU General Public License as published by
 *	the Free Software Foundation, either version 3 of the License, or
 *	(at your option) any later version.
 *
 *	gengen2 is distributed in the hope that it will be useful,
 *	but WITHOUT ANY WARRANTY; without even the implied warranty of
 *	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *	GNU General Public License for more details.
 *
 *	You should have received a copy of the GNU General Public License
 *	along with gengen2.  If not, see <http://www.gnu.org/licenses/>.
 *	
 *	This file also includes code written by Jasper Flick for Catlike
 *	Coding (https://catlikecoding.com). It is not, as far as I know,
 *	distributed under any particular license, and his website says 
 *	attribution is optional. Nevertheless, everything in this file is 
 *	his work, and he deserves credit for his excellent tutorials.
 *	Thanks, Jasper.
 * 
 */

using UnityEngine;
using System.Collections;

/**
 * A 3D structure made by projecting the 6 faces of a cubic surface onto a sphere using a mapping developed by Philip Nowell
 * (http://mathproofs.blogspot.com/2005/07/mapping-cube-to-sphere.html). The benefit of this mapping is that it divides the
 * surface of the sphere into a grid of squares of roughly equal size, with relatively little shape distortion. This grid
 * doesn't serve an irreplaceable purpose within the original scope of the project, but it does simplify the UV mapping 
 * for 
 * 
 * With the cube-sphere created, the program uses Perlin noise to create connecting heightmap textures for each of the 6
 * faces that are colored to depict landforms and bodies of water, suggesting the surface of a planet.
 */
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PerlinPlanet : MonoBehaviour
{
    // Mesh parameters
    [Range(2, 512)] public int resolution = 64;     // Number of quadrilaterals comprising each edge of each face of the cube-sphere
    private Mesh mesh;              // Mesh to be used by this class's GameObject
    private Vector3[] vertices;     // List of Mesh's vertices
    private Vector3[] normals;      // List of normals corresponding to each vertex
    private Vector2[] uvs;          // List of UV coordinates cooresponding to each vertex
    private Material[] materials;   // List of materials, 1 for each of the planet's 6 'faces'
    private Texture2D[] textures;   // List of textures corresponding to each material
    private const float radius = 5f;    // Planet's radius

    // Texture parameters
    public int textureResolution = 256;     // Width/height in pixels of each texture
    public Gradient coloring;               // Colors each texel according to 'elevation'

    // Object/display parameters
    public float axialTilt = -23f;          // Planet's Z-Axis rotation
    public float rotationalSpeed;           // Planet's current rotational speed
    public float targetRotationalSpeed = 5; // Planet's rotational speed tends toward this value
    public float maxRotationalSpeed = 500;  // Planet starts at this speed when created and tends toward it when destroyed
    public GameObject poles;                // North/south pole indicator
    private float startTime;                // Start time for timing start/reset animations
    private PlanetState planetMode;          // Planet's current status, either 'active' (normal state) or 'reset'
    
    // Perlin noise parameters
    public int seed;                // RNG seed for the current planet
    public NoiseMethodType noiseType;                   // Type of noise generation to use, Value or Perlin
    [Range(1, 8)]   public int octaves = 5;             // Number of successive wavelengths to add together
    [Range(1f, 4f)] public float lacunarity = 2f;       // Factor by which frequency scales with each successive octave
    [Range(0f, 1f)] public float persistence = 0.5f;    // Factor by which amplitude scales with each successive octave
    private float frequency;                            // Frequency of noise. Lower frequency means higher wavenlength and thus larger landforms
    private float xOffset, yOffset, zOffset;            // Virtual position of the planet in 3D space, as Perlin noise is determined by planet's initial 'position'

    /**
     * Creates a new Mesh and determines its vertices, normals, triangles, etc., then creates its initial textures.
     */
    private void Start()
    {
        // Create and define new Mesh
        GetComponent<MeshFilter>().mesh = mesh = new Mesh();
        mesh.name = "Procedural Sphere";
        CreateVertices();
        CreateTriangles();

        // Set initial rotational speed and axial tilt
        rotationalSpeed = maxRotationalSpeed;
        transform.rotation = Quaternion.Euler(0, 0, axialTilt);

        // Generate a fresh set of textures (terrain) for this planet
        SetPlanet();
    }

    /**
     * Rotates and scales the planet according to its current state.
     */
    private void Update()
    {
        // Time elapsed since planet state was changed
        float currentTime = Time.time - startTime;

        // If state is ACTIVE, its scale tends toward 1 and its rotational speed tends toward targetRotationalSpeed
        if (planetMode == PlanetState.ACTIVE)
        {
            if (transform.localScale.x < 1)
                transform.localScale = Vector3.one * Smooth(currentTime);

            if (rotationalSpeed != targetRotationalSpeed)
                rotationalSpeed = Mathf.Lerp(rotationalSpeed, targetRotationalSpeed, currentTime / 30);
        }

        // If state is RESTARTING, its scale tends toward 0 and its speed tends toward maxRotationalSpeed.
        // Once its scale drops below 0, we call SetPlanet to generate a new surface.
        else if (planetMode == PlanetState.RESTARTING)
        {
            float highSpeed = maxRotationalSpeed;
            if (rotationalSpeed < 0)
                highSpeed = -highSpeed;

            if (rotationalSpeed < maxRotationalSpeed)
            {
                rotationalSpeed = Mathf.Lerp(rotationalSpeed, highSpeed, currentTime / 30);
            }

            if (transform.localScale.x > 0)
            {
                transform.localScale = Vector3.one - Vector3.one * Smooth(currentTime);
            }
            else
                SetPlanet();
            
        }

        transform.Rotate(Vector3.up * -rotationalSpeed * Time.deltaTime);
    }

    /**
     * Generates a new surface for the planet and resets its state and timer
     */
    private void SetPlanet()
    {
        CreateTextures();
        startTime = Time.time;
        planetMode = PlanetState.ACTIVE;
    }

    /**
     * Sets a new seed for the planet and puts it into RESTARTING mode (which will shortly lead to a new surface being generated).
     * Parameters:
     *  int seed    The new seed to which to set the RNG
     */
    public void ResetPlanet(int seed)
    {
        this.seed = seed;
        startTime = Time.time;
        planetMode = PlanetState.RESTARTING;
    }

    /**
     * Creates a new 
     */
    public void CreateTextures()
    {
        // Randomly choose offsets from seed. These offset determine the virtual location from which noise is sampled and thus
        // uniquely define the surface generated.
        Random.InitState(seed);
        xOffset = Random.Range(0, 1000);
        yOffset = Random.Range(0, 1000);
        zOffset = Random.Range(0, 1000);


        // Create a new texture for each face of the cube-sphere, set its properties, and call FillTexture to draw its texture
        materials = GetComponent<MeshRenderer>().materials;
        textures = new Texture2D[6];
        for (int i = 0; i < 6; i++)
        {
            if (textures[i] == null)
            {
                textures[i] = new Texture2D(textureResolution, textureResolution, TextureFormat.RGB24, true);
                textures[i].name = "Procedural Texture";
                textures[i].wrapMode = TextureWrapMode.Clamp;
                textures[i].filterMode = FilterMode.Point;

                materials[i].SetFloat("_Glossiness", 0f);
                materials[i].mainTexture = textures[i]; // Assign this texture to the corresponding face of the cube-sphere
            }

            textures[i] = FillTexture(i);
        }
    }

    /**
     * Colors each pixel of the given texture
     * Parameters:
     *  int i   The index of the texture to be colored
     */ 
    public Texture2D FillTexture(int i)
    {
        Texture2D texture = textures[i];

        // Resize texture if necessary
        if (texture.width != textureResolution)
        {
            texture.Resize(textureResolution, textureResolution);
        }

        NoiseMethod method = Noise.noiseMethods[(int)noiseType][2];  // Always use 3D noise

        // Fill in each texel
        for (int y = 0; y < textureResolution; y++)
        {
            for (int x = 0; x < textureResolution; x++)
            {
                Vector3 point;
                float gridStep = 0.5f;

                // Textures are 2D squares, but they'll be projected onto the 3D faces of the cube-spheres. Each texel is colored according to the value of
                // the 3-dimensional Perlin noise function at the point on the sphere where it will be displayed.

                // Find the point on the sphere that corresponds to the current texel.
                if (i == 0)
                    point = CubeToSphere((float) (x + gridStep) * resolution / textureResolution, (float) (y + gridStep) * resolution / textureResolution, 0);
                else if (i == 1)
                    point = CubeToSphere(0, (float)(x + gridStep) * resolution / textureResolution, (float)(y + gridStep) * resolution / textureResolution);
                else if (i == 2)
                    point = CubeToSphere((float)(y + gridStep) * resolution / textureResolution, 0, (float)(x + gridStep) * resolution / textureResolution);
                else if (i == 3)
                    point = CubeToSphere((float)(y + gridStep) * resolution / textureResolution, (float)(x + gridStep) * resolution / textureResolution, resolution);
                else if (i == 4)
                    point = CubeToSphere(resolution, (float) (y + gridStep) * resolution / textureResolution, (float)(x + gridStep) * resolution / textureResolution);
                else
                    point = CubeToSphere((float)(x + gridStep) * resolution / textureResolution, resolution, (float)(y + gridStep) * resolution / textureResolution);

                // Apply the offsets to virtually translate the cube-sphere and obtain a unique sphere of noise
                point = new Vector3(point.x + xOffset, point.y + yOffset, point.z + zOffset);

                // Obtain a sample from the Noise function for this point
                if (noiseType == NoiseMethodType.Value)
                {
                    frequency = 1.75f;
                    lacunarity = 2f;
                    persistence = 0.5f;
                }
                else
                {
                    frequency = 1.6f;
                    lacunarity = 4f;
                    persistence = 0.25f;
                }

                    float sample = Noise.Sum(method, point, frequency, octaves, lacunarity, persistence);

                // Perlin noise must be shifted into the 0-1 range
                if (noiseType == NoiseMethodType.Perlin)
                {
                    sample = sample * 0.5f + 0.5f;
                }

                // Color the pixel according to the Perlin 
                texture.SetPixel(x, y, coloring.Evaluate(sample));
            }
        }

        // Apply and return texture
        texture.Apply();
        return texture;
    }

    /**
     * Creates the vertices of the cube-sphere mesh. This method has been modified from Catlike Coding's original version.
     * This method creates a square grid for each face of the cube-sphere as if they were flat squares, like the spaces of cubes,
     * but projects them onto the surface of a sphere to obtain the final position. Thus each face is divided into a regular
     * square grid. Unlike Catlike Coding's version, points along the seams between faces are represented twice, in order to
     * facilitate easy UV mapping.
     * 
     * Note that in called SetVertex, this method also creates the cube-sphere's UV maps.
     */
    public void CreateVertices()
    {
        vertices = new Vector3[(resolution + 1) * (resolution + 1) * 6];
        uvs = new Vector2[vertices.Length];
        normals = new Vector3[vertices.Length];

        int v = 0;

        // 0. z = 0
        for (int y = 0; y <= resolution; y++)
            for (int x = 0; x <= resolution; x++)
                SetVertex(v++, x, y, 0, 0);

        // 1. x = 0
        for (int z = 0; z <= resolution; z++)
            for (int y = 0; y <= resolution; y++)
                SetVertex(v++, 0, y, z, 1);

        // 2. y = 0
        for (int x = 0; x <= resolution; x++)
            for (int z = 0; z <= resolution; z++)
                SetVertex(v++, x, 0, z, 2);

        // 3. z = gridSize
        for (int x = 0; x <= resolution; x++)
            for (int y = 0; y <= resolution; y++)
                SetVertex(v++, x, y, resolution, 3);

        // 4. x = gridSize
        for (int y = 0; y <= resolution; y++)
            for (int z = 0; z <= resolution; z++)
                SetVertex(v++, resolution, y, z, 4);

        // 5. y = gridSize
        for (int z = 0; z <= resolution; z++)
            for (int x = 0; x <= resolution; x++)
                SetVertex(v++, x, resolution, z, 5);

        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uvs;
    }
    
    /**
     * Creates the triangles of the cube-sphere mesh. This method has been modified from Catlike Coding's original version.
     * Triangles are created two (six points) at a time, in pairs forming the individual quadrilaterals that make up the
     * cube-sphere's grid.
     */
    public void CreateTriangles()
    {
        int[] trianglesZmin = new int[(resolution * resolution) * 6];
        int[] trianglesZmax = new int[(resolution * resolution) * 6];
        int[] trianglesXmin = new int[(resolution * resolution) * 6];
        int[] trianglesXmax = new int[(resolution * resolution) * 6];
        int[] trianglesYmin = new int[(resolution * resolution) * 6];
        int[] trianglesYmax = new int[(resolution * resolution) * 6];

        int tZmin = 0, tZmax = 0, tXmin = 0, tXmax = 0, tYmin = 0, tYmax = 0, v = 0;

        // 0. z min
        for (int x = 0; x < resolution; x++, v++)
            for (int y = 0; y < resolution; y++, v++)
                tZmin = SetQuad(trianglesZmin, tZmin, v, v + 1, v + resolution + 1, v + resolution + 2);

        // 1. x min
        v += resolution + 1;
        for (int y = 0; y < resolution; y++, v++)
            for (int z = 0; z < resolution; z++, v++)
                tXmin = SetQuad(trianglesXmin, tXmin, v, v + 1, v + resolution + 1, v + resolution + 2);

        // 2. y min
        v += resolution + 1;
        for (int x = 0; x < resolution; x++, v++)
            for (int z = 0; z < resolution; z++, v++)
                tYmin = SetQuad(trianglesYmin, tYmin, v, v + 1, v + resolution + 1, v + resolution + 2);

        // 3. z max
        v += resolution + 1;
        for (int x = 0; x < resolution; x++, v++)
            for (int y = 0; y < resolution; y++, v++)
                tZmax = SetQuad(trianglesZmax, tZmax, v, v + 1, v + resolution + 1, v + resolution + 2);

        // 4. x max
        v += resolution + 1;
        for (int y = 0; y < resolution; y++, v++)
            for (int z = 0; z < resolution; z++, v++)
                tXmax = SetQuad(trianglesXmax, tXmax, v, v + 1, v + resolution + 1, v + resolution + 2);

        // 5. y max
        v += resolution + 1;
        for (int x = 0; x < resolution; x++, v++)
            for (int z = 0; z < resolution; z++, v++)
                tYmax = SetQuad(trianglesYmax, tYmax, v, v + 1, v + resolution + 1, v + resolution + 2);

        // Set submeshes
        mesh.triangles = trianglesZmin;
        mesh.subMeshCount = 6;
        mesh.SetTriangles(trianglesZmin, 0);
        mesh.SetTriangles(trianglesXmin, 1);
        mesh.SetTriangles(trianglesYmin, 2);
        mesh.SetTriangles(trianglesZmax, 3);
        mesh.SetTriangles(trianglesXmax, 4);
        mesh.SetTriangles(trianglesYmax, 5);
    }

    /**
     *  Projects a point in cubic space into spherical space, using the projection given by Philip Nowell.
     *  Parameters:
     *      float x     The x-coordinate of a point on a cube
     *      float y     The y-coordinate of a point on a cube
     *      float z     The z-coordinate of a point on a cube
     */
    public Vector3 CubeToSphere(float x, float y, float z)
    {
        Vector3 v = new Vector3(x, y, z) * 2f / resolution - Vector3.one;
        float x2 = v.x * v.x;
        float y2 = v.y * v.y;
        float z2 = v.z * v.z;

        Vector3 s;
        s.x = v.x * Mathf.Sqrt(1f - y2 / 2f - z2 / 2f + y2 * z2 / 3f);
        s.y = v.y * Mathf.Sqrt(1f - x2 / 2f - z2 / 2f + x2 * z2 / 3f);
        s.z = v.z * Mathf.Sqrt(1f - x2 / 2f - y2 / 2f + x2 * y2 / 3f);

        return s;
    }

    /**
     * Creates a vertex in the vertex list by projecting the given point from cubic to spherical space,
     * and sets its UV coordinate according to the face it's on.
     * Parameters:
     *  int i       The index of the current vertex
     *  int x       X-coordinate of the point on the cube to be projected
     *  int y       Y-coordinate of the point on the cube to be projected
     *  int z       Z-coordinate of the point on the cube to be projected
     *  int face    Number of the face 
     */
    private void SetVertex (int i, int x, int y, int z, int face)
    {
        normals[i] = CubeToSphere(x, y, z);
        vertices[i] = normals[i] * radius;

        // Set UVs
        // 0 = minZ, 1 = minX, 2 = minY, 3 = maxZ, 4 = maxX, 5 = maxY
        if (face == 0)
            uvs[i] = new Vector2((float) x / resolution, (float) y / resolution);
        else if (face == 1)
            uvs[i] = new Vector2((float) y / resolution, (float) z / resolution);
        else if (face == 2)
            uvs[i] = new Vector2((float) z / resolution, (float) x / resolution);
        else if (face == 3)
            uvs[i] = new Vector2((float) y / resolution, (float) x / resolution);
        else if (face == 4)
            uvs[i] = new Vector2((float) z / resolution, (float) y / resolution);
        else if (face == 5)
            uvs[i] = new Vector2((float) x / resolution, (float) z / resolution);
    }

    /**
     * Sets two triangles representing a quadrangle in the grid covering the face of the cube-sphere.
     * Parameters:
     *  int[] triangles A list of triangles
     *  int i           The index in triangles[] at which to start writing
     *  int v00         Bottom left corner of the quadrilateral
     *  int v10         Bottom right corner of the quadrilateral
     *  int v01         Top left corner of the quadrilateral
     *  int v11         Top right corner of the quadrilateral
     * Returns:
     *  The next empty index of triangles[]
     */
    private static int SetQuad (int[] triangles, int i, int v00, int v10, int v01, int v11)
    {
        triangles[i] = v00;
        triangles[i + 1] = triangles[i + 4] = v01;
        triangles[i + 2] = triangles[i + 3] = v10;
        triangles[i + 5] = v11;
        return i + 6;
    }

    /**
     * Outputs the value of the smoothing function (6t^5 - 15t^4 + 10t^3). Applied to 
     * time values in Update() to smooth scale changes.
     * Parameters:
     *  float t     A number, presumably between 0 and 1
     */
    private static float Smooth(float t)
    {
        return t * t * t * (t * (t * 6f - 15f) + 10f);
    }
}

public enum PlanetState { ACTIVE, RESTARTING };