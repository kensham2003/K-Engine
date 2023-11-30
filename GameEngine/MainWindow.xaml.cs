using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Numerics;

using Microsoft.WindowsAPICodePack.Shell;
using System.Reflection;

using System.Text.Json;
using System.IO;

using TsNode.Interface;
using TsNode.Preset.ViewModels;
using System.Collections.ObjectModel;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Timers;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;

using Microsoft.VisualStudio.Shell;

using GameEngine.GameEntity;
using GameEngine.GameLoop;
using GameEngine.Detail;

using GameLoopClass = GameEngine.GameLoop.GameLoop;

namespace GameEngine
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        GameLoopClass m_gameLoop;
        Game m_game;

        TimeSpan lastRender;
        bool lastVisible;

        Point oldMousePosition;
        //int oldMouseWheelDelta;
        bool    mouseLeftButtonPressed = false;
        bool    mouseRightButtonPressed = false;
        float   cameraMoveSpeed = 0.0f;
        Vector3 cameraMoveVelocity = Vector3.Zero;
        GameObject selectedObject;

        bool m_simulating = false;

        Point mousePosition;
        Point newMousePosition;

        bool inspector_isWorldCoordinate = true;
        bool inspector_isScaleLinked = false;

        Task task;

        System.Timers.Timer timer;

        FileSystemWatcher m_fileSystemWatcher;
        private List<string> m_filesToIgnore = new List<string>();
        
        //public class GameObject
        //{
        //    public Vector3 Position { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
        //    public Vector3 Rotation { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
        //    public Vector3 Scale { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);

        //    public string ModelName { get; set; }

        //    public string Content { get; set; }

        //    public string Script { get; set; }

        //    public GameObject(string content)
        //    {
        //        Content = content;
        //    }

        //    public override string ToString()
        //    {
        //        return Content.ToString();
        //    }
        //}

        public class MainWindowDataContext
        {
            public ObservableCollection<INodeDataContext> Nodes { get; set; }
            public ObservableCollection<IConnectionDataContext> Connections { get; set; }

            public MainWindowDataContext()
            {
                Nodes = new ObservableCollection<INodeDataContext>();
                Connections = new ObservableCollection<IConnectionDataContext>();

                //ノード一個目を作る
                var node1 = new PresetNodeViewModel()
                {
                    OutputPlugs = new ObservableCollection<IPlugDataContext>
                {
                    new PresentPlugViewModel(),
                }
                };

                //ノード二個目を作る
                var node2 = new PresetNodeViewModel()
                {
                    X = 150,
                    InputPlugs = new ObservableCollection<IPlugDataContext>
                {
                    new PresentPlugViewModel(),
                },
                };

                //ノードを追加する
                Nodes.Add(node1);
                Nodes.Add(node2);

                //繋ぐ線を作る
                var connection = new PresetConnectionViewModel()
                {
                    SourcePlug = node1.OutputPlugs[0],
                    DestPlug = node2.InputPlugs[0],
                };

                //線を追加する
                Connections.Add(connection);
            }
        }

        public MainWindow()
        {
            this.InitializeComponent();
            this.host.Loaded += new RoutedEventHandler(this.Host_Loaded);
            this.host.SizeChanged += new SizeChangedEventHandler(this.Host_SizeChanged);

            //プロジェクトウィンドウ
            {
                Assembly assembly = Assembly.GetEntryAssembly();    //実行ファイル
                string path = assembly.Location;                    //実行ファイルのパス
                string dir = System.IO.Path.GetDirectoryName(path); //実行ファイルの場所

                ProjectBrowser.Navigate(ShellFileSystemFolder.FromFolderPath(dir));
            }

            //タイマー
            {
                timer = new System.Timers.Timer(1.0 / 30.0);
                timer.Elapsed += new ElapsedEventHandler(TimerUpdate);
            }

            //ノードエディタ
            DataContext = new MainWindowDataContext();

            //ゲームループ
            m_gameLoop = new GameLoopClass();
            m_game = new Game();
            m_gameLoop.Load(m_game);
            m_gameLoop.Start();

            m_fileSystemWatcher = new FileSystemWatcher(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/asset");
            m_fileSystemWatcher.EnableRaisingEvents = true;
            m_fileSystemWatcher.IncludeSubdirectories = true;
            m_fileSystemWatcher.Changed += OnFileChanged;
            m_fileSystemWatcher.Created += OnFileCreated;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e) 
        {
            m_filesToIgnore.Add(e.Name);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            if (m_filesToIgnore.Contains(e.Name))
            {
                m_filesToIgnore.Remove(e.Name);
                return;
            }

            if (System.IO.Path.GetExtension(e.FullPath) == ".cs")
            {

                string code = "";

                using(FileStream stream = File.Open(e.FullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using(StreamReader reader = new StreamReader(stream))
                    {
                        code = reader.ReadToEnd();
                    }
                }

                //string code = File.ReadAllText(e.FullPath);

                var parsedSyntaxTree = Parse(code, "", CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8));

                string name = System.IO.Path.GetFileNameWithoutExtension(e.Name);

                string upperClassName = name[0].ToString().ToUpper() + name.Substring(1);

                var compilation
                    = CSharpCompilation.Create(name, new SyntaxTree[] { parsedSyntaxTree }, DefaultReferences, DefaultCompilationOptions);

                using (var stream = new MemoryStream())
                {
                    var emitResult = compilation.Emit(stream);

                    foreach (var diagnostic in emitResult.Diagnostics)
                    {
                        var pos = diagnostic.Location.GetLineSpan();
                        var location =
                            "(" + pos.Path + "@Line" + (pos.StartLinePosition.Line + 1) +
                            ":" +
                            (pos.StartLinePosition.Character + 1) + ")";
                        Console.WriteLine(
                            $"[{diagnostic.Severity}, {location}]{diagnostic.Id}, {diagnostic.GetMessage()}"
                        );
                    }
                    if (emitResult.Success)
                    {
                        //コンパイル成功
                        //stream.Seek(0, SeekOrigin.Begin);

                        //var assembly = Assembly.Load(Encoding.UTF8.GetString(stream.ToArray()));

                        //var assembly = Assembly.LoadFile(p);

                        Assembly[] b = AppDomain.CurrentDomain.GetAssemblies();

                        var assembly = Assembly.Load(stream.ToArray());

                        Assembly[] a = AppDomain.CurrentDomain.GetAssemblies();

                        var classType = assembly.GetType("GameEngine.GameEntity." + upperClassName);

                        this.Dispatcher.Invoke(() =>
                        {
                            GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;

                            inspectorObject.RemoveComponent(upperClassName);

                            var instance = Activator.CreateInstance(classType, null);

                            dynamic ins = Convert.ChangeType(instance, classType);

                            string scriptPath = System.IO.Path.GetFullPath(e.FullPath);

                            inspectorObject.AddScript(ins, scriptPath, upperClassName);
                        });



                        //var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
                        //stackPanel.Children.Add(new Label { Content = upperClassName });
                        //Button ComponentButton = new Button();
                        //ComponentButton.Content = "Open Script";
                        //ComponentButton.Width = 180;
                        //ComponentButton.Click += (object ss, RoutedEventArgs ee) => { System.Diagnostics.Process.Start(scriptPath); };

                        //Button CompileButton = new Button();
                        //CompileButton.Content = "Compile Script";
                        //CompileButton.Width = 180;
                        //stackPanel.Children.Add(ComponentButton);
                        //stackPanel.Children.Add(new Separator());

                        //Component_Panel.Children.Add(stackPanel);
                    }
                }
            }
        }

        private static bool Init()
        {

            bool initSucceeded = NativeMethods.InvokeWithDllProtection(() => NativeMethods.Init()) >= 0;
            //System.Threading.Thread.Sleep(500);

            //if (!initSucceeded)
            //{
            //    MessageBox.Show("Failed to initialize.", "WPF D3D Interop", MessageBoxButton.OK, MessageBoxImage.Error);

            //    if (Application.Current != null)
            //    {
            //        Application.Current.Shutdown();
            //    }
            //}

            return initSucceeded;
        }

        private static void Cleanup()
        {
            NativeMethods.InvokeWithDllProtection(NativeMethods.Cleanup);
        }

        private static int Render(IntPtr resourcePointer, bool isNewSurface)
        {
            return NativeMethods.InvokeWithDllProtection(() => NativeMethods.Render(resourcePointer, isNewSurface));
        }

        #region Callbacks
        private void Host_Loaded(object sender, RoutedEventArgs e)
        {
            Init();
            this.InitializeRendering();

        }

        private void Host_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double dpiScale = 1.0; // default value for 96 dpi

            // determine DPI
            // (as of .NET 4.6.1, this returns the DPI of the primary monitor, if you have several different DPIs)
            var hwndTarget = PresentationSource.FromVisual(this).CompositionTarget as HwndTarget;
            if (hwndTarget != null)
            {
                dpiScale = hwndTarget.TransformToDevice.M11;
            }

            int surfWidth = (int)(host.ActualWidth < 0 ? 0 : Math.Ceiling(host.ActualWidth * dpiScale));
            int surfHeight = (int)(host.ActualHeight < 0 ? 0 : Math.Ceiling(host.ActualHeight * dpiScale));

            // Notify the D3D11Image of the pixel size desired for the DirectX rendering.
            // The D3DRendering component will determine the size of the new surface it is given, at that point.
            InteropImage.SetPixelSize(surfWidth, surfHeight);

            // Stop rendering if the D3DImage isn't visible - currently just if width or height is 0
            // TODO: more optimizations possible (scrolled off screen, etc...)
            bool isVisible = (surfWidth != 0 && surfHeight != 0);
            if (lastVisible != isVisible)
            {
                lastVisible = isVisible;
                if (lastVisible)
                {
                    CompositionTarget.Rendering += CompositionTarget_Rendering;
                }
                else
                {
                    CompositionTarget.Rendering -= CompositionTarget_Rendering;
                }
            }
        }

        #endregion Callbacks

        #region Helpers
        private void InitializeRendering()
        {
            InteropImage.WindowOwner = (new System.Windows.Interop.WindowInteropHelper(this)).Handle;
            InteropImage.OnRender = this.DoRender;


            // Start rendering now!
            InteropImage.RequestRender();
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            RenderingEventArgs args = (RenderingEventArgs)e;

            // It's possible for Rendering to call back twice in the same frame 
            // so only render when we haven't already rendered in this frame.
            if (this.lastRender != args.RenderingTime)
            {
                InteropImage.RequestRender();
                this.lastRender = args.RenderingTime;
            }
        }

        private void UninitializeRendering()
        {
            Cleanup();

            CompositionTarget.Rendering -= this.CompositionTarget_Rendering;
        }
        #endregion Helpers

        private void DoRender(IntPtr surface, bool isNewSurface)
        {
            Render(surface, isNewSurface);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            this.UninitializeRendering();
        }

        public static class NativeMethods
        {
            /// <summary>
            /// Variable used to track whether the missing dependency dialog has been displayed,
            /// used to prevent multiple notifications of the same failure.
            /// </summary>
            private static bool errorHasDisplayed;

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int Init();

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void Cleanup();

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int Render(IntPtr resourcePointer, bool isNewSurface);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetObjectPosition(string ObjectName, Vector3 Position);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetObjectRotation(string ObjectName, Vector3 Rotation);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetObjectScale(string ObjectName, Vector3 Scale);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetObjectTransform(string ObjectName, Vector3 Position, Vector3 Rotation, Vector3 Scale);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern Vector3 GetObjectPosition(string ObjectName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern Vector3 GetObjectRotation(string ObjectName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern Vector3 GetObjectScale(string ObjectName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern Vector3 GetObjectRight(string ObjectName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern Vector3 GetObjectTop(string ObjectName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern Vector3 GetObjectForward(string ObjectName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void MoveObjectPosition(string ObjectName, Vector3 vec);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void MoveObjectRight(string ObjectName, float amount);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void MoveObjectTop(string ObjectName, float amount);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void MoveObjectForward(string ObjectName, float amount);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void RotateObject(string ObjectName, Vector3 vec);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void ScaleObject(string ObjectName, Vector3 vec);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetObjectName(string ObjectName, string newObjectName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void CallMoveCamera();

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void ResetMoveCamera();

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr RaycastObject(float x, float y, float screenHeight, float screenWidth);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern Vector3 GetRayFromScreen(float x, float y, float screenHeight, float screenWidth);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void FreeRaycastChar(IntPtr p);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void AddModel(string ObjectName, string FileName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetScenePlaying(bool playing);

            /// <summary>
            /// Method used to invoke an Action that will catch DllNotFoundExceptions and display a warning dialog.
            /// </summary>
            /// <param name="action">The Action to invoke.</param>
            public static void InvokeWithDllProtection(Action action)
            {
                InvokeWithDllProtection(
                    () =>
                    {
                        action.Invoke();
                        return 0;
                    });
            }

            /// <summary>
            /// Method used to invoke A Func that will catch DllNotFoundExceptions and display a warning dialog.
            /// </summary>
            /// <param name="func">The Func to invoke.</param>
            /// <returns>The return value of func, or default(T) if a DllNotFoundException was caught.</returns>
            /// <typeparam name="T">The return type of the func.</typeparam>
            public static T InvokeWithDllProtection<T>(Func<T> func)
            {
                try
                {
                    return func.Invoke();
                }
                catch (DllNotFoundException e)
                {
                    if (!errorHasDisplayed)
                    {
                        MessageBox.Show("This sample requires:\nManual build of the D3DVisualization project, which requires installation of Windows 10 SDK or DirectX SDK.\n" +
                                        "Installation of the DirectX runtime on non-build machines.\n\n" +
                                        "Detailed exception message: " + e.Message, "WPF D3D11 Interop",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                        errorHasDisplayed = true;

                        if (Application.Current != null)
                        {
                            Application.Current.Shutdown();
                        }
                    }
                }

                return default(T);
            }
        }

        //===============================
        //            HOST
        //===============================

        //左クリック：レイキャストして一番近いオブジェクトを選択
        private void Host_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Point localMousePosition = e.GetPosition(host);
            double height = host.ActualHeight;
            double width = host.ActualWidth;
            localMousePosition.Y -= height / 2;
            localMousePosition.X -= width / 2;
            float x = (float)(localMousePosition.X);
            float y = (float)(localMousePosition.Y);
            float screenHeight = (float)height;
            float screenWidth = (float)width;
            string search;
            IntPtr ptr = NativeMethods.InvokeWithDllProtection(() => NativeMethods.RaycastObject(x, y, screenHeight, screenWidth));
            search = Marshal.PtrToStringAnsi(ptr);
            NativeMethods.InvokeWithDllProtection(() => NativeMethods.FreeRaycastChar(ptr));
            if (search != "")
            {
                foreach(GameObject gameObject in HierarchyListBox.Items)
                {
                    if(gameObject.ToString() == search)
                    {
                        HierarchyListBox.SelectedItem = gameObject;
                        return;
                    }
                }
            }
        }

        //右ボタン押しながら+WASD：カメラを移動
        private void Host_MouseRightButtonDown(object sender, MouseEventArgs e)
        {
            host.Focus();
            mouseRightButtonPressed = true;
            task = new Task(MoveCameraTask);
            task.Start();
        }

        private void Host_MouseUp(object sender, MouseEventArgs e)
        {
            mouseRightButtonPressed = false;
        }

        //60fpsで移動
        private void MoveCameraTask()
        {
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (mouseRightButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.CallMoveCamera());
                    now = DateTime.Now;
                }
            }
            NativeMethods.InvokeWithDllProtection(() => NativeMethods.ResetMoveCamera());
        }

        private void Host_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(host);
            Vector3 cameraPosition = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectPosition("Camera"));
            Vector3 cameraRotation = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation("Camera"));



            if (e.RightButton == MouseButtonState.Pressed)
            {
                mouseRightButtonPressed = true;

                cameraRotation.Y += (float)(mousePosition.X - oldMousePosition.X) * 0.003f;
                cameraRotation.X += (float)(mousePosition.Y - oldMousePosition.Y) * 0.003f;

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectRotation("Camera", cameraRotation));

            }

            if(e.RightButton == MouseButtonState.Released)
            {
                mouseRightButtonPressed = false;
            }

            if(e.MiddleButton == MouseButtonState.Pressed)
            {
                float dx = (float)(mousePosition.X - oldMousePosition.X) * 0.01f;
                float dy = (float)(mousePosition.Y - oldMousePosition.Y) * 0.01f;

                cameraPosition.X -= (float)Math.Cos(cameraRotation.Y) * dx - (float)Math.Sin(cameraRotation.Y) * (float)Math.Sin(cameraRotation.X) * dy;
                cameraPosition.Z += (float)Math.Sin(cameraRotation.Y) * dx + (float)Math.Cos(cameraRotation.Y) * (float)Math.Sin(cameraRotation.X) * dy;
                cameraPosition.Y += (float)Math.Cos(cameraRotation.X) * dy;

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectPosition("Camera", cameraPosition));
            }

            if(e.LeftButton == MouseButtonState.Pressed)
            {
                GameObject gameObject = HierarchyListBox.SelectedItem as GameObject;

                if (gameObject == null)
                    return;

                float dx = (float)(mousePosition.X - oldMousePosition.X) * 0.01f;
                float dy = (float)(mousePosition.Y - oldMousePosition.Y) * 0.01f;

                Vector3 position = gameObject.Position;
                position.X += (float)Math.Cos(cameraRotation.Y) * dx - (float)Math.Sin(cameraRotation.Y) * (float)Math.Sin(cameraRotation.X) * dy;
                position.Z -= (float)Math.Sin(cameraRotation.Y) * dx + (float)Math.Cos(cameraRotation.Y) * (float)Math.Sin(cameraRotation.X) * dy;
                position.Y -= (float)Math.Cos(cameraRotation.X) * dy;
                gameObject.Position = position;

                ObjectToInspector();
            }

            oldMousePosition = mousePosition;
        }

        private void Host_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            int wheel = e.Delta;
            Vector3 cameraPosition = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectPosition("Camera"));
            Vector3 cameraRotation = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation("Camera"));

            Matrix4x4 rotation = Matrix4x4.CreateFromYawPitchRoll(cameraRotation.Y, cameraRotation.X, cameraRotation.Z);
            Vector3 dz = new Vector3(rotation.M31, rotation.M32, rotation.M33);

            cameraPosition += dz * wheel * 0.003f;

            NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectPosition("Camera", cameraPosition));
        }

        //Add Model
        private void Host_PreviewDrop(object sender, DragEventArgs e)
        {
            string[] paths = ((string[])e.Data.GetData(DataFormats.FileDrop));

            string filename = paths[0];
            string objectName = System.IO.Path.GetFileNameWithoutExtension(filename);

            int cnt = 1;
            for (int i = 0; i < HierarchyListBox.Items.Count; i++)
            {
                //GameObject now = (GameObject)HierarchyListBox.Items.GetItemAt(i);
                GameObject now = HierarchyListBox.Items.GetItemAt(i) as GameObject;
                if (filename == now.ModelName)
                {
                    cnt++;
                }
            }
            if (cnt > 1)
            {
                objectName = objectName + "_" + cnt.ToString();
            }

            GameObject gameObject = new GameObject(objectName);
            gameObject.ModelName = filename;
            gameObject.AddModel(filename);
            gameObject.AddComponent(new GameEntity.testComponent(gameObject));
            m_game.AddGameObject(gameObject, Define.LAYER_3D_OBJECT);
            
            HierarchyListBox.Items.Add(gameObject);

            NativeMethods.InvokeWithDllProtection(() => NativeMethods.AddModel(objectName, filename));
        }
        

        //=====================
        //       MENU
        //=====================

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                WriteIndented = true,
                IncludeFields = true,
            };
            string fileName = "TestScene.json";
            string jsonString = File.ReadAllText(fileName);
            List<GameObject> gameObjects = JsonSerializer.Deserialize<List<GameObject>>(jsonString, options);

            foreach (GameObject gameObject in gameObjects)
            {
                HierarchyListBox.Items.Add(gameObject);
                NativeMethods.InvokeWithDllProtection(() => NativeMethods.AddModel(gameObject.Name, gameObject.ModelName));
                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectPosition(gameObject.Name, gameObject.Position));
                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectRotation(gameObject.Name, gameObject.Rotation));
                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectScale(gameObject.Name, gameObject.Scale));
            }
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                WriteIndented = true,
                IncludeFields = true,
            };

            string fileName = "TestScene.json";
            //foreach(object o in HierarchyListBox.Items)
            //{
            //    GameObject g = o as GameObject;
            //    string jsonStr = JsonSerializer.Serialize(g.Components.ToArray(), options);
            //    File.WriteAllText(fileName, jsonStr);
            //}
            string jsonString = JsonSerializer.Serialize(HierarchyListBox.Items, options);
            File.WriteAllText(fileName, jsonString);
        }

        private void MenuItem_Run_Click(object sender, RoutedEventArgs e)
        {
            InspectorToObject();
            timer.Start();
        }

        private void MenuItem_Stop_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }

        private void MenuItem_Simulate_Play_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_SimulatePlay.Visibility = Visibility.Collapsed;
            MenuItem_SimulateStop.Visibility = Visibility.Visible;
            m_gameLoop.Play();
            //NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetScenePlaying(true));
            m_simulating = true;
            var th = new Thread(new ThreadStart(SimulatingInspectorTask));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void SimulatingInspectorTask()
        {
            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);

            while (m_simulating)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        //GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;
                        //if (inspectorObject == null) return;

                        //string objectName = inspectorObject.ToString();

                        //Vector3 Pos = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectPosition(objectName));
                        //PositionX.Text = Pos.X.ToString("F2");
                        //PositionY.Text = Pos.Y.ToString("F2");
                        //PositionZ.Text = Pos.Z.ToString("F2");

                        //Vector3 Rot = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation(objectName));
                        //RotationX.Text = Rot.X.ToString("F2");
                        //RotationY.Text = Rot.Y.ToString("F2");
                        //RotationZ.Text = Rot.Z.ToString("F2");

                        //Vector3 Scl = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectScale(objectName));
                        //ScaleX.Text = Scl.X.ToString("F2");
                        //ScaleY.Text = Scl.Y.ToString("F2");
                        //ScaleZ.Text = Scl.Z.ToString("F2");

                        GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;
                        if (inspectorObject == null) return;

                        Vector3 Pos = inspectorObject.Position;
                        PositionX.Text = Pos.X.ToString("F2");
                        PositionY.Text = Pos.Y.ToString("F2");
                        PositionZ.Text = Pos.Z.ToString("F2");

                        Vector3 Rot = inspectorObject.Rotation;
                        RotationX.Text = Rot.X.ToString("F2");
                        RotationY.Text = Rot.Y.ToString("F2");
                        RotationZ.Text = Rot.Z.ToString("F2");

                        Vector3 Scl = inspectorObject.Scale;
                        ScaleX.Text = Scl.X.ToString("F2");
                        ScaleY.Text = Scl.Y.ToString("F2");
                        ScaleZ.Text = Scl.Z.ToString("F2");
                    });

                    now = DateTime.Now;
                }
            }
        }

        private void MenuItem_Simulate_Stop_Click(object sender, RoutedEventArgs e)
        {
            MenuItem_SimulateStop.Visibility = Visibility.Collapsed;
            MenuItem_SimulatePlay.Visibility = Visibility.Visible;
            //NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetScenePlaying(false));
            m_simulating = false;
            m_gameLoop.Stop();
            ObjectToInspector();
        }

        //========================
        //       HIERARCHY
        //========================

        private void HierarchyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ObjectToInspector();
            if (HierarchyListBox.SelectedItem != null)
            {
                ShowInspector();
            }
        }

        private void HierarchyListBox_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            HierarchyListBox.Focus();
            HierarchyListBox.SelectedItem = null;
            HideInspector();
        }

        private void ScriptTextBox_KeyUp(object sender, KeyEventArgs e)
        {
            InspectorToObject();
        }

        public void TimerUpdate(object sender, ElapsedEventArgs e)
        {
            List<Assembly> assembly = new List<Assembly>()
            {
                Assembly.GetAssembly(typeof(System.Dynamic.DynamicObject)),
                Assembly.GetAssembly(typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo)),
                Assembly.GetAssembly(typeof(System.Dynamic.ExpandoObject)),
                Assembly.GetAssembly(typeof(System.Data.DataTable)),
            };

            ScriptOptions option = ScriptOptions.Default.AddReferences(assembly);

            foreach(GameObject gameObject in HierarchyListBox.Items)
            {
                if (gameObject.Script == null)
                    continue;

                CSharpScript.RunAsync(gameObject.Script, globals: gameObject, options: option).Wait();

                string objectName = gameObject.ToString();

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectPosition(objectName, gameObject.Position));
            }

            if (!mouseRightButtonPressed)
            {
                cameraMoveVelocity = Vector3.Zero;
            }
        }

        //==================================
        //           INSPECTOR
        //==================================

        private void Panel_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            Inspector_Panel.Focus();
        }

        private void Inspector_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return)
                return;

            InspectorToObject();
        }

        private void ObjectToInspector()
        {
            GameObject gameObject = HierarchyListBox.SelectedItem as GameObject;

            if (gameObject == null)
                return;

            string objectName = gameObject.ToString();
            {
                PositionX.Text = gameObject.Position.X.ToString("F2");
                PositionY.Text = gameObject.Position.Y.ToString("F2");
                PositionZ.Text = gameObject.Position.Z.ToString("F2");

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectPosition(objectName, gameObject.Position));
            }
            {
                Vector3 rotation = gameObject.Rotation / (float)Math.PI * 180.0f; //Radian->Degree

                RotationX.Text = rotation.X.ToString("F2");
                RotationY.Text = rotation.Y.ToString("F2");
                RotationZ.Text = rotation.Z.ToString("F2");

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectRotation(objectName, gameObject.Rotation));
            }
            {
                ScaleX.Text = gameObject.Scale.X.ToString("F2");
                ScaleY.Text = gameObject.Scale.Y.ToString("F2");
                ScaleZ.Text = gameObject.Scale.Z.ToString("F2");

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectScale(objectName, gameObject.Scale));

            }
            ScriptTextBox.Text = gameObject.Script;
        }

        private void InspectorToObject()
        {
            GameObject gameObject = HierarchyListBox.SelectedItem as GameObject;

            if (gameObject == null)
                return;

            string objectName = gameObject.ToString();


            {
                Vector3 position;
                if (!float.TryParse(PositionX.Text, out position.X) ||
                   !float.TryParse(PositionY.Text, out position.Y) ||
                   !float.TryParse(PositionZ.Text, out position.Z))
                {
                    //入力が数字じゃない場合
                    ObjectToInspector();
                    return;
                }
                //position.X = float.Parse(PositionX.Text);
                //position.Y = float.Parse(PositionY.Text);
                //position.Z = float.Parse(PositionZ.Text);
                gameObject.Position = position;

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectPosition(objectName, gameObject.Position));
            }
            {
                Vector3 rotation;
                if (!float.TryParse(RotationX.Text, out rotation.X) ||
                    !float.TryParse(RotationY.Text, out rotation.Y) ||
                    !float.TryParse(RotationZ.Text, out rotation.Z))
                {
                    //入力が数字じゃない場合
                    ObjectToInspector();
                    return;
                }
                //rotation.X = float.Parse(RotationX.Text) * (float)Math.PI / 180.0f;
                //rotation.Y = float.Parse(RotationY.Text) * (float)Math.PI / 180.0f;
                //rotation.Z = float.Parse(RotationZ.Text) * (float)Math.PI / 180.0f;
                rotation = rotation * (float)Math.PI / 180.0f;
                gameObject.Rotation = rotation;

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectRotation(objectName, gameObject.Rotation));
            }
            {
                Vector3 scale;
                if (!float.TryParse(ScaleX.Text, out scale.X) ||
                    !float.TryParse(ScaleY.Text, out scale.Y) ||
                    !float.TryParse(ScaleZ.Text, out scale.Z))
                {
                    //入力が数字じゃない場合
                    ObjectToInspector();
                    return;
                }
                //scale.X = float.Parse(ScaleX.Text);
                //scale.Y = float.Parse(ScaleY.Text);
                //scale.Z = float.Parse(ScaleZ.Text);
                gameObject.Scale = scale;

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectScale(objectName, gameObject.Scale));
            }
            gameObject.Script = ScriptTextBox.Text;
        }

        //=================================
        //      INSPECTOR VISIBILITY
        //=================================

        private void HideInspector()
        {
            //foreach (TextBox tb in FindChildren<TextBox>(Inspector_StackPanel))
            //{
            //    tb.Visibility = Visibility.Collapsed;
            //}
            //foreach (Label l in FindChildren<Label>(Inspector_StackPanel))
            //{
            //    l.Visibility = Visibility.Collapsed;
            //}
            //foreach (Button b in FindChildren<Button>(Inspector_StackPanel))
            //{
            //    b.Visibility = Visibility.Collapsed;
            //}
            Inspector_StackPanel.Visibility = Visibility.Collapsed;
        }

        private void ShowInspector()
        {
            //foreach(TextBox tb in FindChildren<TextBox>(Inspector_StackPanel))
            //{
            //    tb.Visibility = Visibility.Visible;
            //}
            //foreach (Label l in FindChildren<Label>(Inspector_StackPanel))
            //{
            //    l.Visibility = Visibility.Visible;
            //}
            //foreach (Button b in FindChildren<Button>(Inspector_StackPanel))
            //{
            //    b.Visibility = Visibility.Visible;
            //}
            Inspector_StackPanel.Visibility = Visibility.Visible;

            Inspector_Position_DockPanel.Visibility = Visibility.Collapsed;
            Inspector_Rotation_DockPanel.Visibility = Visibility.Collapsed;
            Inspector_Scale_DockPanel.Visibility = Visibility.Collapsed;

            GameObject gameObject = HierarchyListBox.SelectedItem as GameObject;
            Inspector_Name.Text = gameObject.ToString();
        }

        private void Inspector_Position_Show(object sender, RoutedEventArgs e)
        {
            Inspector_Position_DockPanel.Visibility = Visibility.Visible;
        }

        private void Inspector_Rotation_Show(object sender, RoutedEventArgs e)
        {
            Inspector_Rotation_DockPanel.Visibility = Visibility.Visible;
        }

        private void Inspector_Scale_Show(object sender, RoutedEventArgs e)
        {
            Inspector_Scale_DockPanel.Visibility = Visibility.Visible;
        }

        private void Inspector_Panel_Loaded(object sender, RoutedEventArgs e)
        {
            HideInspector();
        }

        //==============================
        //        OBJECT NAME
        //==============================

        private void Inspector_Name_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Return)
                return;

            RenameObject();
        }


        private void RenameObject()
        {
            GameObject gameObject = HierarchyListBox.SelectedItem as GameObject;
            if (gameObject == null)
                return;

            string objectName = gameObject.ToString();
            NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectName(objectName, Inspector_Name.Text));

            gameObject.Name = Inspector_Name.Text;

            HierarchyListBox.Items.Refresh();
        }

        //=====================================
        //         INSPECTOR POSITION
        //=====================================

        private void Inspector_Position_X_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mousePosition = PointToScreen(Mouse.GetPosition(this));
            mouseLeftButtonPressed = true;
            selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(MoveObjectTaskX));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void Inspector_Position_Y_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mousePosition = PointToScreen(Mouse.GetPosition(this));
            mouseLeftButtonPressed = true;
            selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(MoveObjectTaskY));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void Inspector_Position_Z_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mousePosition = PointToScreen(Mouse.GetPosition(this));
            mouseLeftButtonPressed = true;
            selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(MoveObjectTaskZ));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }


        private void MoveObjectTaskX()
        {
            string objectName = selectedObject.ToString();
            Vector3 vecX = new Vector3(1.0f, 0.0f, 0.0f);

            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos(1, (int)newMousePosition.Y);
                            newMousePosition.X = 1;
                            mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (newMousePosition.X <= 0 && mousePosition.X > 0)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)newMousePosition.Y);
                            newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = newMousePosition.X - mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    if (inspector_isWorldCoordinate)
                    {
                        NativeMethods.InvokeWithDllProtection(() => NativeMethods.MoveObjectPosition(objectName, vecX * (float)xDiff));
                    }
                    else
                    {
                        NativeMethods.InvokeWithDllProtection(() => NativeMethods.MoveObjectRight(objectName, (float)xDiff));
                    }
                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Pos = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectPosition(objectName));
                        PositionX.Text = Pos.X.ToString("F2");
                        PositionY.Text = Pos.Y.ToString("F2");
                        PositionZ.Text = Pos.Z.ToString("F2");
                    });

                    mousePosition = newMousePosition;
                    now = DateTime.Now;
                }
            }
        }


        private void MoveObjectTaskY()
        {
            string objectName = selectedObject.ToString();
            Vector3 vecY = new Vector3(0.0f, 1.0f, 0.0f);

            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos(1, (int)newMousePosition.Y);
                            newMousePosition.X = 1;
                            mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (newMousePosition.X <= 0 && mousePosition.X > 0)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)newMousePosition.Y);
                            newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = newMousePosition.X - mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    if (inspector_isWorldCoordinate)
                    {
                        NativeMethods.InvokeWithDllProtection(() => NativeMethods.MoveObjectPosition(objectName, vecY * (float)xDiff));
                    }
                    else
                    {
                        NativeMethods.InvokeWithDllProtection(() => NativeMethods.MoveObjectTop(objectName, (float)xDiff));
                    }
                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Pos = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectPosition(objectName));
                        PositionX.Text = Pos.X.ToString("F2");
                        PositionY.Text = Pos.Y.ToString("F2");
                        PositionZ.Text = Pos.Z.ToString("F2");
                    });

                    mousePosition = newMousePosition;
                    now = DateTime.Now;
                }
            }
        }


        private void MoveObjectTaskZ()
        {
            string objectName = selectedObject.ToString();
            Vector3 vecZ = new Vector3(0.0f, 0.0f, 1.0f);

            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos(1, (int)newMousePosition.Y);
                            newMousePosition.X = 1;
                            mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (newMousePosition.X <= 0 && mousePosition.X > 0)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)newMousePosition.Y);
                            newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = newMousePosition.X - mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    if (inspector_isWorldCoordinate)
                    {
                        NativeMethods.InvokeWithDllProtection(() => NativeMethods.MoveObjectPosition(objectName, vecZ * (float)xDiff));
                    }
                    else
                    {
                        NativeMethods.InvokeWithDllProtection(() => NativeMethods.MoveObjectForward(objectName, (float)xDiff));
                    }
                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Pos = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectPosition(objectName));
                        PositionX.Text = Pos.X.ToString("F2");
                        PositionY.Text = Pos.Y.ToString("F2");
                        PositionZ.Text = Pos.Z.ToString("F2");
                    });

                    mousePosition = newMousePosition;
                    now = DateTime.Now;
                }
            }
        }

        private void Inspector_Position_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mouseLeftButtonPressed = false;
            GameObject gameObject = HierarchyListBox.SelectedItem as GameObject;
            Vector3 Pos = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectPosition(gameObject.ToString()));
            gameObject.Position = Pos;
        }

        private void Inspector_Change_World_Local(object sender, RoutedEventArgs e)
        {
            if (inspector_isWorldCoordinate)
            {
                inspector_isWorldCoordinate = false;
                Inspector_Coordinate_Button.Content = "ローカル";
            }
            else
            {
                inspector_isWorldCoordinate = true;
                Inspector_Coordinate_Button.Content = "ワールド";
            }
        }

        //=====================================
        //         INSPECTOR ROTATION
        //=====================================

        private void Inspector_Rotation_X_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mousePosition = PointToScreen(Mouse.GetPosition(this));
            mouseLeftButtonPressed = true;
            selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(RotateObjectTaskX));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void Inspector_Rotation_Y_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mousePosition = PointToScreen(Mouse.GetPosition(this));
            mouseLeftButtonPressed = true;
            selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(RotateObjectTaskY));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void Inspector_Rotation_Z_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mousePosition = PointToScreen(Mouse.GetPosition(this));
            mouseLeftButtonPressed = true;
            selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(RotateObjectTaskZ));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void RotateObjectTaskX()
        {
            string objectName = selectedObject.ToString();
            Vector3 vecX = new Vector3(1.0f, 0.0f, 0.0f);

            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos(1, (int)newMousePosition.Y);
                            newMousePosition.X = 1;
                            mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (newMousePosition.X <= 0 && mousePosition.X > 0)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)newMousePosition.Y);
                            newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = newMousePosition.X - mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.RotateObject(objectName, vecX * (float)xDiff));

                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Rot = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation(objectName));
                        RotationX.Text = Rot.X.ToString("F2");
                    });

                    mousePosition = newMousePosition;
                    now = DateTime.Now;
                }
            }
        }

        private void RotateObjectTaskY()
        {
            string objectName = selectedObject.ToString();
            Vector3 vecY = new Vector3(0.0f, 1.0f, 0.0f);

            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos(1, (int)newMousePosition.Y);
                            newMousePosition.X = 1;
                            mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (newMousePosition.X <= 0 && mousePosition.X > 0)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)newMousePosition.Y);
                            newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = newMousePosition.X - mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.RotateObject(objectName, vecY * (float)xDiff));

                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Rot = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation(objectName));
                        RotationY.Text = Rot.Y.ToString("F2");
                    });

                    mousePosition = newMousePosition;
                    now = DateTime.Now;
                }
            }
        }
        
        private void RotateObjectTaskZ()
        {
            string objectName = selectedObject.ToString();
            Vector3 vecZ = new Vector3(0.0f, 0.0f, 1.0f);

            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos(1, (int)newMousePosition.Y);
                            newMousePosition.X = 1;
                            mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (newMousePosition.X <= 0 && mousePosition.X > 0)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)newMousePosition.Y);
                            newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = newMousePosition.X - mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.RotateObject(objectName, vecZ * (float)xDiff));

                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Rot = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation(objectName));
                        RotationZ.Text = Rot.Z.ToString("F2");
                    });

                    mousePosition = newMousePosition;
                    now = DateTime.Now;
                }
            }
        }

        private void Inspector_Rotation_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mouseLeftButtonPressed = false;
            GameObject gameObject = HierarchyListBox.SelectedItem as GameObject;
            Vector3 Rot = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation(gameObject.ToString()));
            gameObject.Rotation = Rot;
        }

        //=====================================
        //         INSPECTOR SCALE
        //=====================================

        private void Inspector_Scale_X_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mousePosition = PointToScreen(Mouse.GetPosition(this));
            mouseLeftButtonPressed = true;
            selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(ScaleObjectTaskX));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void Inspector_Scale_Y_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mousePosition = PointToScreen(Mouse.GetPosition(this));
            mouseLeftButtonPressed = true;
            selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(ScaleObjectTaskY));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void Inspector_Scale_Z_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            mousePosition = PointToScreen(Mouse.GetPosition(this));
            mouseLeftButtonPressed = true;
            selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(ScaleObjectTaskZ));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void ScaleObjectTaskX()
        {
            string objectName = selectedObject.ToString();
            Vector3 vecX = new Vector3(1.0f, 0.0f, 0.0f);
            if (inspector_isScaleLinked)
            {
                vecX = new Vector3(1.0f, 1.0f, 1.0f);
            }
            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos(1, (int)newMousePosition.Y);
                            newMousePosition.X = 1;
                            mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (newMousePosition.X <= 0 && mousePosition.X > 0)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)newMousePosition.Y);
                            newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = newMousePosition.X - mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.ScaleObject(objectName, vecX * (float)xDiff));

                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Scl = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectScale(objectName));
                        ScaleX.Text = Scl.X.ToString("F2");
                        ScaleY.Text = Scl.Y.ToString("F2");
                        ScaleZ.Text = Scl.Z.ToString("F2");
                    });

                    mousePosition = newMousePosition;
                    now = DateTime.Now;
                }
            }
        }

        private void ScaleObjectTaskY()
        {
            string objectName = selectedObject.ToString();
            Vector3 vecY = new Vector3(0.0f, 1.0f, 0.0f);
            if (inspector_isScaleLinked)
            {
                vecY = new Vector3(1.0f, 1.0f, 1.0f);
            }
            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos(1, (int)newMousePosition.Y);
                            newMousePosition.X = 1;
                            mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (newMousePosition.X <= 0 && mousePosition.X > 0)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)newMousePosition.Y);
                            newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = newMousePosition.X - mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.ScaleObject(objectName, vecY * (float)xDiff));

                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Scl = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectScale(objectName));
                        ScaleX.Text = Scl.X.ToString("F2");
                        ScaleY.Text = Scl.Y.ToString("F2");
                        ScaleZ.Text = Scl.Z.ToString("F2");
                    });

                    mousePosition = newMousePosition;
                    now = DateTime.Now;
                }
            }
        }

        private void ScaleObjectTaskZ()
        {
            string objectName = selectedObject.ToString();
            Vector3 vecZ = new Vector3(0.0f, 0.0f, 1.0f);
            if (inspector_isScaleLinked)
            {
                vecZ = new Vector3(1.0f, 1.0f, 1.0f);
            }
            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos(1, (int)newMousePosition.Y);
                            newMousePosition.X = 1;
                            mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (newMousePosition.X <= 0 && mousePosition.X > 0)
                        {
                            double diff = newMousePosition.X - mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)newMousePosition.Y);
                            newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = newMousePosition.X - mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.ScaleObject(objectName, vecZ * (float)xDiff));

                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Scl = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectScale(objectName));
                        ScaleX.Text = Scl.X.ToString("F2");
                        ScaleY.Text = Scl.Y.ToString("F2");
                        ScaleZ.Text = Scl.Z.ToString("F2");
                    });

                    mousePosition = newMousePosition;
                    now = DateTime.Now;
                }
            }
        }

        private void Inspector_Scale_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            mouseLeftButtonPressed = false;
            GameObject gameObject = HierarchyListBox.SelectedItem as GameObject;
            Vector3 Scl = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectScale(gameObject.ToString()));
            gameObject.Scale = Scl;
        }

        private void Inspector_Change_Scale_Link(object sender, RoutedEventArgs e)
        {
            if (inspector_isScaleLinked)
            {
                inspector_isScaleLinked = false;
                Inspector_Scale_Button.Content = "Not Linked";
            }
            else
            {
                inspector_isScaleLinked = true;
                Inspector_Scale_Button.Content = "Linked";
            }
        }

        //===========================================
        //               FUNCTIONAL
        //===========================================

        public static IEnumerable<T> FindChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) yield return (T)Enumerable.Empty<T>();
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                DependencyObject ithChild = VisualTreeHelper.GetChild(depObj, i);
                if (ithChild == null) continue;
                if (ithChild is T t) yield return t;
                foreach (T childOfChild in FindChildren<T>(ithChild)) yield return childOfChild;
            }
        }

        private void Add_Component(object sender, RoutedEventArgs e)
        {
            string className = "";
            var dialog = new userInputDialog();
            if (dialog.ShowDialog() == true)
            {
                className = dialog.InputText;
            }
            string upperClassName = className[0].ToString().ToUpper() + className.Substring(1);
            string classDll = "asset/" + className + ".dll";
            string path = "asset/" + className + ".cs";
            string code = $@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;


namespace GameEngine.GameEntity
{{
    class {upperClassName} : GameScript
    {{
        public {upperClassName}() {{ }}

        public {upperClassName}(GameObject gameObject) : base(gameObject) {{ }}

        private float moveAmount = 0.1f;

        public override void BeginPlay()
        {{

        }}

        public override void Update(TimeSpan gameTime)
        {{
            Vector3 pos = Parent.Position;
            if(pos.X > 3 || pos.X < -3){{moveAmount *= -1;}}
            
            pos.X += moveAmount;
            Parent.Position = pos;
        }}
    }}
}}
";
            File.WriteAllText(path, code);
            //作った.csをロード

            var parsedSyntaxTree = Parse(code, "", CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8));

            var compilation
                = CSharpCompilation.Create(className, new SyntaxTree[] { parsedSyntaxTree }, DefaultReferences, DefaultCompilationOptions);
            using (var stream = new MemoryStream())
            {
                var emitResult = compilation.Emit(stream);

                foreach (var diagnostic in emitResult.Diagnostics)
                {
                    var pos = diagnostic.Location.GetLineSpan();
                    var location =
                        "(" + pos.Path + "@Line" + (pos.StartLinePosition.Line + 1) +
                        ":" +
                        (pos.StartLinePosition.Character + 1) + ")";
                    Console.WriteLine(
                        $"[{diagnostic.Severity}, {location}]{diagnostic.Id}, {diagnostic.GetMessage()}"
                    );
                }
                if (emitResult.Success)
                {
                    //コンパイル成功
                    //stream.Seek(0, SeekOrigin.Begin);

                    //var assembly = Assembly.Load(Encoding.UTF8.GetString(stream.ToArray()));

                    //var assembly = Assembly.LoadFile(p);

                    Assembly[] b = AppDomain.CurrentDomain.GetAssemblies();

                    var assembly = Assembly.Load(stream.ToArray());

                    Assembly[] a = AppDomain.CurrentDomain.GetAssemblies();

                    var classType = assembly.GetType("GameEngine.GameEntity." + upperClassName);

                    GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;

                    //var instance = (Component)Activator.CreateInstance(classType, null);

                    var instance = Activator.CreateInstance(classType, null);

                    dynamic ins = Convert.ChangeType(instance, classType);

                    //Component component = ConvertToComponent(instance);

                    string scriptPath = System.IO.Path.GetFullPath(path);

                    inspectorObject.AddScript(ins, scriptPath, upperClassName);

                    var stackPanel = new StackPanel { Orientation = Orientation.Vertical };
                    stackPanel.Children.Add(new Label { Content = upperClassName });
                    Button ComponentButton = new Button();
                    ComponentButton.Content = "Open Script";
                    ComponentButton.Width = 180;
                    ComponentButton.Click += (object ss, RoutedEventArgs ee) => { System.Diagnostics.Process.Start(scriptPath); };

                    Button CompileButton = new Button();
                    CompileButton.Content = "Compile Script";
                    CompileButton.Width = 180;
                    stackPanel.Children.Add(ComponentButton);
                    stackPanel.Children.Add(new Separator());

                    Component_Panel.Children.Add(stackPanel);

                    stream.Close();
                }
            }
        }

        private static readonly IEnumerable<string> DefaultNamespaces =
            new[]
            {
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Numerics",
                "System.Threading.Tasks",
                "GameEngine.GameEntity"
            };

        private static string runtimePath = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.1\{0}.dll";

        private static readonly IEnumerable<MetadataReference> DefaultReferences =
            new[]
            {
                MetadataReference.CreateFromFile(string.Format(runtimePath, "mscorlib")),
                MetadataReference.CreateFromFile(string.Format(runtimePath, "System")),
                MetadataReference.CreateFromFile(string.Format(runtimePath, "System.Core")),
                MetadataReference.CreateFromFile(string.Format(runtimePath, "System.Numerics")),
                //MetadataReference.CreateFromFile("CoreModule.dll")
                MetadataReference.CreateFromFile(typeof(Component).Assembly.Location),
            };

        private static readonly CSharpCompilationOptions DefaultCompilationOptions =
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOverflowChecks(true).WithOptimizationLevel(OptimizationLevel.Release)
                    .WithUsings(DefaultNamespaces);

        public SyntaxTree Parse(string text, string filename = "", CSharpParseOptions options = null)
        {
            var stringText = SourceText.From(text, Encoding.UTF8);
            return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
        }

        public Component ConvertToComponent(object m)
        {
            // Serialize the original object to json
            // Desarialize the json object to the new type 
            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                WriteIndented = true,
                IncludeFields = true,
            };
            Component obj = JsonSerializer.Deserialize<Component>(JsonSerializer.Serialize(m, options), options);
            return obj;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_simulating = false;
        }

    }

}
