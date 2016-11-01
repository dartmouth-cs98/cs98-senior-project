using UnityEngine;
using System.Runtime.InteropServices;
using System;
using System.Drawing;
using System.IO;
using Windows.Kinect;

public class TestDLL : MonoBehaviour
{
    //Constants
    static readonly int MAX_IMG_BYTES = 10000;

    //Read image from Kinect
    public int ColorWidth { get; private set; }
    public int ColorHeight { get; private set; }
    private KinectSensor _Sensor;
    private ColorFrameReader _Reader;
    private Texture2D _Texture;
    private byte[] _Data;

    // The imported function
#if UNITY_STANDALONE_WIN
    [DllImport("OpenCVUnity", EntryPoint = "TestSort")]
      public static extern void TestSort(int[] a, int length);
      [DllImport("OpenCVUnity", EntryPoint = "OpenCVFunc")]
      public static extern IntPtr OpenCVFunc();
      [DllImport("OpenCVUnity", EntryPoint = "NumHolds")]
      public static extern int NumHolds();
      #endif

    // Game objects
    public GameObject[] handHolds;
    public GameObject Handhold;
    public Camera mainCam;

    // Class variables
    // private static int scalingFactor = 150;
    // private static int leftShift = 5;
    // private static int downShift = 5;
    // private static float imgX = 2448;
    // private static float imgY = 3264;
    private static float imgX = 100;
    private static float imgY = 100;
    private Boolean first = true;
    private Image testFrame;

    public int segments = 10;
    LineRenderer line;

    private static int cameraSize = 5;

    // TODO: Restyle according to C# standards.
    void Start () { 
        int numHolds = 0;
        int[] boundingBoxArray;
        if (!climbSystemEnv.isWindows())
        {
            //Untested code.
            //http://stackoverflow.com/questions/29171151/passing-a-byte-array-from-unity-c-sharp-into-a-c-library-method
            Image frame = Image.FromFile("pathToImage/img.png");
            byte[] imgData = imageToByteArray(frame);

            IntPtr unmanagedArray = Marshal.AllocHGlobal(MAX_IMG_BYTES);
            Marshal.Copy(imgData, 0, unmanagedArray, MAX_IMG_BYTES);
            //End untested.

            IntPtr bb = OpenCVFunc();
            numHolds = NumHolds();
            boundingBoxArray = new int[numHolds * 4];
            Marshal.Copy(bb, boundingBoxArray, 0, numHolds * 4);
        }
        else
        {
            _Sensor = KinectSensor.GetDefault();

            if (_Sensor != null)
            {
                print("Acquired sensor");
                _Reader = _Sensor.ColorFrameSource.OpenReader();

                var frameDesc = _Sensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Rgba);
                ColorWidth = frameDesc.Width;
                ColorHeight = frameDesc.Height;

                _Texture = new Texture2D(frameDesc.Width, frameDesc.Height, TextureFormat.RGBA32, false);
                _Data = new byte[frameDesc.BytesPerPixel * frameDesc.LengthInPixels];

                if (!_Sensor.IsOpen)
                {
                    print("Sensor is not open");
                    _Sensor.Open();
                }
            }
            else
            {
                Debug.Log("Using image");
            }

            boundingBoxArray = new int[] { 50, 50, 10, 20, 90, 90, 20, 10 };
            numHolds = boundingBoxArray.Length/4;
        }

        this.handHolds = new GameObject[numHolds];

        // Adjust camera zoom.
        this.mainCam.orthographicSize = cameraSize / 2f;

        // Instantiate handholds.
        for (int i = 0; i < numHolds; i++)
        {
            int holdIndex = i * 4;
            float x = boundingBoxArray[holdIndex] / imgX * cameraSize - cameraSize / 2f;
            float y = boundingBoxArray[holdIndex + 1] / imgY * cameraSize - cameraSize / 2f;
            float width = boundingBoxArray[holdIndex + 2] / (imgX * 2.0f) * cameraSize;
            float height = boundingBoxArray[holdIndex + 3] / (imgY * 2f) * cameraSize;

            // Create handhold object.
            this.handHolds[i] = GameObject.Instantiate(Handhold);
            line = this.handHolds[i].GetComponent<LineRenderer>();

            line.SetVertexCount(segments + 1);
            line.useWorldSpace = false;
            CreatePoints(width, height);


            // transform handholds to be.
            this.handHolds[i].transform.localPosition =
                new Vector2(x + width,
                            (y + height) * -1f);
        }
        print("done");
    }

    void CreatePoints(float xradius, float yradius)
    {
        segments = 10;
        float x;
        float y;
        float z = 0f;

        float angle = 20f;

        for (int i = 0; i < (segments + 1); i++)
        {
            x = Mathf.Sin(Mathf.Deg2Rad * angle) * xradius;
            y = Mathf.Cos(Mathf.Deg2Rad * angle) * yradius;

            line.SetPosition(i, new Vector3(x, y, z));

            angle += (360f / segments);
            print(x + " " + y + " " + i + " " + segments);
        }
    }

    void Update () {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            
            if (frame != null)
            {
                frame.CopyConvertedFrameDataToArray(_Data, ColorImageFormat.Rgba);
                _Texture.LoadRawTextureData(_Data);
                _Texture.Apply();

                // Call OpenCV Plugin.
                // http://stackoverflow.com/questions/10894836/c-sharp-convert-image-formats-to-jpg.
                // Test code.
                if (this.first)
                {
                    //this.testFrame = (Bitmap)((new ImageConverter()).ConvertFrom(_Data));
                    //x.Save("c:\\frame.Jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    this.first = false;
                }
                // End test code.

                frame.Dispose();
                frame = null;
            }
        }
        else
        {
            Debug.Log("Using image");
        }
    }

    private byte[] imageToByteArray(Image imageIn)
    {
        MemoryStream ms = new MemoryStream();
        imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
        return ms.ToArray();
    }

    public Image byteArrayToImage(byte[] byteArrayIn)
    {
        MemoryStream ms = new MemoryStream(byteArrayIn);
        Image returnImage = Image.FromStream(ms);
        return returnImage;
    }

    void OnApplicationQuit()
    {
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }

        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }
}
