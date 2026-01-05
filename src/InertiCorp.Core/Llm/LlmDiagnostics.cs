using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using LLama.Native;

namespace InertiCorp.Core.Llm;

/// <summary>
/// Captures and stores LLM diagnostic information for troubleshooting.
/// </summary>
public static class LlmDiagnostics
{
    // Windows API for adding DLL search directories
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern IntPtr AddDllDirectory(string newDirectory);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
    private static extern bool SetDefaultDllDirectories(uint directoryFlags);

    private const uint LOAD_LIBRARY_SEARCH_DEFAULT_DIRS = 0x00001000;
    private const uint LOAD_LIBRARY_SEARCH_USER_DIRS = 0x00000400;

    private static readonly StringBuilder _logBuffer = new();
    private static readonly object _lock = new();
    private static bool _initialized;

    private static string? _logFilePath;

    /// <summary>
    /// Log to Console, Debug, and a diagnostic file for visibility in release builds.
    /// </summary>
    private static void Log(string message)
    {
        Console.WriteLine(message);
        Debug.WriteLine(message);

        // Also write to a diagnostic file since Godot doesn't capture Console output
        try
        {
            if (_logFilePath == null)
            {
                var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                var logDir = Path.Combine(appData, "InertiCorp");
                Directory.CreateDirectory(logDir);
                _logFilePath = Path.Combine(logDir, "llm_diagnostics.log");
                // Clear previous log
                File.WriteAllText(_logFilePath, $"=== LLM Diagnostics Log - {DateTime.Now} ===\n");
            }
            File.AppendAllText(_logFilePath, message + "\n");
        }
        catch
        {
            // Ignore file write errors
        }
    }

    /// <summary>
    /// Whether CUDA/GPU was detected during initialization.
    /// </summary>
    public static bool GpuDetected { get; private set; }

    /// <summary>
    /// The GPU device name if detected.
    /// </summary>
    public static string? GpuDeviceName { get; private set; }

    /// <summary>
    /// The backend that was loaded (CPU, CUDA12, etc.).
    /// </summary>
    public static string BackendLoaded { get; private set; } = "Unknown";

    /// <summary>
    /// Number of GPU layers being used.
    /// </summary>
    public static int GpuLayersUsed { get; private set; }

    /// <summary>
    /// Number of CPU threads configured.
    /// </summary>
    public static int CpuThreads { get; private set; }

    /// <summary>
    /// The full diagnostic log from LLamaSharp.
    /// </summary>
    public static string FullLog
    {
        get
        {
            lock (_lock)
            {
                return _logBuffer.ToString();
            }
        }
    }

