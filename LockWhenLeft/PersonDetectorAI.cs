using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Dnn;
using Emgu.CV.Structure;
using Backend = Emgu.CV.Dnn.Backend;

namespace LockWhenLeft;

public class PersonDetectorAI : IPersonDetector
{
    #region Fields

    private const float NMS_THRESHOLD = 0.4f;
    private VideoCapture _capture;
    private bool _paused;
    private bool _running;
    private float confidenceTreshold = 0.5f;
    private bool isPersonDetected;
    private Net net;

    #endregion

    #region Constructor

    public PersonDetectorAI()
    {
        InitializeDetector();
        TryReinitializeCamera();
        _running = true;
    }

    #endregion

    #region Properties & Events

    public int Sensitivity { get; set; }
    public event Action PersonDetected;
    public event Action NoPersonDetected;
    public event Action NoPersonButChairDetected;
    public event Action<string> OnErrorOccurred;
    public event Action<Bitmap> NewFrameAvailable;

    public bool Paused
    {
        get => _paused;
        set
        {
            if (_paused == value)
                return;

            _paused = value;
        }
    }

    public bool ForceCameraFeed { get; set; }

    public float ConfidenceTreshold
    {
        get => (int)(confidenceTreshold * 100);
        set => confidenceTreshold = value / 100;
    }

    #endregion

    #region Public Methods

    public void Stop()
    {
        _running = false;
    }

    public void Start()
    {
        while (_running)
            try
            {
                if (_paused && !ForceCameraFeed)
                {
                    if (_capture != null)
                    {
                        _capture?.Dispose();
                        _capture = null;
                    }
                    Thread.Sleep(500);
                    continue;
                }

                if (_capture == null || !_capture.IsOpened)
                {
                    Debug.WriteLine("Camera not available (in use?). Retrying...");
                    OnErrorOccurred?.Invoke("Camera not available (in use?). Retrying...");
                    TryReinitializeCamera();
                    Thread.Sleep(250);
                    continue;
                }

                using (var frame = new Mat())
                {
                    Debug.WriteLine("Reading frame");
                    _capture.Read(frame);
                    if (frame == null || frame.IsEmpty)
                    {
                        Debug.WriteLine("Frame null or empty");
                        OnErrorOccurred?.Invoke("Empty frame received, retrying...");
                        Thread.Sleep(100);
                        continue;
                    }

                    try
                    {
                        var detections = DetectPersons(frame);
                        if (isPersonDetected) DrawDetections(frame, detections, "Person");

                        NewFrameAvailable?.Invoke(frame.ToBitmap());

                        if (isPersonDetected)
                        {
                            PersonDetected?.Invoke();
                        }
                        else
                        {
                            NoPersonDetected?.Invoke();
                        }
                    }
                    catch (Exception frameEx)
                    {
                        OnErrorOccurred?.Invoke($"Frame processing error: {frameEx.Message}");
                        Thread.Sleep(200);
                    }
                }

                Thread.Sleep(1000);
            }
            catch (Exception loopEx)
            {
                OnErrorOccurred?.Invoke($"Main loop exception: {loopEx.Message}");
                TryReinitializeCamera();
                Thread.Sleep(1000);
            }
    }

    #endregion

    #region Private Methods

    private void InitializeDetector()
    {
        try
        {
            var modelPath = "yolo.onnx";

            if (File.Exists(modelPath))
            {
                net = DnnInvoke.ReadNetFromONNX(modelPath);
                net.SetPreferableBackend(Backend.OpenCV);
                net.SetPreferableTarget(Target.Cpu);
            }
        }
        catch (Exception)
        {
        }
    }

