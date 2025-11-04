using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq; // CHANGED: Added for .First() and .ToArray()
using System.Threading;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Dnn;
using Emgu.CV.Structure;
// CHANGED: Add OnnxRuntime usings
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

// CHANGED: Renamed class to reflect new implementation
namespace LockWhenLeft;

public class PersonDetectorRfDetr : IPersonDetector
{
    #region Fields

    private const string MODEL_FILENAME = "rf-detr-nano.onnx";
    private const float NMS_THRESHOLD = 0.4f;
    private VideoCapture _capture;
    private bool _paused;
    private bool _running;
    private float confidenceTreshold = 0.5f;
    private bool isPersonDetected;
    private bool isChairDetected;

    // CHANGED: Replaced 'Net net' with 'InferenceSession'
    private InferenceSession _inferenceSession;
    private string[] _inputNames;
    private string[] _outputNames;

    #endregion

    #region Constructor

    public PersonDetectorRfDetr()
    {
        InitializeDetector();
        TryReinitializeCamera();
        _running = true;
    }
    private void TryReinitializeCamera()
    {
        try
        {
            Debug.WriteLine($"{DateTime.Now} Camera not available TryReinitializeCamera");
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
        get => (int) (confidenceTreshold * 100);
        set => confidenceTreshold = value / 100;
    }

    #endregion

    #region Public Methods

    public void Stop()
    {
        _running = false;
        // CHANGED: Dispose the session when stopping
        // _inferenceSession?.Dispose();
    }

    public void Start()
    {
        try
        {
            while (_running)
            {
                var swElaped = Stopwatch.StartNew();
                try
                {
                    if (_paused && !ForceCameraFeed)
                    {
                        if (_capture != null)
                        {
                            _capture?.Dispose();
                            _capture = null;
                        }

                        Thread.Sleep(250);
                        continue;
                    }

                    if (_capture == null || !_capture.IsOpened)
                    {
                        Debug.WriteLine($"{DateTime.Now} Camera not available (in use?). Retrying...");
                        OnErrorOccurred?.Invoke("Camera not available (in use?). Retrying...");
                        TryReinitializeCamera();
                        if (_capture == null || !_capture.IsOpened)
                            Thread.Sleep(100);
                        continue;
                    }

                    using (var frame = new Mat())
                    {
                        Debug.WriteLine($"{DateTime.Now} Reading frame");
                        _capture.Read(frame);
                        if (frame == null || frame.IsEmpty)
                        {
                            Debug.WriteLine($"{DateTime.Now} Frame null or empty");
                            OnErrorOccurred?.Invoke("Empty frame received, retrying...");
                            Thread.Sleep(100);
                            continue;
                        }

                        // BRIGHTNESS CHECK
                        using (var grayFrame = new Mat())
                        {
                            CvInvoke.CvtColor(frame, grayFrame, ColorConversion.Bgr2Gray);
                            var meanBrightness = CvInvoke.Mean(grayFrame).V0;

                            if (meanBrightness < 30)
                            {
                                if (isPersonDetected)
                                {
                                    isPersonDetected = false;
                                    NoPersonDetected?.Invoke();
                                }

                                NewFrameAvailable?.Invoke(frame.ToBitmap());
                                Thread.Sleep(100);
                                continue;
                            }
                        }

                        try
                        {
                            var sw = Stopwatch.StartNew();
                            var detections = DetectPersons(frame);
                            sw.Stop();
                            Debug.WriteLine($"Detection took {sw.ElapsedMilliseconds}");
                            if (detections.Any()) DrawDetections(frame, detections);

                            NewFrameAvailable?.Invoke(frame.ToBitmap());
                            isPersonDetected = detections.Any(d => d.ClassId == 1);
                            isChairDetected = detections.Any(d => d.ClassId == 62);

                            if (isPersonDetected)
                            {
                                PersonDetected?.Invoke();
                            }
                            else if (isChairDetected)
                            {
                                NoPersonButChairDetected?.Invoke();
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

                    swElaped.Stop();
                    // var sleepTime = (int) System.Math.Max(1, 200 - swElaped.ElapsedMilliseconds);
                    Thread.Sleep(250); // Wait between frames
                }
                catch (Exception loopEx)
                {
                    OnErrorOccurred?.Invoke($"Main loop exception: {loopEx.Message}");
                    TryReinitializeCamera();
                    Thread.Sleep(500);
                }
                finally
                {
                    swElaped.Restart();
                }
            }
        }
        finally
        {
            _inferenceSession?.Dispose();
            _capture?.Dispose();
        }
    }

    private void DrawDetections(Mat frame, List<Detection> detections)
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

            var label = $"{detection.ClassName}: {detection.Confidence * 100:F1}% ({box.Width}x{box.Height})";
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

    #region Private Methods

    private readonly Dictionary<int, string> _classNames = new Dictionary<int, string>
    {
        { 1, "person" }, { 2, "bicycle" }, { 3, "car" }, { 4, "motorcycle" }, { 5, "airplane" },
        { 6, "bus" }, { 7, "train" }, { 8, "truck" }, { 9, "boat" }, { 10, "traffic light" },
        { 11, "fire hydrant" }, { 13, "stop sign" }, { 14, "parking meter" }, { 15, "bench" },
        { 16, "bird" }, { 17, "cat" }, { 18, "dog" }, { 19, "horse" }, { 20, "sheep" },
        { 21, "cow" }, { 22, "elephant" }, { 23, "bear" }, { 24, "zebra" }, { 25, "giraffe" },
        { 27, "backpack" }, { 28, "umbrella" }, { 31, "handbag" }, { 32, "tie" }, { 33, "suitcase" },
        { 34, "frisbee" }, { 35, "skis" }, { 36, "snowboard" }, { 37, "sports ball" }, { 38, "kite" },
        { 39, "baseball bat" }, { 40, "baseball glove" }, { 41, "skateboard" }, { 42, "surfboard" },
        { 43, "tennis racket" }, { 44, "bottle" }, { 46, "wine glass" }, { 47, "cup" }, { 48, "fork" },
        { 49, "knife" }, { 50, "spoon" }, { 51, "bowl" }, { 52, "banana" }, { 53, "apple" },
        { 54, "sandwich" }, { 55, "orange" }, { 56, "broccoli" }, { 57, "carrot" }, { 58, "hot dog" },
        { 59, "pizza" }, { 60, "donut" }, { 61, "cake" }, { 62, "chair" }, { 63, "couch" },
        { 64, "potted plant" }, { 65, "bed" }, { 67, "dining table" }, { 70, "toilet" }, { 72, "tv" },
        { 73, "laptop" }, { 74, "mouse" }, { 75, "remote" }, { 76, "keyboard" }, { 77, "cell phone" },
        { 78, "microwave" }, { 79, "oven" }, { 80, "toaster" }, { 81, "sink" }, { 82, "refrigerator" },
        { 84, "book" }, { 85, "clock" }, { 86, "vase" }, { 87, "scissors" }, { 88, "teddy bear" },
        { 89, "hair drier" }, { 90, "toothbrush" }
    };

    // ######################################################################
// CHANGED: This entire method is rewritten for OnnxRuntime
// AND dynamic metadata reading
// ######################################################################
    private void InitializeDetector()
    {
        try
        {
            var path = new FileInfo(Environment.ProcessPath).DirectoryName;
            var modelPath = Path.Combine(path, MODEL_FILENAME);

            if (File.Exists(modelPath))
            {
                var sessionOptions = new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                    IntraOpNumThreads = 2
                };

                _inferenceSession = new InferenceSession(modelPath, sessionOptions);

                // CHANGED: Dynamically get the *first* input name
                _inputNames = new string[] {_inferenceSession.InputMetadata.Keys.First()};
                // _inputNames = new string[] { "input"};

                // CHANGED: Dynamically get *all* output names
                // This makes the code more robust, but we must know the order.
                // For DETR models, the order is typically [boxes, logits]
                _outputNames = _inferenceSession.OutputMetadata.Keys.ToArray();
                // _outputNames = new string[] { "pred_boxes", "pred_logits" };

                // Sanity check
                if (_outputNames.Length < 2)
                {
                    throw new Exception(
                        $"Model does not have at least 2 outputs. Found: {string.Join(", ", _outputNames)}");
                }

                // We assume the first output is boxes and the second is logits
                // based on the names you provided.
                if (!_outputNames.Contains("pred_boxes") || !_outputNames.Contains("pred_logits"))
                {
                    OnErrorOccurred?.Invoke(
                        $"Warning: Model outputs are [{string.Join(", ", _outputNames)}]. Code is optimized for [pred_boxes, pred_logits]. Proceeding, but order must be correct.");
                }

                // We will now assume the order based on your names:
                // 0: pred_boxes, 1: pred_logits
                // We re-order our internal list to match this assumption.
                _outputNames = new string[] {"pred_boxes", "pred_logits"};
            }
            else
            {
                OnErrorOccurred?.Invoke($"Model file not found: {modelPath}");
            }
        }
        catch (Exception ex)
        {
            OnErrorOccurred?.Invoke($"Failed to load model: {ex.Message}");
        }
    }

// ######################################################################
// CHANGED: This entire method is rewritten for 2-output DETR
// (pred_boxes, pred_logits)
// ######################################################################
// This helper struct is for debugging, place it inside your class
private struct DebugDetection
{
    public int ClassId { get; set; }
    public float Score { get; set; }
    public RectangleF Box { get; set; }
    public override string ToString() => $"ID: {ClassId}, Score: {Score:P2}, Box: [x={Box.X:F0}, y={Box.Y:F0}, w={Box.Width:F0}, h={Box.Height:F0}]";
}

private List<Detection> DetectPersons(Mat frame)
{
    Debug.WriteLine($"{DateTime.Now} Detecting objects (OnnxRuntime)");

    isPersonDetected = false;
    isChairDetected = false;
    var allPersonDetections = new List<Detection>();

    // CHANGED: Match your model's input size
    var inputWidth = 384;
    var inputHeight = 384;

    // 1. Pre-processing (With ImageNet Standardization)
    // -------------------------------------------------
    var tensor = new DenseTensor<float>(new[] { 1, 3, inputHeight, inputWidth });

    // ImageNet constants (R, G, B order)
    float[] mean = new float[] { 0.485f, 0.456f, 0.406f };
    float[] std = new float[] { 0.229f, 0.224f, 0.225f };

    using (var resizedImage = new Mat())
    {
        CvInvoke.Resize(frame, resizedImage, new Size(inputWidth, inputHeight), 0, 0, Inter.Linear);
        byte[] bgrBytes = resizedImage.ToImage<Bgr, byte>().Bytes;

        int pixelIndex = 0;
        for (int y = 0; y < inputHeight; y++)
        {
            for (int x = 0; x < inputWidth; x++)
            {
                // Normalize 0-255 -> 0-1
                float r_norm = bgrBytes[pixelIndex + 2] / 255.0f;
                float g_norm = bgrBytes[pixelIndex + 1] / 255.0f;
                float b_norm = bgrBytes[pixelIndex + 0] / 255.0f;

                // Standardize (value - mean) / std
                tensor[0, 0, y, x] = (r_norm - mean[0]) / std[0]; // R
                tensor[0, 1, y, x] = (g_norm - mean[1]) / std[1]; // G
                tensor[0, 2, y, x] = (b_norm - mean[2]) / std[2]; // B

                pixelIndex += 3;
            }
        }
    }

    var inputs = new List<NamedOnnxValue>
    {
        NamedOnnxValue.CreateFromTensor(_inputNames[0], tensor)
    };

    // 2. Inference
    // -------------------------------------------------
    using (var results = _inferenceSession.Run(inputs, _outputNames))
    {
        // 3. Post-processing
        // -------------------------------------------------
        var boxesTensor = results.First(r => r.Name == _outputNames[0]).AsTensor<float>();
        var logitsTensor = results.First(r => r.Name == _outputNames[1]).AsTensor<float>();

        var boxesData = boxesTensor.ToDenseTensor().Buffer.Span;
        var logitsData = logitsTensor.ToDenseTensor().Buffer.Span;

        var numQueries = (int)logitsTensor.Dimensions[1];
        var numClasses = (int)logitsTensor.Dimensions[2];

        var nmsBoxes = new List<Rectangle>();
        var nmsConfidences = new List<float>();
        var nmsDetections = new List<Detection>();

        // For debugging:
        var allDetectionsForDebug = new List<DebugDetection>();

        for (var i = 0; i < numQueries; i++)
        {
            // Find the best class for this query
            float maxScore = -float.MaxValue;
            int maxClassId = -1;
            int logitsBaseIndex = i * numClasses;

            for (var j = 0; j < numClasses; j++)
            {
                var score = logitsData[logitsBaseIndex + j];
                if (score > maxScore)
                {
                    maxScore = score;
                    maxClassId = j;
                }
            }

            float confidence = 1.0f / (1.0f + (float)Math.Exp(-maxScore));



            // Now, filter based on confidence and class ID
            if (confidence > confidenceTreshold)
            {
                // --- THIS IS THE FIXED BOX LOGIC ---
                // Get the correct box data from the 'boxesData' tensor
                int boxBaseIndex = i * 4;

                // Get normalized [cx, cy, w, h] and scale to original frame size
                float centerX = boxesData[boxBaseIndex + 0] * frame.Width;
                float centerY = boxesData[boxBaseIndex + 1] * frame.Height;
                float width   = boxesData[boxBaseIndex + 2] * frame.Width;
                float height  = boxesData[boxBaseIndex + 3] * frame.Height;

                // Convert from [center, center] to top-left [x, y]
                float x = centerX - width / 2;
                float y = centerY - height / 2;

                // --- Add to debug list BEFORE filtering ---
                allDetectionsForDebug.Add(new DebugDetection
                {
                    ClassId = maxClassId,
                    Score = confidence,
                    Box = new RectangleF(x, y, width, height)
                });
                if (width > frame.Width / Sensitivity)
                {
                    nmsBoxes.Add(new Rectangle((int)x, (int)y, (int)width, (int)height));
                    nmsConfidences.Add(confidence);
                    nmsDetections.Add(new Detection
                    {
                        Box = new RectangleF(x, y, width, height),
                        Confidence = confidence,
                        ClassId = maxClassId,
                        ClassName = _classNames[maxClassId]
                    });
                }
                // else if (maxClassId == 62) // Chair
                // {
                //     isChairDetected = true;
                // }
            }
        }

        // --- DEBUGGING: Print top 5 detections ---
        var top5 = allDetectionsForDebug.OrderByDescending(d => d.Score).Take(5);
        Debug.WriteLine("--- MODEL TOP 5 DETECTIONS (PRE-FILTER) ---");
        foreach (var d in top5)
        {
            Debug.WriteLine(d.ToString());
        }
        // --- END DEBUGGING ---

        // 4. NMS (Non-Maximum Suppression)
        if (nmsDetections.Count > 0)
        {
            int[] indices = DnnInvoke.NMSBoxes(nmsBoxes.ToArray(), nmsConfidences.ToArray(), confidenceTreshold,
                NMS_THRESHOLD);

            if (indices != null && indices.Length > 0)
            {
                foreach (var idx in indices)
                {
                    allPersonDetections.Add(nmsDetections[idx]);
                }
                isPersonDetected = true;
            }
        }
    }

    return allPersonDetections;
}
#endregion

    #region Nested Types

    private class Detection
    {
        public RectangleF Box { get; set; }
        public float Confidence { get; set; }
        public string ClassName { get; set; }
        public int ClassId { get; set; }
    }

    #endregion
}