    /// <summary>
    /// Initializes diagnostic logging and configures backend priority.
    /// Must be called before any LLamaSharp operations.
    /// </summary>
    public static void Initialize()
    {
        if (_initialized) return;
        _initialized = true;

        Log("[LlmDiagnostics] Initializing - configuring CUDA preference with CPU fallback");

        // Log current working directory - this is where llama.cpp looks for backends!
        var cwd = Environment.CurrentDirectory;
        Log($"[LlmDiagnostics] Current working directory: {cwd}");

        // Set GGML_BACKEND_DIR FIRST before any other operations
        // This must be done before llama.cpp tries to discover backends
        // llama.cpp searches: GGML_BACKEND_DIR, executable dir, current working dir
        var earlyBaseDir = AppDomain.CurrentDomain.BaseDirectory;
        var earlyAssemblyDir = Path.GetDirectoryName(typeof(LlmDiagnostics).Assembly.Location);
        if (!string.IsNullOrEmpty(earlyAssemblyDir) && Directory.Exists(earlyAssemblyDir))
        {
            earlyBaseDir = earlyAssemblyDir;
        }
        var earlyCuda12Path = Path.Combine(earlyBaseDir, "runtimes", "win-x64", "native", "cuda12");
        if (Directory.Exists(earlyCuda12Path))
        {
            // Set both variants just in case
            Environment.SetEnvironmentVariable("GGML_BACKEND_DIR", earlyCuda12Path);
            Environment.SetEnvironmentVariable("GGML_BACKEND_PATH", earlyCuda12Path);
            Log($"[LlmDiagnostics] Early set GGML_BACKEND_DIR={earlyCuda12Path}");
        }
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        Log($"[LlmDiagnostics] AppDomain base directory: {baseDir}");

        // For Godot exports, .NET assemblies are in data_* subdirectory, not the base directory
        // We need to find the actual directory containing our assemblies
        var assemblyLocation = typeof(LlmDiagnostics).Assembly.Location;
        Log($"[LlmDiagnostics] Assembly.Location: {assemblyLocation}");

        var assemblyDir = Path.GetDirectoryName(assemblyLocation);
        if (!string.IsNullOrEmpty(assemblyDir) && Directory.Exists(assemblyDir))
        {
            baseDir = assemblyDir;
            Log($"[LlmDiagnostics] Using assembly directory: {baseDir}");
        }
        else
        {
            // Fallback: look for data_* directory in the base directory (Godot export structure)
            Log($"[LlmDiagnostics] Assembly directory not found, checking for data_* fallback");
            var dataDir = Directory.GetDirectories(baseDir, "data_*").FirstOrDefault();
            if (dataDir != null)
            {
                baseDir = dataDir;
                Log($"[LlmDiagnostics] Using Godot data directory: {baseDir}");
            }
            else
            {
                Log($"[LlmDiagnostics] No data_* directory found, using base: {baseDir}");
            }
        }

        // Check CUDA12 native directory
        var cuda12Path = Path.Combine(baseDir, "runtimes", "win-x64", "native", "cuda12");
        var ggmlCudaPath = Path.Combine(cuda12Path, "ggml-cuda.dll");
        Log($"[LlmDiagnostics] CUDA12 path: {cuda12Path}");
        Log($"[LlmDiagnostics] CUDA12 directory exists: {Directory.Exists(cuda12Path)}");
        Log($"[LlmDiagnostics] ggml-cuda.dll exists: {File.Exists(ggmlCudaPath)}");

        // Check for NVIDIA driver (nvcuda.dll)
        var nvcudaPath = Path.Combine(Environment.SystemDirectory, "nvcuda.dll");
        Log($"[LlmDiagnostics] NVIDIA driver (nvcuda.dll) exists: {File.Exists(nvcudaPath)}");

        // Use Windows API to add DLL search directories for CUDA runtime
        // This is more reliable than PATH for DLL loading
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                // Also add to PATH for fallback DLL resolution
                var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
                if (!currentPath.Contains(cuda12Path))
                {
                    Environment.SetEnvironmentVariable("PATH", cuda12Path + ";" + baseDir + ";" + currentPath);
                    Log($"[LlmDiagnostics] Added cuda12 and base to PATH");
                }

                // Enable user-defined DLL directories
                SetDefaultDllDirectories(LOAD_LIBRARY_SEARCH_DEFAULT_DIRS | LOAD_LIBRARY_SEARCH_USER_DIRS);

                // Add cuda12 directory so ggml-cuda.dll can find CUDA runtime DLLs
                if (Directory.Exists(cuda12Path))
                {
                    var result = AddDllDirectory(cuda12Path);
                    Log($"[LlmDiagnostics] AddDllDirectory(cuda12): {(result != IntPtr.Zero ? "success" : "failed")}");
                }

                // Add base directory (also contains CUDA runtime DLLs)
                var baseResult = AddDllDirectory(baseDir);
                Log($"[LlmDiagnostics] AddDllDirectory(base): {(baseResult != IntPtr.Zero ? "success" : "failed")}");
            }
            catch (Exception ex)
            {
                Log($"[LlmDiagnostics] Failed to add DLL directories: {ex.Message}");
            }
        }

        // CUDA12 runtimes folder is the canonical location for native libs
        // Do NOT put DLLs in base directory - use the NuGet package structure
        var llamaCudaPath = Path.Combine(cuda12Path, "llama.dll");
        Log($"[LlmDiagnostics] llama.dll in cuda12: {File.Exists(llamaCudaPath)}");

        // Set backend path to cuda12 directory where ggml-cuda.dll lives
        // This tells llama.cpp where to find backend plugins
        if (Directory.Exists(cuda12Path))
        {
            Environment.SetEnvironmentVariable("GGML_BACKEND_DIR", cuda12Path);
            Environment.SetEnvironmentVariable("GGML_BACKEND_PATH", cuda12Path);
            Log($"[LlmDiagnostics] Set GGML_BACKEND_DIR={cuda12Path}");
        }

        // Configure LLamaSharp with the cuda12 runtime path
        var llamaPath = File.Exists(llamaCudaPath) ? llamaCudaPath : null;

        if (llamaPath != null)
        {
            Log($"[LlmDiagnostics] Using llama.dll from: {llamaPath}");

            // Configure with explicit library path, CUDA enabled, AND auto-fallback for CPU ops
            NativeLibraryConfig.LLama
                .WithLibrary(llamaPath)
                .WithSearchDirectories([cuda12Path])  // Only use cuda12 runtime folder
                .WithCuda()  // Enable CUDA backend loading
                .WithAutoFallback()  // CRITICAL: Enables CPU backend for mixed operations
                .WithLogCallback(OnNativeLog);
        }
        else if (Directory.Exists(cuda12Path))
        {
            Log("[LlmDiagnostics] Configuring with CUDA12 search path");
            NativeLibraryConfig.LLama
                .WithSearchDirectories([cuda12Path])
                .WithCuda()
                .WithAutoFallback()
                .WithLogCallback(OnNativeLog);
        }
        else
        {
            Log("[LlmDiagnostics] CUDA12 directory not found, using default config");
            NativeLibraryConfig.All
                .WithCuda()
                .WithAutoFallback()
                .WithLogCallback(OnNativeLog);
        }

        Log("[LlmDiagnostics] Configuration complete");

        // Let LLamaSharp handle native library loading - don't interfere with P/Invoke
        // The native library will be loaded when the model is first accessed
        Log("[LlmDiagnostics] Setup complete - native library will load on first model access");
    }

    private static void OnNativeLog(LLamaLogLevel level, string message)
    {
        lock (_lock)
        {
            var logLine = $"[{level}] {message.TrimEnd()}";
            _logBuffer.AppendLine(logLine);

            // Also print to debug output so we can see it in Godot
            Debug.WriteLine($"[LLamaSharp] {logLine}");

            // Write to diagnostic file too
            Log($"[Native] {logLine}");

            // Parse log messages to extract diagnostic info
            ParseLogMessage(message);
        }
    }

    private static void ParseLogMessage(string message)
    {
        // Detect CUDA initialization
        if (message.Contains("ggml_cuda_init") || message.Contains("CUDA devices"))
        {
            GpuDetected = true;
        }

        // Detect GPU device name (e.g., "Device 0: NVIDIA GeForce RTX 3080")
        if (message.Contains("Device") && message.Contains("NVIDIA"))
        {
            var startIdx = message.IndexOf("NVIDIA");
            if (startIdx >= 0)
            {
                var endIdx = message.IndexOf(',', startIdx);
                GpuDeviceName = endIdx > startIdx
                    ? message[startIdx..endIdx].Trim()
                    : message[startIdx..].Trim();
            }
        }

        // Detect which backend was loaded
        if (message.Contains("cuda12") && message.Contains("loaded"))
        {
            BackendLoaded = "CUDA 12 (GPU)";
        }
        else if (message.Contains("cuda11") && message.Contains("loaded"))
        {
            BackendLoaded = "CUDA 11 (GPU)";
        }
        else if (message.Contains("cpu") && message.Contains("loaded"))
        {
            BackendLoaded = "CPU";
        }
        else if (message.Contains("vulkan") && message.Contains("loaded"))
        {
            BackendLoaded = "Vulkan (GPU)";
        }

        // Detect layer assignment to GPU
        if (message.Contains("assigned to device CUDA"))
        {
            GpuDetected = true;
        }
    }

    /// <summary>
    /// Records the model parameters after loading.
    /// </summary>
    public static void RecordModelParams(int gpuLayers, int cpuThreads)
    {
        GpuLayersUsed = gpuLayers;
        CpuThreads = cpuThreads;
    }

    /// <summary>
    /// Gets a summary string for display in UI.
    /// </summary>
    public static string GetSummary()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Backend: {BackendLoaded}");
        sb.AppendLine($"GPU Detected: {(GpuDetected ? "Yes" : "No")}");
        if (GpuDetected && !string.IsNullOrEmpty(GpuDeviceName))
        {
            sb.AppendLine($"GPU: {GpuDeviceName}");
        }
        sb.AppendLine($"GPU Layers: {GpuLayersUsed}");
        sb.AppendLine($"CPU Threads: {CpuThreads}");
        return sb.ToString();
    }

    /// <summary>
    /// Clears the log buffer.
    /// </summary>
    public static void ClearLog()
    {
        lock (_lock)
        {
            _logBuffer.Clear();
        }
    }

    /// <summary>
    /// Logs an exception with full details for debugging.
    /// </summary>
    public static void LogException(string context, Exception ex)
    {
        Log($"[LlmDiagnostics] EXCEPTION in {context}:");
        Log($"  Type: {ex.GetType().FullName}");
        Log($"  Message: {ex.Message}");
        if (ex.InnerException != null)
        {
            Log($"  Inner Type: {ex.InnerException.GetType().FullName}");
            Log($"  Inner Message: {ex.InnerException.Message}");
        }
        Log($"  Stack: {ex.StackTrace}");
    }

}