    private void TryReinitializeCamera()
    {
        try
        {
            PersonDetected?.Invoke();
            _capture?.Dispose();
            _capture = new VideoCapture();
            _capture.Set(CapProp.FrameWidth, 640);
            _capture.Set(CapProp.FrameHeight, 480);
            if (!_capture.IsOpened)
            {
                _capture?.Dispose();
                _capture = null;
                PersonDetected?.Invoke();
                throw new Exception("Cannot open camera (in use by another app?)");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred?.Invoke($"Camera init failed: {ex.Message}");
            _capture = null;
        }
    }

    private List<Detection> DetectPersons(Mat frame)
    {
        Debug.WriteLine("Detecting persons");

        isPersonDetected = false;
        var allPersonDetections = new List<Detection>();

        var inputWidth = 640;
        var inputHeight = 640;

        var blob = DnnInvoke.BlobFromImage(frame, 1.0 / 255.0, new Size(inputWidth, inputHeight),
            new MCvScalar(0, 0, 0), true);

        net.SetInput(blob);

        var output = new Mat();
        net.Forward(output);

        var outputData = (float[,,])output.GetData();
        var dimensions = outputData.GetLength(1);
        var rows = outputData.GetLength(2);

        float xFactor = (float)frame.Width / inputWidth;
        float yFactor = (float)frame.Height / inputHeight;

        for (var i = 0; i < rows; i++)
        {
            float maxScore = 0;
            var classId = 0;

            for (var j = 4; j < dimensions; j++)
            {
                var score = outputData[0, j, i];
                if (score > maxScore)
                {
                    maxScore = score;
                    classId = j - 4;
                }
            }

            if ((classId == 0 || classId == 74) && maxScore > confidenceTreshold)
            {
                float centerX = outputData[0, 0, i] * xFactor;
                float centerY = outputData[0, 1, i] * yFactor;
                float width = outputData[0, 2, i] * xFactor;
                float height = outputData[0, 3, i] * xFactor;

                var x = centerX - width / 2;
                var y = centerY - height / 2;

                if (width > frame.Width / Sensitivity)
                    allPersonDetections.Add(new Detection
                    {
                        Box = new RectangleF(x, y, width, height),
                        Confidence = maxScore
                    });
            }
        }

        if (allPersonDetections.Count > 0)
        {
            var boxes = new Rectangle[allPersonDetections.Count];
            var confidences = new float[allPersonDetections.Count];

            for (var i = 0; i < allPersonDetections.Count; i++)
            {
                boxes[i] = new Rectangle((int)allPersonDetections[i].Box.X, (int)allPersonDetections[i].Box.Y,
                    (int)allPersonDetections[i].Box.Width, (int)allPersonDetections[i].Box.Height);
                confidences[i] = allPersonDetections[i].Confidence;
            }

            int[] indices;
            indices = DnnInvoke.NMSBoxes(boxes, confidences, confidenceTreshold, NMS_THRESHOLD);

            var finalDetections = new List<Detection>();
            if (indices != null)
                foreach (var idx in indices)
                    finalDetections.Add(allPersonDetections[idx]);

            allPersonDetections = finalDetections;
            isPersonDetected = finalDetections.Count > 0;
        }
        return allPersonDetections;
    }

    private void DrawDetections(Mat frame, List<Detection> detections, string description)
    {
        foreach (var detection in detections)
        {
            var box = new Rectangle(
                (int)detection.Box.X,
                (int)detection.Box.Y,
                (int)detection.Box.Width,
                (int)detection.Box.Height
            );

            CvInvoke.Rectangle(frame, box, new MCvScalar(0, 255, 0), 2);

            var label = $"{description}: {detection.Confidence * 100:F1}% ({box.Width}x{box.Height})";
            var baseline = 0;
            var labelSize = CvInvoke.GetTextSize(label, FontFace.HersheySimplex,
                0.5, 1, ref baseline);

            CvInvoke.Rectangle(frame,
                new Rectangle(box.X, box.Y - labelSize.Height - 10,
                    labelSize.Width, labelSize.Height + 10),
                new MCvScalar(0, 255, 0), -1);

            CvInvoke.PutText(frame, label,
                new Point(box.X, box.Y - 5),
                FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 0));
        }
    }

    #endregion

    #region Nested Types

    private class Detection
    {
        public RectangleF Box { get; set; }
        public float Confidence { get; set; }
    }

    #endregion
}