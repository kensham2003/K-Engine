using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace GameEngine
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        TimeSpan lastRender;
        bool lastVisible;

        Point oldMousePosition;
        //int oldMouseWheelDelta;
        bool    hostMouseRightButtonPressed = false;
        float   cameraMoveSpeed = 0.0f;
        Vector3 cameraMoveVelocity = Vector3.Zero;

        Task task;

        Timer timer;
        
        public class GameObject
        {
            public Vector3 Position { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
            public Vector3 Rotation { get; set; } = new Vector3(0.0f, 0.0f, 0.0f);
            public Vector3 Scale { get; set; } = new Vector3(1.0f, 1.0f, 1.0f);

            public string ModelName { get; set; }

            public string Content { get; set; }

            public string Script { get; set; }

            public GameObject(string content)
            {
                Content = content;
            }

            public override string ToString()
            {
                return Content.ToString();
            }
        }

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
                timer = new Timer(1.0 / 30.0);
                timer.Elapsed += new ElapsedEventHandler(TimerUpdate);
            }

            //ノードエディタ
            DataContext = new MainWindowDataContext();
        }

        private static bool Init()
        {

            bool initSucceeded = NativeMethods.InvokeWithDllProtection(() => NativeMethods.Init()) >= 0;

            if (!initSucceeded)
            {
                MessageBox.Show("Failed to initialize.", "WPF D3D Interop", MessageBoxButton.OK, MessageBoxImage.Error);

                if (Application.Current != null)
                {
                    Application.Current.Shutdown();
                }
            }

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

        private static class NativeMethods
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
            public static extern void CallMoveCamera();

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void ResetMoveCamera();

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void AddObject(string ObjectName, string FileName);

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

        private void Host_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.RightButton == MouseButtonState.Pressed)
            {
                hostMouseRightButtonPressed = true;
                task = new Task(MoveCameraTask);
                task.Start();
            }
        }

        private void Host_MouseUp(object sender, MouseEventArgs e)
        {
            hostMouseRightButtonPressed = false;
        }

        private void MoveCameraTask()
        {
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (hostMouseRightButtonPressed)
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
            Point mousePosition = e.GetPosition(host);            Vector3 cameraPosition = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectPosition("Camera"));            Vector3 cameraRotation = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation("Camera"));            if (e.RightButton == MouseButtonState.Pressed)            {                hostMouseRightButtonPressed = true;                cameraRotation.Y += (float)(mousePosition.X - oldMousePosition.X) * 0.003f;                cameraRotation.X += (float)(mousePosition.Y - oldMousePosition.Y) * 0.003f;                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectRotation("Camera", cameraRotation));
            }

            if(e.RightButton == MouseButtonState.Released)
            {
                hostMouseRightButtonPressed = false;
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
            Vector3 cameraPosition = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectPosition("Camera"));            Vector3 cameraRotation = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation("Camera"));

            Matrix4x4 rotation = Matrix4x4.CreateFromYawPitchRoll(cameraRotation.Y, cameraRotation.X, cameraRotation.Z);
            Vector3 dz = new Vector3(rotation.M31, rotation.M32, rotation.M33);

            cameraPosition += dz * wheel * 0.003f;

            NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectPosition("Camera", cameraPosition));
        }

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
            
            HierarchyListBox.Items.Add(gameObject);

            NativeMethods.InvokeWithDllProtection(() => NativeMethods.AddObject(objectName, filename));
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
                if(!float.TryParse(PositionX.Text, out position.X) || 
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
                NativeMethods.InvokeWithDllProtection(() => NativeMethods.AddObject(gameObject.Content, gameObject.ModelName));
                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectPosition(gameObject.Content, gameObject.Position));
                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectRotation(gameObject.Content, gameObject.Rotation));
                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectScale(gameObject.Content, gameObject.Scale));
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

        private void HierarchyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ObjectToInspector();
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

            if (!hostMouseRightButtonPressed)
            {
                cameraMoveVelocity = Vector3.Zero;
            }
        }
        
    }
}
