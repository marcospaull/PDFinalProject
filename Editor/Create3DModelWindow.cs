using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using itk.simple;
using g3;

public class Create3DModelWindow : EditorWindow
{
    //for GUI
    private string datasetFolder = "";
    private string modelName = "";
    private bool groupEnabled = true;
    
    // mesh reconstruction
    private double numCells = 256;
    private float edgeLengthMultiplier = 2f;
    private int remeshPasses = 20;
    private int reducedTriangleCount = 10000;

    // Code from assignment 
    [MenuItem("CS116A/Create3DModel")]
    public static void ShowWindow()
    {
        // Code from assignment 
        EditorWindow.GetWindow(typeof(Create3DModelWindow));
    }

    // Code from assignment  
    private void OnGUI()
    {
        GUILayout.Label("Settings", EditorStyles.boldLabel);

        if (GUILayout.Button("Browse a Folder"))
        {
            // Code from assignment  
            datasetFolder = EditorUtility.OpenFolderPanel("Select DICOM Directory", "", "");
            modelName = Path.GetFileName(datasetFolder);
        }

        datasetFolder = EditorGUILayout.TextField("Selected Folder:", datasetFolder);
        modelName = EditorGUILayout.TextField("Model Name:", modelName);

        groupEnabled = EditorGUILayout.BeginToggleGroup("Optional Settings", groupEnabled);
        numCells = EditorGUILayout.DoubleField("NumCells:", numCells);
        edgeLengthMultiplier = EditorGUILayout.FloatField("EdgeLengthMultiplier:", edgeLengthMultiplier);
        remeshPasses = EditorGUILayout.IntField("Smoothing Iteration:", remeshPasses);
        reducedTriangleCount = EditorGUILayout.IntField("ReducedTriangleCount:", reducedTriangleCount);
        EditorGUILayout.EndToggleGroup();

        if (GUILayout.Button("Reconstruct 3D Model"))
        {
            if (datasetFolder.Length > 0)
            {
                ReconstructModel();
            }
        }
    }

    // Code from assignment 
    private Image ResampleVolumeImage(Image _volumeImage)
    {
        // Code from assignment
        ResampleImageFilter resample = new ResampleImageFilter();

        // Code from assignment 
        resample.SetInterpolator(InterpolatorEnum.sitkLinear);

        // Code from assignment 
        resample.SetOutputDirection(_volumeImage.GetDirection());
        resample.SetOutputOrigin(_volumeImage.GetOrigin());

        // Code from assignment 
        VectorDouble newSpacing = new VectorDouble { 1.0, 1.0, 1.0 };
        resample.SetOutputSpacing(newSpacing);

        // Code from assignment 
        var originalSize = _volumeImage.GetSize();
        var originalSpacing = _volumeImage.GetSpacing();
        VectorUInt32 newSize = new VectorUInt32();

        for (int i = 0; i < _volumeImage.GetDimension(); i++)
        {
            newSize.Add((uint)Math.Ceiling(originalSize[i] * originalSpacing[i] / newSpacing[i]));
        }

        resample.SetSize(newSize);

        // Code from assignment instructions 
        resample.SetDefaultPixelValue(0.0);

        // Code from assignment instructions
        var volumeImage = resample.Execute(_volumeImage);

        return volumeImage;
    }

    //for volume data
    private class VolumeImplicit : BoundedImplicitFunction3d
    {
        public byte[] Data;
        public int Width, Height, Depth;

        public AxisAlignedBox3d Bounds()
        {
            return new AxisAlignedBox3d(0, 0, 0, Width, Height, Depth);
        }

        public double Value(ref Vector3d pt){
            int x = (int)pt.x;
            int y = (int)pt.y;
            int z = (int)pt.z;

            if (x < 0 || x >= Width || y < 0 || y >= Height || z < 0 || z >= Depth)
                return 1;

            int index = x + y * Width + z * Width * Height;
            return Data[index] > 0 ? -1 : 1;
        }
    }

    private void ReconstructModel()
    {
        Debug.Log("Starting reconstruction for: " + datasetFolder);

        // Read DICOM 
        ImageSeriesReader reader = new ImageSeriesReader();
        VectorString dicomFiles = ImageSeriesReader.GetGDCMSeriesFileNames(datasetFolder);
        reader.SetFileNames(dicomFiles);
        Image volumeImage = reader.Execute();

        Image resampledImage = ResampleVolumeImage(volumeImage);

        int width = (int)resampledImage.GetSize()[0];
        int height = (int)resampledImage.GetSize()[1];
        int depth = (int)resampledImage.GetSize()[2];

        Debug.Log("Size: " + width + " x " + height + " x " + depth);

        // Get pixel data
        IntPtr buffer = resampledImage.GetBufferAsUInt8();
        byte[] pixelData = new byte[width * height * depth];
        System.Runtime.InteropServices.Marshal.Copy(buffer, pixelData, 0, pixelData.Length);

        VolumeImplicit volImplicit = new VolumeImplicit();
        volImplicit.Data = pixelData;
        volImplicit.Width = width;
        volImplicit.Height = height;
        volImplicit.Depth = depth;

        // Marching Cubes
        MarchingCubes mc = new MarchingCubes();
        mc.Implicit = volImplicit;
        mc.Bounds = volImplicit.Bounds();
        mc.CubeSize = mc.Bounds.MaxDim / numCells;
        mc.IsoValue = 0;
        mc.Generate();

        DMesh3 mesh = mc.Mesh;
        Debug.Log("Vertices: " + mesh.VertexCount);

        // Remeshing
        Remesher remesher = new Remesher(mesh);
        remesher.SetTargetEdgeLength(edgeLengthMultiplier);
        remesher.SmoothSpeedT = 0.5;
        for (int i = 0; i < remeshPasses; i++)
        {
            remesher.BasicRemeshPass();
        }

        //Reduce triangles
        Reducer reducer = new Reducer(mesh);
        reducer.ReduceToTriangleCount(reducedTriangleCount);

        // Code from assignment instructions 
        MeshNormals.QuickCompute(mesh);
        var centroid = new Vector3d(width, height, depth) / 2;
        MeshTransforms.Translate(mesh, centroid * -1);
        MeshTransforms.FlipLeftRightCoordSystems(mesh);

        // Code from assignment instructions
        var savePath = $"{Application.dataPath}/{modelName}.obj";
        Debug.Log("Saving to: " + savePath);
        g3UnityUtils.WriteOutputMesh(mesh, savePath);

        AssetDatabase.Refresh();
        Debug.Log("Done!");
    }
}