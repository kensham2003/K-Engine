﻿/////////////////////////////////////////////
///
///  MainWindowクラス
///  
///  機能：メインウインドウの制御
/// 
/////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Numerics;

using Microsoft.WindowsAPICodePack.Shell;
using System.Reflection;

using System.Text.Json;
using System.IO;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using System.Timers;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;

using Microsoft.Build.Evaluation;

using GameEngine.GameEntity;
using GameEngine.ScriptLoading;
using GameEngine.Detail;

using Component = GameEngine.GameEntity.Component;
using System.Diagnostics;
using GameEngine.MVVM.ViewModel;
using System.Text.RegularExpressions;

namespace GameEngine
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        //メンバ変数
        #region
        //インポート関数
        [DllImport("User32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        //ビューモデル関連
        MainViewModel m_mainViewModel;

        //レンダー関連
        TimeSpan m_lastRender;
        bool m_lastVisible;

        //カーソル関連
        Point m_oldMousePosition;
        bool    m_mouseLeftButtonPressed = false;
        bool    m_mouseRightButtonPressed = false;
        Point m_mousePosition;
        Point m_newMousePosition;
        bool m_hostLeftButtonDown = false;
        GameObject m_selectedObject;

        //インスペクター関連
        bool m_inspectorIsWorldCoordinate = true;
        bool m_inspectorIsScaleLinked = false;

        //エディタカメラ関連
        Task m_task;

        //エンジン内部時間計測関連
        System.Timers.Timer m_timer;

        //ファイル監視関連
        FileSystemWatcher m_fileSystemWatcher;
        private List<string> m_filesToIgnore = new List<string>();
        DateTime m_lastWatch = DateTime.MinValue;

        //ファイル生成関連
        CSharpCompilation m_compilation;

        //シミュレート関連
        bool m_simulating = false;
        Sandbox m_sandbox;
        Loader m_loader;

        //スクリプトコンパイル関連
        Project m_scriptLibrary;
        BasicFileLogger m_logger = new BasicFileLogger();
        bool m_isSuccessfullyBuilt = true;
        bool m_slnOpening = false;

        //設定関連
        Settings m_settings;
        string m_devenvPath;

        //プロジェクトブラウザ関連
        bool m_pressedPageButton = false;
        bool m_projectBrowserLoading = true;
        string m_currentPath;
        Stack<string> m_traversedPath = new Stack<string>();
        Stack<string> m_nextPath = new Stack<string>();
        System.Collections.ObjectModel.ObservableCollection<string> m_projectBrowserItems = new System.Collections.ObjectModel.ObservableCollection<string>();
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            //ユーザ設定初期化
            m_settings = new Settings();
            m_settings.Read();
            //devenvのパスを設定
            if (m_settings.Contains("m_devenvPath"))
            {
                m_devenvPath = m_settings.GetValue("m_devenvPath");
            }
            else
            {
                //デフォルトパス
                m_devenvPath = @"C:\Program Files (x86)\Microsoft Visual Studio\2019\Community\Common7\IDE";
                m_settings.SaveString("m_devenvPath", m_devenvPath);
            }

            this.host.Loaded += new RoutedEventHandler(this.Host_Loaded);
            this.host.SizeChanged += new SizeChangedEventHandler(this.Host_SizeChanged);

            //プロジェクトウィンドウ
            {
                Assembly assembly = Assembly.GetEntryAssembly();    //実行ファイル
                string path = assembly.Location;                    //実行ファイルのパス
                string dir = System.IO.Path.GetDirectoryName(path); //実行ファイルの場所

                //ProjectBrowser.Navigate(ShellFileSystemFolder.FromFolderPath(dir));
                ProjectBrowser_Goto(dir);
            }

            //タイマー
            {
                m_timer = new System.Timers.Timer(1.0 / 30.0);
                m_timer.Elapsed += new ElapsedEventHandler(TimerUpdate);
            }

            ////ノードエディタ
            //DataContext = new MainWindowDataContext();

            //ゲームループ
            m_sandbox = new Sandbox();
            m_sandbox.InitSandbox();
            m_loader = (Loader)m_sandbox.m_appDomain.CreateInstanceAndUnwrap(typeof(Loader).Assembly.FullName, typeof(Loader).FullName);
            m_loader.InitDomain();

            //ファイルウォッチャーを初期化
            m_fileSystemWatcher = new FileSystemWatcher(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/asset", "*.cs");
            m_fileSystemWatcher.EnableRaisingEvents = true;
            m_fileSystemWatcher.IncludeSubdirectories = true;
            m_fileSystemWatcher.Changed += OnFileChanged;
            m_fileSystemWatcher.Created += OnFileCreated;
            m_fileSystemWatcher.Renamed += OnFileRenamed;
        }

        private void OnFileCreated(object sender, FileSystemEventArgs e) 
        {
            m_filesToIgnore.Add(e.Name);
        }

        private void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            //.csファイル作る時にも入るのを防止
            if (m_filesToIgnore.Contains(e.Name))
            {
                m_filesToIgnore.Remove(e.Name);
                return;
            }

            //この関数を一度に二回入るのを防止（FileSystemWatcherの既存バグ）
            DateTime lastChange = File.GetLastWriteTime(e.FullPath);
            if(lastChange == m_lastWatch) { return; }

            m_lastWatch = lastChange;

            //Sandbox AppDomainをアンロード
            string[] serializedGameObjects = m_loader.UninitDomain();
            AppDomain.Unload(m_sandbox.m_appDomain);
            m_sandbox.InitSandbox();
            m_loader = (Loader)m_sandbox.m_appDomain.CreateInstanceAndUnwrap(typeof(Loader).Assembly.FullName, typeof(Loader).FullName);
            m_loader.InitDomain();

            m_mainViewModel.ClearItem();

            bool m_isSuccessfullyBuilt = m_scriptLibrary.Build(m_logger);

            if (!m_isSuccessfullyBuilt)
            {
                SetMessages(m_logger.m_Message);
            }
            else
            {
                SetMessage("");
            }

            string dllPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/ScriptsLibrary/ScriptsLibrary.dll";
            m_loader.LoadAssembly(dllPath);
            m_loader.LoadGameObjects(serializedGameObjects);

            //インスペクターのコンポーネント情報を更新
            this.Dispatcher.Invoke(() =>
            {
                GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;
                if (inspectorObject == null) return;

                LoadComponents(inspectorObject.Name);
            });

        }

        //Visual Studio上でファイル保存する時は「.TMPファイル生成　→　旧.csファイル削除　→　.TMPファイル改名」
        //という処理を行うので「OnFileChanged」ではなく「OnFileRenamed」でファイル変更を処理
        //※他のテキストエディタの保存はそのまま「OnFileChanged」で処理
        private void OnFileRenamed(object sender, RenamedEventArgs e)
        {
            //なん段階の改名があるので最後の.cs拡張子に改名する段階だけ処理する
            string ext = System.IO.Path.GetExtension(e.FullPath);
            if(ext != ".cs") { return; }

            

            //Sandbox AppDomainをアンロード
            string[] serializedGameObjects = m_loader.UninitDomain();
            AppDomain.Unload(m_sandbox.m_appDomain);
            m_sandbox.InitSandbox();
            m_loader = (Loader)m_sandbox.m_appDomain.CreateInstanceAndUnwrap(typeof(Loader).Assembly.FullName, typeof(Loader).FullName);
            m_loader.InitDomain();

            m_mainViewModel.ClearItem();

            m_isSuccessfullyBuilt = m_scriptLibrary.Build(m_logger);

            if (!m_isSuccessfullyBuilt)
            {
                SetMessages(m_logger.m_Message);
            }
            else
            {
                SetMessage("");
            }

            string dllPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/ScriptsLibrary/ScriptsLibrary.dll";
            m_loader.LoadAssembly(dllPath);
            m_loader.LoadGameObjects(serializedGameObjects);

            //インスペクターのコンポーネント情報を更新
            this.Dispatcher.Invoke(() =>
            {
                GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;
                if (inspectorObject == null) return;

                LoadComponents(inspectorObject.Name);
            });
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
            //var a = MSBuildLocator.QueryVisualStudioInstances().ToList();
            //var vs2019 = MSBuildLocator.QueryVisualStudioInstances().Where(x => x.Name == "Visual Studio Community 2019").First();
            //if (MSBuildLocator.CanRegister)
            //{
            //    MSBuildLocator.RegisterDefaults();
            //}
            //else
            //{
            //    MSBuildLocator.RegisterInstance(vs2019);
            //}

            m_scriptLibrary = new Project(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/ScriptsLibrary/ScriptsLibrary.csproj");
            m_logger.Parameters = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/ScriptsLibrary/buildLog.txt";
            m_logger.Verbosity = Microsoft.Build.Framework.LoggerVerbosity.Normal;
            ReloadDll();

            //メインカメラを追加
            GameObject gameObject = new GameObject("MainCamera");
            HierarchyListBox.Items.Add(gameObject);
            NativeMethods.InvokeWithDllProtection(() => NativeMethods.AddMainCamera(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\EngineAssets\\model\\camera.obj"));
            NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectCanRayHit("MainCamera", false));
            m_loader.AddGameObject("MainCamera", Define.LAYER_CAMERA);
            m_loader.AddComponentToGameObject("MainCamera", "Camera");

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(DispatcherTimerTick);
            dispatcherTimer.Interval = TimeSpan.FromSeconds(120);
            dispatcherTimer.Start();
        }


        /// <summary>
        /// ロードしてから2分ごとに行う関数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DispatcherTimerTick(object sender, EventArgs e)
        {
            if (!m_simulating)
            {
                SaveFile("自動セーブ成功！");
            }
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
            if (m_lastVisible != isVisible)
            {
                m_lastVisible = isVisible;
                if (m_lastVisible)
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
            if (this.m_lastRender != args.RenderingTime)
            {
                InteropImage.RequestRender();
                this.m_lastRender = args.RenderingTime;
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
            public static extern int GetObjectCount(int layer);

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
            public static extern void AddMainCamera(string FileName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void AddBoxCollider(string ObjectName, string FileName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void AddSphereCollider(string ObjectName, string FileName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetObjectBoxColliderSize(string ObjectName, Vector3 Size);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetObjectBoxColliderRotate(string ObjectName, Vector3 Rotate);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetObjectBoxColliderOffset(string ObjectName, Vector3 Offset);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetObjectSphereColliderSize(string ObjectName, float Size);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetObjectSphereColliderOffset(string ObjectName, Vector3 Offset);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetObjectDrawFlag(string ObjectName, bool Flag);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetObjectCanRayHit(string ObjectName, bool hit);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void RemoveObject(string ObjectName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void RemoveBoxCollider(string ObjectName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void RemoveSphereCollider(string ObjectName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern bool GetMaterialTextureEnable(string ObjectName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern int GetModelSubsetNum(string ObjectName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetModelVS(string ObjectName, string FileName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetModelPS(string ObjectName, string FileName);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetScenePlaying(bool playing);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void ChangeActiveCamera();

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetCameraTargetPosition(Vector3 target);

            [DllImport("GameEngineDLL.dll", CallingConvention = CallingConvention.Cdecl)]
            public static extern void SetCameraFocusTarget(bool focus);

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

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Application.Current.MainWindow = this;
            m_mainViewModel = (MainViewModel)DataContext;
            //MessageLog.MouseLeftButtonDown += 
        }

        //===============================
        //            HOST
        //===============================

        //左クリック：レイキャストして一番近いオブジェクトを選択
        private void Host_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_hostLeftButtonDown = true;

            //マウスとホストの相対位置を取得して、C++側に送る
            Point localMousePosition = e.GetPosition(host);
            m_oldMousePosition = localMousePosition;
            double height = host.ActualHeight;
            double width = host.ActualWidth;
            localMousePosition.Y -= height / 2;
            localMousePosition.X -= width / 2;
            float x = (float)(localMousePosition.X);
            float y = (float)(localMousePosition.Y);
            float screenHeight = (float)height;
            float screenWidth = (float)width;
            string search;

            //カメラ位置からレイキャストし、最初に当たったオブジェクト名を返す
            IntPtr ptr = NativeMethods.InvokeWithDllProtection(() => NativeMethods.RaycastObject(x, y, screenHeight, screenWidth));
            search = Marshal.PtrToStringAnsi(ptr);
            NativeMethods.InvokeWithDllProtection(() => NativeMethods.FreeRaycastChar(ptr));

            //取得したオブジェクト名からオブジェクトを取得
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

        private void Host_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_hostLeftButtonDown = false;
            m_oldMousePosition = e.GetPosition(host);
        }

        //右ボタン押しながら+WASD：カメラを移動
        private void Host_MouseRightButtonDown(object sender, MouseEventArgs e)
        {
            //シミュレート中は動かせないように
            if (m_simulating)
            {
                return;
            }
            host.Focus();
            m_mouseRightButtonPressed = true;
            m_task = new Task(MoveCameraTask);
            m_task.Start();
        }

        private void Host_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void Host_MouseUp(object sender, MouseEventArgs e)
        {
            m_mouseRightButtonPressed = false;
        }

        //60fpsで移動
        private void MoveCameraTask()
        {
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (m_mouseRightButtonPressed)
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
            //シミュレート中は動かせないように
            if (m_simulating)
            {
                return;
            }
            Point mousePosition = e.GetPosition(host);
            Vector3 cameraPosition = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectPosition("Camera"));
            Vector3 cameraRotation = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation("Camera"));


            //右ボタン：カメラ方向移動
            if (e.RightButton == MouseButtonState.Pressed)
            {
                m_mouseRightButtonPressed = true;

                cameraRotation.Y += (float)(mousePosition.X - m_oldMousePosition.X) * 0.003f;
                cameraRotation.X += (float)(mousePosition.Y - m_oldMousePosition.Y) * 0.003f;

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectRotation("Camera", cameraRotation));

            }

            if(e.RightButton == MouseButtonState.Released)
            {
                m_mouseRightButtonPressed = false;
            }

            //ホイールボタン：カメラ前後移動
            if(e.MiddleButton == MouseButtonState.Pressed)
            {
                float dx = (float)(mousePosition.X - m_oldMousePosition.X) * 0.01f;
                float dy = (float)(mousePosition.Y - m_oldMousePosition.Y) * 0.01f;

                cameraPosition.X -= (float)Math.Cos(cameraRotation.Y) * dx - (float)Math.Sin(cameraRotation.Y) * (float)Math.Sin(cameraRotation.X) * dy;
                cameraPosition.Z += (float)Math.Sin(cameraRotation.Y) * dx + (float)Math.Cos(cameraRotation.Y) * (float)Math.Sin(cameraRotation.X) * dy;
                cameraPosition.Y += (float)Math.Cos(cameraRotation.X) * dy;

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectPosition("Camera", cameraPosition));
            }

            //左ボタン：選択中オブジェクトの移動
            //ダイアログをクリックなどで誤反応を防止するためトリガーしたかをチェック
            if (e.LeftButton == MouseButtonState.Pressed && m_hostLeftButtonDown)
            {
                GameObject gameObject = HierarchyListBox.SelectedItem as GameObject;

                if (gameObject == null)
                    return;

                float dx = (float)(mousePosition.X - m_oldMousePosition.X) * 0.01f;
                float dy = (float)(mousePosition.Y - m_oldMousePosition.Y) * 0.01f;

                Vector3 position = gameObject.Position;
                position.X += (float)Math.Cos(cameraRotation.Y) * dx - (float)Math.Sin(cameraRotation.Y) * (float)Math.Sin(cameraRotation.X) * dy;
                position.Z -= (float)Math.Sin(cameraRotation.Y) * dx + (float)Math.Cos(cameraRotation.Y) * (float)Math.Sin(cameraRotation.X) * dy;
                position.Y -= (float)Math.Cos(cameraRotation.X) * dy;
                gameObject.Position = position;

                m_loader.SetGameObjectPosition(gameObject.Name, position.X, position.Y, position.Z);

                ObjectToInspector();
            }

            m_oldMousePosition = mousePosition;
        }

        private void Host_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            //シミュレート中は動かせないように
            if (m_simulating)
            {
                return;
            }
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
            string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop);

            string filename = paths[0];
            //.obj以外のファイルドロップは禁止
            if(System.IO.Path.GetExtension(filename) != ".obj")
            {
                CenterMessageBox.Show(new WindowWrapper(this), ".objのみドロップできます！", "Alert", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }
            string objectName = System.IO.Path.GetFileNameWithoutExtension(filename);

            //同じモデルを入れる場合は名前を「モデル_2」のようにする
            int cnt = 1;
            for (int i = 0; i < HierarchyListBox.Items.Count; i++)
            {
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

            //C++側とゲーム処理側にオブジェクト登録
            GameObject gameObject = new GameObject(objectName);
            gameObject.ModelName = filename;
            gameObject.AddModel(filename);
            //gameObject.AddComponent(new GameEntity.testComponent(gameObject));
            //m_game.AddGameObject(gameObject, Define.LAYER_3D_OBJECT);

            m_loader.AddGameObject(objectName, filename);
            
            HierarchyListBox.Items.Add(gameObject);

            NativeMethods.InvokeWithDllProtection(() => NativeMethods.AddModel(objectName, filename));
        }


        private void Host_AddModel(string filename)
        {
            //.obj以外のファイルドロップは禁止
            if (System.IO.Path.GetExtension(filename) != ".obj")
            {
                CenterMessageBox.Show(new WindowWrapper(this), ".objのみドロップできます！", "Alert", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }
            string objectName = System.IO.Path.GetFileNameWithoutExtension(filename);

            //同じモデルを入れる場合は名前を「モデル_2」のようにする
            int cnt = 1;
            for (int i = 0; i < HierarchyListBox.Items.Count; i++)
            {
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

            //C++側とゲーム処理側にオブジェクト登録
            GameObject gameObject = new GameObject(objectName);
            gameObject.ModelName = filename;
            gameObject.AddModel(filename);
            //gameObject.AddComponent(new GameEntity.testComponent(gameObject));
            //m_game.AddGameObject(gameObject, Define.LAYER_3D_OBJECT);

            m_loader.AddGameObject(objectName, filename);

            HierarchyListBox.Items.Add(gameObject);

            NativeMethods.InvokeWithDllProtection(() => NativeMethods.AddModel(objectName, filename));
        }
        

        //=====================
        //       MENU
        //=====================

        private void MenuItem_Open_Click(object sender, RoutedEventArgs e)
        {
            m_loader.RemoveAllGameObjects(Define.LAYER_3D_OBJECT);

            JsonSerializerOptions options = new JsonSerializerOptions()
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                WriteIndented = true,
                IncludeFields = true,
            };
            string fileName = "TestScene.json";
            string jsonString = File.ReadAllText(fileName);
            string[] jsonStrings = new string[Define.NUM_LAYER];
            for(int i = 0; i < Define.NUM_LAYER; i++)
            {
                jsonStrings[i] = "[]";
            }
            jsonStrings[Define.LAYER_3D_OBJECT] = jsonString;
            List<GameObject> gameObjects = JsonSerializer.Deserialize<List<GameObject>>(jsonString, options);

            foreach (GameObject gameObject in gameObjects)
            {
                if(gameObject.Name == "MainCamera") { continue; }
                HierarchyListBox.Items.Add(gameObject);
                if(gameObject.ModelName != null)
                {
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.AddModel(gameObject.Name, gameObject.ModelName));
                }
                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectPosition(gameObject.Name, gameObject.Position));
                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectRotation(gameObject.Name, gameObject.Rotation));
                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectScale(gameObject.Name, gameObject.Scale));
            }
            m_loader.LoadGameObjects(jsonStrings);
        }

        private void MenuItem_Save_Click(object sender, RoutedEventArgs e)
        {
            var confirmWindow = new confirmWindow("セーブしますか？");
            confirmWindow.Owner = GetWindow(this);
            if (confirmWindow.ShowDialog() == true)
            {
                if (confirmWindow.m_isConfirm)
                {
                    SaveFile("セーブ成功！");
                }
            }
        }


        /// <summary>
        /// 現在の情報をファイルにセーブ（TestScene.json）、メインカメラ含まない
        /// </summary>
        /// <param name="successMsg">成功したらデバッグログに出すメッセージ</param>
        private void SaveFile(string successMsg)
        {
            string fileName = "TestScene.json";
            string jsonString = m_loader.Serialize(Define.LAYER_3D_OBJECT);
            File.WriteAllText(fileName, jsonString);
            SetMessage(successMsg);
        }

        private void MenuItem_Simulate_Play_Click(object sender, RoutedEventArgs e)
        {
            if (!m_isSuccessfullyBuilt)
            {
                CenterMessageBox.Show(new WindowWrapper(this), "ビルドエラーを修正してからシミュレートしてください。", "Alert", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }
            MenuItem_SimulatePlay.Visibility = Visibility.Collapsed;
            MenuItem_SimulateStop.Visibility = Visibility.Visible;
            m_loader.Play();
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

            //インスペクターの位置情報を更新
            while (m_simulating)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    this.Dispatcher.Invoke(() =>
                    {
                        //Input.Keyboard.UpdateKeyState();
                        //デバッグログを取得
                        List<string> debugMessage = m_loader.GetDebugMessage();
                        if (debugMessage.Count() > 0)
                        {
                            //MessageLog.Content = debugMessage.Last();
                            m_loader.ClearDebugLog();
                            SetMessages(debugMessage);
                        }

                        GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;
                        if (inspectorObject == null) return;

                        Vector3 Pos = m_loader.GetGameObjectPosition(inspectorObject.Name);
                        PositionX.Text = Pos.X.ToString("F2");
                        PositionY.Text = Pos.Y.ToString("F2");
                        PositionZ.Text = Pos.Z.ToString("F2");

                        Vector3 Rot = m_loader.GetGameObjectRotation(inspectorObject.Name);
                        RotationX.Text = Rot.X.ToString("F2");
                        RotationY.Text = Rot.Y.ToString("F2");
                        RotationZ.Text = Rot.Z.ToString("F2");

                        Vector3 Scl = m_loader.GetGameObjectScale(inspectorObject.Name);
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
            m_simulating = false;
            m_loader.Stop();
            foreach(GameObject gameObject in HierarchyListBox.Items)
            {
                ObjectToInterop(gameObject);
            }
            ObjectToInspector();
        }

        private void MenuItem_PathSettings_Click(object sender, RoutedEventArgs e)
        {
            //パス設定のダイアログを表示
            var dialog = new pathSettingWindow(m_devenvPath);
            dialog.Owner = GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                m_devenvPath = dialog.m_devenvPath;
                m_settings.SaveString("m_devenvPath", m_devenvPath);
            }
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

            //if (!mouseRightButtonPressed)
            //{
            //    cameraMoveVelocity = Vector3.Zero;
            //}
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
                m_loader.SetGameObjectPosition(objectName, gameObject.Position.X, gameObject.Position.Y, gameObject.Position.Z);
            }
            {
                Vector3 rotation = gameObject.Rotation / (float)Math.PI * 180.0f; //Radian->Degree

                RotationX.Text = rotation.X.ToString("F2");
                RotationY.Text = rotation.Y.ToString("F2");
                RotationZ.Text = rotation.Z.ToString("F2");

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectRotation(objectName, gameObject.Rotation));
                m_loader.SetGameObjectRotation(objectName, gameObject.Rotation.X, gameObject.Rotation.Y, gameObject.Rotation.Z);
            }
            {
                ScaleX.Text = gameObject.Scale.X.ToString("F2");
                ScaleY.Text = gameObject.Scale.Y.ToString("F2");
                ScaleZ.Text = gameObject.Scale.Z.ToString("F2");

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectScale(objectName, gameObject.Scale));
                m_loader.SetGameObjectScale(objectName, gameObject.Scale.X, gameObject.Scale.Y, gameObject.Scale.Z);

            }
        }


        /// <summary>
        /// オブジェクトの位置情報を描画や処理サイドに送る
        /// </summary>
        /// <param name="gameObject">対象ゲームオブジェクト</param>
        private void ObjectToInterop(GameObject gameObject)
        {
            if (gameObject == null)
                return;

            string objectName = gameObject.ToString();
            {
                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectPosition(objectName, gameObject.Position));
                m_loader.SetGameObjectPosition(objectName, gameObject.Position.X, gameObject.Position.Y, gameObject.Position.Z);
            }
            {
                Vector3 rotation = gameObject.Rotation / (float)Math.PI * 180.0f; //Radian->Degree

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectRotation(objectName, gameObject.Rotation));
                m_loader.SetGameObjectRotation(objectName, gameObject.Rotation.X, gameObject.Rotation.Y, gameObject.Rotation.Z);
            }
            {
                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectScale(objectName, gameObject.Scale));
                m_loader.SetGameObjectScale(objectName, gameObject.Scale.X, gameObject.Scale.Y, gameObject.Scale.Z);

            }
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
                gameObject.Position = position;

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectPosition(objectName, gameObject.Position));
                m_loader.SetGameObjectPosition(objectName, position.X, position.Y, position.Z);
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
                gameObject.Rotation = rotation;

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectRotation(objectName, gameObject.Rotation));
                m_loader.SetGameObjectRotation(objectName, rotation.X, rotation.Y, rotation.Z);
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
                gameObject.Scale = scale;

                NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectScale(objectName, gameObject.Scale));
                m_loader.SetGameObjectScale(objectName, scale.X, scale.Y, scale.Z);
            }
            //gameObject.Script = ScriptTextBox.Text;
        }

        //=================================
        //      INSPECTOR VISIBILITY
        //=================================

        /// <summary>
        /// インスペクター情報を非表示
        /// </summary>
        private void HideInspector()
        {
            Inspector_StackPanel.Visibility = Visibility.Collapsed;
        }


        /// <summary>
        /// インスペクター情報を表示
        /// </summary>
        private void ShowInspector()
        {
            Inspector_StackPanel.Visibility = Visibility.Visible;

            Inspector_Position_DockPanel.Visibility = Visibility.Collapsed;
            Inspector_Rotation_DockPanel.Visibility = Visibility.Collapsed;
            Inspector_Scale_DockPanel.Visibility = Visibility.Collapsed;

            GameObject gameObject = HierarchyListBox.SelectedItem as GameObject;
            Inspector_Name.Text = gameObject.ToString();
            Model_Lighting.IsChecked = gameObject.HasLighting;
            Model_Ray.IsChecked = gameObject.CanRayHit;

            Component_Panel.Children.Clear();
            LoadComponents(gameObject.Name);
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

            m_loader.RenameObject(objectName, Inspector_Name.Text);

            gameObject.Name = Inspector_Name.Text;

            HierarchyListBox.Items.Refresh();
        }

        //=====================================
        //         INSPECTOR POSITION
        //=====================================

        private void Inspector_Position_X_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_mousePosition = PointToScreen(Mouse.GetPosition(this));
            m_mouseLeftButtonPressed = true;
            m_selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(MoveObjectTaskX));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void Inspector_Position_Y_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_mousePosition = PointToScreen(Mouse.GetPosition(this));
            m_mouseLeftButtonPressed = true;
            m_selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(MoveObjectTaskY));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void Inspector_Position_Z_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_mousePosition = PointToScreen(Mouse.GetPosition(this));
            m_mouseLeftButtonPressed = true;
            m_selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(MoveObjectTaskZ));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }


        private void MoveObjectTaskX()
        {
            string objectName = m_selectedObject.ToString();
            Vector3 vecX = new Vector3(1.0f, 0.0f, 0.0f);

            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (m_mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        m_newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (m_newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && m_mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos(1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = 1;
                            m_mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (m_newMousePosition.X <= 0 && m_mousePosition.X > 0)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            m_mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = m_newMousePosition.X - m_mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    if (m_inspectorIsWorldCoordinate)
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
                        m_loader.SetGameObjectPosition(objectName, Pos.X, Pos.Y, Pos.Z);
                    });

                    m_mousePosition = m_newMousePosition;
                    now = DateTime.Now;
                }
            }
        }


        private void MoveObjectTaskY()
        {
            string objectName = m_selectedObject.ToString();
            Vector3 vecY = new Vector3(0.0f, 1.0f, 0.0f);

            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (m_mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        m_newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (m_newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && m_mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos(1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = 1;
                            m_mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (m_newMousePosition.X <= 0 && m_mousePosition.X > 0)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            m_mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = m_newMousePosition.X - m_mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    if (m_inspectorIsWorldCoordinate)
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
                        m_loader.SetGameObjectPosition(objectName, Pos.X, Pos.Y, Pos.Z);
                    });

                    m_mousePosition = m_newMousePosition;
                    now = DateTime.Now;
                }
            }
        }


        private void MoveObjectTaskZ()
        {
            string objectName = m_selectedObject.ToString();
            Vector3 vecZ = new Vector3(0.0f, 0.0f, 1.0f);

            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (m_mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        m_newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (m_newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && m_mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos(1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = 1;
                            m_mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (m_newMousePosition.X <= 0 && m_mousePosition.X > 0)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            m_mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = m_newMousePosition.X - m_mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    if (m_inspectorIsWorldCoordinate)
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
                        m_loader.SetGameObjectPosition(objectName, Pos.X, Pos.Y, Pos.Z);
                    });

                    m_mousePosition = m_newMousePosition;
                    now = DateTime.Now;
                }
            }
        }

        private void Inspector_Position_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_mouseLeftButtonPressed = false;
            GameObject gameObject = HierarchyListBox.SelectedItem as GameObject;
            Vector3 Pos = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectPosition(gameObject.ToString()));
            gameObject.Position = Pos;
        }

        private void Inspector_Change_World_Local(object sender, RoutedEventArgs e)
        {
            if (m_inspectorIsWorldCoordinate)
            {
                m_inspectorIsWorldCoordinate = false;
                Inspector_Coordinate_Button.Content = "ローカル";
            }
            else
            {
                m_inspectorIsWorldCoordinate = true;
                Inspector_Coordinate_Button.Content = "ワールド";
            }
        }

        //=====================================
        //         INSPECTOR ROTATION
        //=====================================

        private void Inspector_Rotation_X_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_mousePosition = PointToScreen(Mouse.GetPosition(this));
            m_mouseLeftButtonPressed = true;
            m_selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(RotateObjectTaskX));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void Inspector_Rotation_Y_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_mousePosition = PointToScreen(Mouse.GetPosition(this));
            m_mouseLeftButtonPressed = true;
            m_selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(RotateObjectTaskY));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void Inspector_Rotation_Z_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_mousePosition = PointToScreen(Mouse.GetPosition(this));
            m_mouseLeftButtonPressed = true;
            m_selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(RotateObjectTaskZ));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void RotateObjectTaskX()
        {
            string objectName = m_selectedObject.ToString();
            Vector3 vecX = new Vector3(1.0f, 0.0f, 0.0f);

            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (m_mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        m_newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (m_newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && m_mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos(1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = 1;
                            m_mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (m_newMousePosition.X <= 0 && m_mousePosition.X > 0)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            m_mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = m_newMousePosition.X - m_mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.RotateObject(objectName, vecX * (float)xDiff));

                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Rot = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation(objectName));
                        RotationX.Text = Rot.X.ToString("F2");
                        m_loader.SetGameObjectRotation(objectName, Rot.X, Rot.Y, Rot.Z);
                    });

                    m_mousePosition = m_newMousePosition;
                    now = DateTime.Now;
                }
            }
        }

        private void RotateObjectTaskY()
        {
            string objectName = m_selectedObject.ToString();
            Vector3 vecY = new Vector3(0.0f, 1.0f, 0.0f);

            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (m_mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        m_newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (m_newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && m_mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos(1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = 1;
                            m_mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (m_newMousePosition.X <= 0 && m_mousePosition.X > 0)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            m_mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = m_newMousePosition.X - m_mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.RotateObject(objectName, vecY * (float)xDiff));

                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Rot = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation(objectName));
                        RotationY.Text = Rot.Y.ToString("F2");
                        m_loader.SetGameObjectRotation(objectName, Rot.X, Rot.Y, Rot.Z);
                    });

                    m_mousePosition = m_newMousePosition;
                    now = DateTime.Now;
                }
            }
        }
        
        private void RotateObjectTaskZ()
        {
            string objectName = m_selectedObject.ToString();
            Vector3 vecZ = new Vector3(0.0f, 0.0f, 1.0f);

            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (m_mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        m_newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (m_newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && m_mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos(1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = 1;
                            m_mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (m_newMousePosition.X <= 0 && m_mousePosition.X > 0)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            m_mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = m_newMousePosition.X - m_mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.RotateObject(objectName, vecZ * (float)xDiff));

                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Rot = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation(objectName));
                        RotationZ.Text = Rot.Z.ToString("F2");
                        m_loader.SetGameObjectRotation(objectName, Rot.X, Rot.Y, Rot.Z);
                    });

                    m_mousePosition = m_newMousePosition;
                    now = DateTime.Now;
                }
            }
        }

        private void Inspector_Rotation_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_mouseLeftButtonPressed = false;
            GameObject gameObject = HierarchyListBox.SelectedItem as GameObject;
            Vector3 Rot = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectRotation(gameObject.ToString()));
            gameObject.Rotation = Rot;
        }

        //=====================================
        //         INSPECTOR SCALE
        //=====================================

        private void Inspector_Scale_X_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_mousePosition = PointToScreen(Mouse.GetPosition(this));
            m_mouseLeftButtonPressed = true;
            m_selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(ScaleObjectTaskX));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void Inspector_Scale_Y_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_mousePosition = PointToScreen(Mouse.GetPosition(this));
            m_mouseLeftButtonPressed = true;
            m_selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(ScaleObjectTaskY));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void Inspector_Scale_Z_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            m_mousePosition = PointToScreen(Mouse.GetPosition(this));
            m_mouseLeftButtonPressed = true;
            m_selectedObject = HierarchyListBox.SelectedItem as GameObject;
            var th = new Thread(new ThreadStart(ScaleObjectTaskZ));
            th.SetApartmentState(ApartmentState.STA);
            th.Start();
        }

        private void ScaleObjectTaskX()
        {
            string objectName = m_selectedObject.ToString();
            Vector3 vecX = new Vector3(1.0f, 0.0f, 0.0f);
            if (m_inspectorIsScaleLinked)
            {
                vecX = new Vector3(1.0f, 1.0f, 1.0f);
            }
            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (m_mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        m_newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (m_newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && m_mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos(1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = 1;
                            m_mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (m_newMousePosition.X <= 0 && m_mousePosition.X > 0)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            m_mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = m_newMousePosition.X - m_mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.ScaleObject(objectName, vecX * (float)xDiff));

                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Scl = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectScale(objectName));
                        ScaleX.Text = Scl.X.ToString("F2");
                        ScaleY.Text = Scl.Y.ToString("F2");
                        ScaleZ.Text = Scl.Z.ToString("F2");
                        m_loader.SetGameObjectScale(objectName, Scl.X, Scl.Y, Scl.Z);
                    });

                    m_mousePosition = m_newMousePosition;
                    now = DateTime.Now;
                }
            }
        }

        private void ScaleObjectTaskY()
        {
            string objectName = m_selectedObject.ToString();
            Vector3 vecY = new Vector3(0.0f, 1.0f, 0.0f);
            if (m_inspectorIsScaleLinked)
            {
                vecY = new Vector3(1.0f, 1.0f, 1.0f);
            }
            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (m_mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        m_newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (m_newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && m_mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos(1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = 1;
                            m_mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (m_newMousePosition.X <= 0 && m_mousePosition.X > 0)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            m_mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = m_newMousePosition.X - m_mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.ScaleObject(objectName, vecY * (float)xDiff));

                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Scl = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectScale(objectName));
                        ScaleX.Text = Scl.X.ToString("F2");
                        ScaleY.Text = Scl.Y.ToString("F2");
                        ScaleZ.Text = Scl.Z.ToString("F2");
                        m_loader.SetGameObjectScale(objectName, Scl.X, Scl.Y, Scl.Z);
                    });

                    m_mousePosition = m_newMousePosition;
                    now = DateTime.Now;
                }
            }
        }

        private void ScaleObjectTaskZ()
        {
            string objectName = m_selectedObject.ToString();
            Vector3 vecZ = new Vector3(0.0f, 0.0f, 1.0f);
            if (m_inspectorIsScaleLinked)
            {
                vecZ = new Vector3(1.0f, 1.0f, 1.0f);
            }
            double xDiff;

            //60fps
            DateTime now = DateTime.Now;
            TimeSpan interval = TimeSpan.FromSeconds(1.0f / 60);
            while (m_mouseLeftButtonPressed)
            {
                if (DateTime.Now.Subtract(now) > interval)
                {
                    //マウス座標を取得
                    this.Dispatcher.Invoke(() => {
                        m_newMousePosition = PointToScreen(Mouse.GetPosition(this));
                        //右いっぱいになったら左に移動
                        if (m_newMousePosition.X >= System.Windows.SystemParameters.PrimaryScreenWidth - 1 && m_mousePosition.X < System.Windows.SystemParameters.PrimaryScreenWidth - 1)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos(1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = 1;
                            m_mousePosition.X = 1 - diff;
                        }
                        //左いっぱいになったら右に移動
                        else if (m_newMousePosition.X <= 0 && m_mousePosition.X > 0)
                        {
                            double diff = m_newMousePosition.X - m_mousePosition.X;
                            SetCursorPos((int)System.Windows.SystemParameters.PrimaryScreenWidth - 1, (int)m_newMousePosition.Y);
                            m_newMousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1;
                            m_mousePosition.X = System.Windows.SystemParameters.PrimaryScreenWidth - 1 - diff;
                        }
                    });

                    xDiff = m_newMousePosition.X - m_mousePosition.X;
                    //スケーリング、プールダウンメニューで設定可能にしたら便利かも
                    xDiff *= 0.01f;
                    NativeMethods.InvokeWithDllProtection(() => NativeMethods.ScaleObject(objectName, vecZ * (float)xDiff));

                    //インスペクターの数値を変更
                    this.Dispatcher.Invoke(() => {
                        Vector3 Scl = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectScale(objectName));
                        ScaleX.Text = Scl.X.ToString("F2");
                        ScaleY.Text = Scl.Y.ToString("F2");
                        ScaleZ.Text = Scl.Z.ToString("F2");
                        m_loader.SetGameObjectScale(objectName, Scl.X, Scl.Y, Scl.Z);
                    });

                    m_mousePosition = m_newMousePosition;
                    now = DateTime.Now;
                }
            }
        }

        private void Inspector_Scale_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            m_mouseLeftButtonPressed = false;
            GameObject gameObject = HierarchyListBox.SelectedItem as GameObject;
            Vector3 Scl = NativeMethods.InvokeWithDllProtection(() => NativeMethods.GetObjectScale(gameObject.ToString()));
            gameObject.Scale = Scl;
        }

        private void Inspector_Change_Scale_Link(object sender, RoutedEventArgs e)
        {
            if (m_inspectorIsScaleLinked)
            {
                m_inspectorIsScaleLinked = false;
                Inspector_Scale_Button.Content = "Not Linked";
            }
            else
            {
                m_inspectorIsScaleLinked = true;
                Inspector_Scale_Button.Content = "Linked";
            }
        }

        //===========================================
        //               FUNCTIONAL
        //===========================================


        public void SetMessage(string message)
        {
            this.Dispatcher.Invoke(() =>
            {
                MessageLog.Content = message;
            });
            m_mainViewModel.AddItem(message);
        }

        public void SetMessages(List<string> messages)
        {
            m_mainViewModel.AddItem(messages);
            this.Dispatcher.Invoke(() =>
            {
                MessageLog.Content = m_mainViewModel.GetLastItem();
            });
        }

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

        //新しいスクリプトを生成、ロードする
        private void Add_Component(object sender, RoutedEventArgs e)
        {
            string className = "";

            //スクリプト名を入力するウインドウを起動
            //var dialog = new userInputDialog();
            List<string> scriptsList = m_loader.GetScriptsList();
            List<string> componentsList = new List<string>();
            componentsList.AddRange(scriptsList);
            componentsList.AddRange(Define.preDefinedComponents);

            var dialog = new userInputDialog(componentsList);
            dialog.Owner = GetWindow(this);
            if (dialog.ShowDialog() == true)
            {
                className = dialog.InputText;
                //入力文字列のスペースを無視
                className = Regex.Replace(className, @"\s+", "");
            }
            else { return; }

            //テンプレート.csファイルを生成
            string upperClassName = className[0].ToString().ToUpper() + className.Substring(1);

            //プリデファインドコンポーネントなら新しく作らずコンポーネントをそのまま追加する
            foreach(string componentName in Define.preDefinedComponents)
            {
                if(componentName == upperClassName)
                {
                    //選択中のゲームオブジェクトに作成されたスクリプトを追加
                    GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;
                    //重複しているスクリプトの場合は追加しない
                    if (m_loader.IsObjectContainingScript(inspectorObject.Name, upperClassName))
                    {
                        //MessageBox.Show(Define.AddSpacesToString(upperClassName) + "は既に" + inspectorObject.Name + "に存在しています。", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                        CenterMessageBox.Show(new WindowWrapper(this), Define.AddSpacesToString(upperClassName) + "は既に" + inspectorObject.Name + "に存在しています。", "Alert", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                        return;
                    }

                    if(m_loader.AddComponentToGameObject(inspectorObject.Name, upperClassName) == false)
                    {
                        //MessageBox.Show(inspectorObject.Name + "は既にコライダーを持っています。", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                        //MessageBoxEx.Show(inspectorObject.Name + "は既にコライダーを持っています。", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                        //MessageBoxEx.Show(this, inspectorObject.Name + "は既にコライダーを持っています。",)
                        CenterMessageBox.Show(new WindowWrapper(this), inspectorObject.Name + "は既にコライダーを持っています。", "Alert", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);

                        return;
                    }

                    //インスペクターへ反映
                    LoadComponents(inspectorObject.Name);

                    return;
                }
            }

            //リストにある名前なら新しく作らずコンポーネントをそのまま追加する
            foreach (string scriptName in scriptsList)
            {
                if(scriptName == upperClassName)
                {
                    //選択中のゲームオブジェクトに作成されたスクリプトを追加
                    GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;
                    //重複しているスクリプトの場合は追加しない
                    if(m_loader.IsObjectContainingScript(inspectorObject.Name, upperClassName))
                    {
                        //MessageBox.Show(Define.AddSpacesToString(upperClassName) + "は既に" + inspectorObject.Name + "に存在しています。", "Alert", MessageBoxButton.OK, MessageBoxImage.Error);
                        CenterMessageBox.Show(new WindowWrapper(this), Define.AddSpacesToString(upperClassName) + "は既に" + inspectorObject.Name + "に存在しています。", "Alert", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                        return;
                    }
                    string scriptPath = System.IO.Path.GetFullPath("asset/" + className + ".cs");
                    m_loader.AddScriptToGameObject(inspectorObject.Name, upperClassName, scriptPath);

                    //インスペクターへ反映
                    LoadComponents(inspectorObject.Name);

                    return;
                }
            }
            string classDll = "asset/" + className + ".dll";
            string path = "asset/" + className + ".cs";
            string code = $@"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Windows.Forms;
using GameEngine.Detail;


namespace GameEngine.GameEntity
{{
    [Serializable]
    class {upperClassName} : GameScript
    {{
        public {upperClassName}() {{ }}

        public {upperClassName}(GameObject gameObject) : base(gameObject) {{ }}

        public float moveAmount = 0.1f;

        public bool stopMoving = true;

        public override void BeginPlay()
        {{
            
        }}

        public override void Update(TimeSpan gameTime)
        {{
            if(stopMoving){{return;}}
            Vector3 pos = Parent.Position;
            if(pos.X > 3 || pos.X < -3){{moveAmount *= -1;}}
            
            pos.X += moveAmount;
            Parent.Position = pos;
            Debug.Log(pos);
        }}
    }}
}}
";
            File.WriteAllText(path, code);

            //作った.csをロード
            var parsedSyntaxTree = Parse(code, "", CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp8));

            m_compilation = CSharpCompilation.Create(className, new SyntaxTree[] { parsedSyntaxTree }, DefaultReferences, DefaultCompilationOptions);

            //.dllとして出力
            var emitResult = m_compilation.Emit(classDll);

            //コンパイルエラーメッセージ
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

            //コンパイル成功
            if (emitResult.Success)
            {
                ReloadDll();

                //選択中のゲームオブジェクトに作成されたスクリプトを追加
                GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;
                string scriptPath = System.IO.Path.GetFullPath(path);
                m_loader.AddScriptToGameObject(inspectorObject.Name, upperClassName, scriptPath);

                //インスペクターへ反映
                LoadComponents(inspectorObject.Name);
            }
        }

        //スクリプトコンパイル用
        private static readonly IEnumerable<string> DefaultNamespaces =
            new[]
            {
                "System",
                "System.Collections.Generic",
                "System.Linq",
                "System.Text",
                "System.Numerics",
                "System.Threading.Tasks",
                "System.Windows.Forms",
                "GameEngine.GameEntity"
            };
        //スクリプトコンパイル用
        private static string runtimePath = @"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.1\{0}.dll";
        //スクリプトコンパイル用
        private static readonly IEnumerable<MetadataReference> DefaultReferences =
            new[]
            {
                MetadataReference.CreateFromFile(string.Format(runtimePath, "mscorlib")),
                MetadataReference.CreateFromFile(string.Format(runtimePath, "System")),
                MetadataReference.CreateFromFile(string.Format(runtimePath, "System.Core")),
                MetadataReference.CreateFromFile(string.Format(runtimePath, "System.Numerics")),
                MetadataReference.CreateFromFile(string.Format(runtimePath, "System.Windows.Forms")),
                //MetadataReference.CreateFromFile("CoreModule.dll")
                MetadataReference.CreateFromFile(typeof(Component).Assembly.Location),
            };
        //スクリプトコンパイル用
        private static readonly CSharpCompilationOptions DefaultCompilationOptions =
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOverflowChecks(true).WithOptimizationLevel(OptimizationLevel.Release)
                    .WithUsings(DefaultNamespaces);

        public SyntaxTree Parse(string text, string filename = "", CSharpParseOptions options = null)
        {
            var stringText = SourceText.From(text, Encoding.UTF8);
            return SyntaxFactory.ParseSyntaxTree(stringText, options, filename);
        }


        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            m_simulating = false;
            m_settings.Save();
        }

        /// <summary>
        /// 「/asset」の中にあるDllファイルを全部リロード
        /// </summary>
        public void ReloadDll()
        {
            //m_loaderを再作成（AppDomain内のアセンブリ（dll情報）を更新するため）
            string[] serializedGameObjects = m_loader.UninitDomain();
            AppDomain.Unload(m_sandbox.m_appDomain);
            m_sandbox.InitSandbox();
            m_loader = (Loader)m_sandbox.m_appDomain.CreateInstanceAndUnwrap(typeof(Loader).Assembly.FullName, typeof(Loader).FullName);
            m_loader.InitDomain();

            //assetフォルダの中の全て.csファイルをScriptLibraryソリューションに入れる
            string directory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/asset";
            string[] csFiles = System.IO.Directory.GetFiles(directory, "*.cs", SearchOption.AllDirectories);
            var items = m_scriptLibrary.GetItems("Compile");
            //元のスクリプト情報を全部消して、最新の情報を登録
            m_scriptLibrary.RemoveItems(items);
            //アセンブリ情報ファイルであり、参照が必要
            m_scriptLibrary.AddItem("Compile", System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/ScriptsLibrary/Properties/AssemblyInfo.cs");
            foreach (string cs in csFiles)
            {
                m_scriptLibrary.AddItem("Compile", cs);
            }
            m_scriptLibrary.Save();

            m_mainViewModel.ClearItem();
            //ScriptLibraryソリューションをビルド
            //（VSでエンジンを起動してビルドすると失敗する可能性がある）
            //（→.exeを起動するのが推奨）
            m_isSuccessfullyBuilt = m_scriptLibrary.Build(m_logger);

            if (!m_isSuccessfullyBuilt)
            {
                SetMessages(m_logger.m_Message);
            }
            else
            {
                SetMessage("");
            }
            //ビルドした.dllをロード
            string dllPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/ScriptsLibrary/ScriptsLibrary.dll";
            m_loader.LoadAssembly(dllPath);

            m_loader.LoadGameObjects(serializedGameObjects);
        }

        /// <summary>
        /// ゲームオブジェクトのスクリプトをインスペクターへ反映する
        /// </summary>
        public void LoadComponents(string gameObjectName)
        {
            List<string> scriptNames = m_loader.GetScriptsName(gameObjectName);
            List<string> scriptPaths = m_loader.GetScriptsPath(gameObjectName);
            List<ComponentPropInfo> propInfos = m_loader.GetScriptsPropInfos(gameObjectName);
            List<ComponentPropInfo> fieldInfos = m_loader.GetScriptsFieldInfos(gameObjectName);

            int scriptCount = 0;

            Component_Panel.Children.Clear();

            for (int i = 0; i < scriptNames.Count(); i++)
            {
                if(scriptPaths[i] != "")
                {
                    scriptCount++;
                }
                var stackPanelTemp = new StackPanel { Orientation = Orientation.Vertical };
                string scriptName = scriptNames[i];
                //stackPanelTemp.Children.Add(new Label { Content = scriptName });
                stackPanelTemp.Children.Add(new Label { Content = Define.AddSpacesToString(scriptName) });

                var stackPanelProp = new StackPanel { Orientation = Orientation.Vertical };

                //テキストボックスの値をオブジェクトの値へ代入（ローカル関数）
                void SetValue(bool isProperty, TextBox tb, string changedName, string changedValue)
                {
                    string result = m_loader.SetPropertyOrFieldValue(isProperty, gameObjectName, scriptName, changedName, changedValue);

                    //Set value失敗
                    if (result != null)
                    {
                        tb.Text = result;
                    }
                };

                //テキストボックスの値をオブジェクトの値へ代入（ローカル関数）
                void SetSVector3Value(bool isProperty, TextBox tbx, TextBox tby, TextBox tbz, string changedName)
                {
                    SVector3 changedValue = new SVector3(tbx.Text, tby.Text, tbz.Text);
                    string result = m_loader.SetPropertyOrFieldValue(isProperty, gameObjectName, scriptName, changedName, changedValue.ToString());

                    //Set value失敗
                    if (result != null)
                    {
                        SVector3 resultVector = new SVector3(result);
                        tbx.Text = resultVector.X.ToString();
                        tby.Text = resultVector.Y.ToString();
                        tbz.Text = resultVector.Z.ToString();
                    }
                };

                void SetGameObjectValue(bool isProperty, ComboBox cb, string changedName)
                {
                    string changedValue = cb.SelectedItem.ToString();
                    string result = m_loader.SetPropertyOrFieldValue(isProperty, gameObjectName, scriptName, changedName, changedValue);

                    //Set value失敗
                    if(result != null)
                    {
                        foreach(object o in cb.Items)
                        {
                            if(o.ToString() == result)
                            {
                                cb.SelectedItem = o;
                            }
                        }
                    }
                }

                //チェックボックスの値をオブジェクトの値へ代入（ローカル関数）
                //（ローカル関数はオーバーロードがサポートされていない）
                void SetValueBool(bool isProperty, CheckBox cb, string changedName, string changedValue)
                {
                    string result = m_loader.SetPropertyOrFieldValue(isProperty, gameObjectName, scriptName, changedName, changedValue);

                    //Set value失敗
                    if (result != null)
                    {
                        cb.IsChecked = bool.Parse(result);
                    }
                };

                for (int j = 0; j < propInfos[i].PropAmount; j++) {
                    string propName = propInfos[i].PropNames[j];
                    if (propName == "FilePath" || propName == "Name") continue;
                    var stackPanelOneProp = new StackPanel { Orientation = Orientation.Horizontal };
                    stackPanelOneProp.Children.Add(new Label {
                        Content = propInfos[i].PropNames[j] ,
                        MinWidth = 70
                    });
                    switch (propInfos[i].PropTypes[j])
                    {
                        //bool型はチェックボックスで表示
                        //（定数ではないと直接スイッチ文に入れられないのでこの書き方に）
                        case string value when value == typeof(bool).AssemblyQualifiedName:
                            CheckBox propInputBox = new CheckBox();
                            propInputBox.IsChecked = bool.Parse(propInfos[i].PropValues[j]);
                            propInputBox.Checked += (object sender, RoutedEventArgs e) =>
                            {
                                SetValueBool(true, propInputBox, propName, propInputBox.IsChecked.ToString());
                            };
                            propInputBox.Unchecked += (object sender, RoutedEventArgs e) =>
                            {
                                SetValueBool(true, propInputBox, propName, propInputBox.IsChecked.ToString());
                            };
                            propInputBox.VerticalAlignment = VerticalAlignment.Center;
                            stackPanelOneProp.Children.Add(propInputBox);
                            stackPanelProp.Children.Add(stackPanelOneProp);
                            break;

                        case string value when value == typeof(SVector3).AssemblyQualifiedName:
                            StackPanel vectorPanel = new StackPanel { Orientation = Orientation.Horizontal };
                            SVector3 propValue = new SVector3(propInfos[i].PropValues[j]);

                            vectorPanel.Children.Add(new Label { Content = "X" });
                            TextBox xInputField = new TextBox { Width = 30 };
                            xInputField.Text = propValue.X.ToString();
                            xInputField.Tag = false;
                            vectorPanel.Children.Add(xInputField);

                            vectorPanel.Children.Add(new Label { Content = "Y" });
                            TextBox yInputField = new TextBox { Width = 30 };
                            yInputField.Text = propValue.Y.ToString();
                            yInputField.Tag = false;
                            vectorPanel.Children.Add(yInputField);

                            vectorPanel.Children.Add(new Label { Content = "Z" });
                            TextBox zInputField = new TextBox { Width = 30 };
                            zInputField.Text = propValue.Z.ToString();
                            zInputField.Tag = false;
                            vectorPanel.Children.Add(zInputField);

                            //ENTERを押したらSetValueを呼ぶ
                            xInputField.KeyDown += (object sender, KeyEventArgs e) =>
                            {
                                if (e.Key != Key.Return)
                                    return;

                                SetSVector3Value(true, xInputField, yInputField, zInputField, propName);
                                Keyboard.ClearFocus();
                            };
                            yInputField.KeyDown += (object sender, KeyEventArgs e) =>
                            {
                                if (e.Key != Key.Return)
                                    return;

                                SetSVector3Value(true, xInputField, yInputField, zInputField, propName);
                                Keyboard.ClearFocus();
                            };
                            zInputField.KeyDown += (object sender, KeyEventArgs e) =>
                            {
                                if (e.Key != Key.Return)
                                    return;

                                SetSVector3Value(true, xInputField, yInputField, zInputField, propName);
                                Keyboard.ClearFocus();
                            };

                            //テキストボックスのフォーカスが失ってもSetValueを呼ぶ
                            xInputField.LostFocus += (object sender, RoutedEventArgs e) =>
                            {
                                if (Convert.ToBoolean(xInputField.Tag) == false) { return; }
                                SetSVector3Value(true, xInputField, yInputField, zInputField, propName);
                                xInputField.Tag = false;
                            };
                            yInputField.LostFocus += (object sender, RoutedEventArgs e) =>
                            {
                                if (Convert.ToBoolean(yInputField.Tag) == false) { return; }
                                SetSVector3Value(true, xInputField, yInputField, zInputField, propName);
                                yInputField.Tag = false;
                            };
                            zInputField.LostFocus += (object sender, RoutedEventArgs e) =>
                            {
                                if (Convert.ToBoolean(zInputField.Tag) == false) { return; }
                                SetSVector3Value(true, xInputField, yInputField, zInputField, propName);
                                zInputField.Tag = false;
                            };

                            //テキストが変わらなくてもフォーカス変更だけで勝手に更新しないように
                            xInputField.TextChanged += (object sender, TextChangedEventArgs e) =>
                            {
                                xInputField.Tag = true;
                            };
                            yInputField.TextChanged += (object sender, TextChangedEventArgs e) =>
                            {
                                yInputField.Tag = true;
                            };
                            zInputField.TextChanged += (object sender, TextChangedEventArgs e) =>
                            {
                                zInputField.Tag = true;
                            };

                            stackPanelOneProp.Children.Add(vectorPanel);
                            stackPanelProp.Children.Add(stackPanelOneProp);

                            break;

                        case string value when value == typeof(GameObject).AssemblyQualifiedName:
                            ComboBox propInputComboBox = new ComboBox() { MinWidth = 30 };
                            foreach(object o in HierarchyListBox.Items)
                            {
                                propInputComboBox.Items.Add(o);
                            }
                            propInputComboBox.SelectedIndex = propInputComboBox.Items.Add("null");
                            foreach(GameObject obj in HierarchyListBox.Items)
                            {
                                if(obj.Name == propInfos[i].PropValues[j])
                                {
                                    propInputComboBox.SelectedItem = obj;
                                    break;
                                }
                            }
                            propInputComboBox.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
                            {
                                SetGameObjectValue(true, propInputComboBox, propName);
                            };
                            stackPanelOneProp.Children.Add(propInputComboBox);
                            stackPanelProp.Children.Add(stackPanelOneProp);
                            break;

                        default:
                            TextBox propInputField = new TextBox() { MinWidth = 30 };
                            propInputField.Text = propInfos[i].PropValues[j];

                            //ENTERを押したらSetValueを呼ぶ
                            propInputField.KeyDown += (object sender, KeyEventArgs e) =>
                            {
                                if (e.Key != Key.Return)
                                    return;

                                SetValue(true, propInputField, propName, propInputField.Text);
                                Keyboard.ClearFocus();
                            };

                            //テキストボックスのフォーカスが失ってもSetValueを呼ぶ
                            propInputField.LostFocus += (object sender, RoutedEventArgs e) =>
                            {
                                SetValue(true, propInputField, propName, propInputField.Text);
                            };

                            stackPanelOneProp.Children.Add(propInputField);
                            stackPanelProp.Children.Add(stackPanelOneProp);
                            break;
                    }

                }
                stackPanelTemp.Children.Add(stackPanelProp);

                var stackPanelField = new StackPanel { Orientation = Orientation.Vertical };
                for (int k = 0; k < fieldInfos[i].PropAmount; k++)
                {
                    string fieldName = fieldInfos[i].PropNames[k];
                    var stackPanelOneField = new StackPanel {
                        Orientation = Orientation.Horizontal
                    };
                    stackPanelOneField.Children.Add(new Label {
                        Content = fieldInfos[i].PropNames[k] ,
                        MinWidth = 70
                    });
                    switch (fieldInfos[i].PropTypes[k])
                    {
                        //bool型はチェックボックスで表示
                        //（定数ではないと直接スイッチ文に入れられないのでこの書き方に）
                        case string value when value == typeof(bool).AssemblyQualifiedName:
                            CheckBox fieldInputBox = new CheckBox();
                            fieldInputBox.IsChecked = bool.Parse(fieldInfos[i].PropValues[k]);
                            fieldInputBox.Checked += (object sender, RoutedEventArgs e) =>
                            {
                                SetValueBool(false, fieldInputBox, fieldName, fieldInputBox.IsChecked.ToString());
                            };
                            fieldInputBox.Unchecked += (object sender, RoutedEventArgs e) =>
                            {
                                SetValueBool(false, fieldInputBox, fieldName, fieldInputBox.IsChecked.ToString());
                            };
                            fieldInputBox.VerticalAlignment = VerticalAlignment.Center;
                            stackPanelOneField.Children.Add(fieldInputBox);
                            stackPanelField.Children.Add(stackPanelOneField);
                            break;

                        case string value when value == typeof(SVector3).AssemblyQualifiedName:
                            StackPanel vectorPanel = new StackPanel { Orientation = Orientation.Horizontal };
                            SVector3 fieldValue = new SVector3(fieldInfos[i].PropValues[k]);

                            vectorPanel.Children.Add(new Label { Content = "X" });
                            TextBox xInputField = new TextBox { Width = 30 };
                            xInputField.Text = fieldValue.X.ToString();
                            xInputField.Tag = false;
                            vectorPanel.Children.Add(xInputField);

                            vectorPanel.Children.Add(new Label { Content = "Y" });
                            TextBox yInputField = new TextBox { Width = 30 };
                            yInputField.Text = fieldValue.Y.ToString();
                            yInputField.Tag = false;
                            vectorPanel.Children.Add(yInputField);

                            vectorPanel.Children.Add(new Label { Content = "Z" });
                            TextBox zInputField = new TextBox { Width = 30 };
                            zInputField.Text = fieldValue.Z.ToString();
                            zInputField.Tag = false;
                            vectorPanel.Children.Add(zInputField);

                            //ENTERを押したらSetValueを呼ぶ
                            xInputField.KeyDown += (object sender, KeyEventArgs e) =>
                            {
                                if (e.Key != Key.Return)
                                    return;

                                SetSVector3Value(false, xInputField, yInputField, zInputField, fieldName);
                                Keyboard.ClearFocus();
                            };
                            yInputField.KeyDown += (object sender, KeyEventArgs e) =>
                            {
                                if (e.Key != Key.Return)
                                    return;

                                SetSVector3Value(false, xInputField, yInputField, zInputField, fieldName);
                                Keyboard.ClearFocus();
                            };
                            zInputField.KeyDown += (object sender, KeyEventArgs e) =>
                            {
                                if (e.Key != Key.Return)
                                    return;

                                SetSVector3Value(false, xInputField, yInputField, zInputField, fieldName);
                                Keyboard.ClearFocus();
                            };

                            //テキストボックスのフォーカスが失ってもSetValueを呼ぶ
                            xInputField.LostFocus += (object sender, RoutedEventArgs e) =>
                            {
                                if (Convert.ToBoolean(xInputField.Tag) == false) { return; }
                                SetSVector3Value(false, xInputField, yInputField, zInputField, fieldName);
                                xInputField.Tag = false;
                            };
                            yInputField.LostFocus += (object sender, RoutedEventArgs e) =>
                            {
                                if (Convert.ToBoolean(yInputField.Tag) == false) { return; }
                                SetSVector3Value(false, xInputField, yInputField, zInputField, fieldName);
                                yInputField.Tag = false;
                            };
                            zInputField.LostFocus += (object sender, RoutedEventArgs e) =>
                            {
                                if (Convert.ToBoolean(zInputField.Tag) == false) { return; }
                                SetSVector3Value(false, xInputField, yInputField, zInputField, fieldName);
                                zInputField.Tag = false;
                            };

                            //テキストが変わらなくてもフォーカス変更だけで勝手に更新しないように
                            xInputField.TextChanged += (object sender, TextChangedEventArgs e) =>
                            {
                                xInputField.Tag = true;
                            };
                            yInputField.TextChanged += (object sender, TextChangedEventArgs e) =>
                            {
                                yInputField.Tag = true;
                            };
                            zInputField.TextChanged += (object sender, TextChangedEventArgs e) =>
                            {
                                zInputField.Tag = true;
                            };

                            stackPanelOneField.Children.Add(vectorPanel);
                            stackPanelField.Children.Add(stackPanelOneField);

                            break;

                        case string value when value == typeof(GameObject).AssemblyQualifiedName:
                            ComboBox fieldInputComboBox = new ComboBox() { MinWidth = 30 };
                            foreach (object o in HierarchyListBox.Items)
                            {
                                fieldInputComboBox.Items.Add(o);
                            }
                            fieldInputComboBox.SelectedIndex = fieldInputComboBox.Items.Add("null");
                            foreach (GameObject obj in HierarchyListBox.Items)
                            {
                                if (obj.Name == fieldInfos[i].PropValues[k])
                                {
                                    fieldInputComboBox.SelectedItem = obj;
                                    break;
                                }
                            }
                            fieldInputComboBox.SelectionChanged += (object sender, SelectionChangedEventArgs e) =>
                            {
                                SetGameObjectValue(false, fieldInputComboBox, fieldName);
                            };
                            stackPanelOneField.Children.Add(fieldInputComboBox);
                            stackPanelField.Children.Add(stackPanelOneField);
                            break;

                        //bool以外の型はテキストで表示
                        default:
                            TextBox fieldInputField = new TextBox() { MinWidth = 30 };
                            fieldInputField.Text = fieldInfos[i].PropValues[k];
                            fieldInputField.VerticalAlignment = VerticalAlignment.Center;

                            //ENTERを押したらSetValueを呼ぶ
                            fieldInputField.KeyDown += (object sender, KeyEventArgs e) =>
                            {
                                if (e.Key != Key.Return)
                                    return;

                                SetValue(false, fieldInputField, fieldName, fieldInputField.Text);
                                Keyboard.ClearFocus();
                            };

                            //テキストボックスのフォーカスが失ってもSetValueを呼ぶ
                            fieldInputField.LostFocus += (object sender, RoutedEventArgs e) =>
                            {
                                SetValue(false, fieldInputField, fieldName, fieldInputField.Text);
                            };

                            stackPanelOneField.Children.Add(fieldInputField);
                            stackPanelField.Children.Add(stackPanelOneField);
                            break;
                    }

                }
                stackPanelTemp.Children.Add(stackPanelField);

                bool predefined = false;
                foreach(string s in Define.preDefinedComponents)
                {
                    if(scriptName == s)
                    {
                        predefined = true;
                        break;
                    }
                }
                if (!predefined)
                {
                    Button ComponentButton = new Button();
                    ComponentButton.Content = "Open Script";
                    ComponentButton.Width = 180;
                    string path = scriptPaths[i];
                    string slnPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/ScriptsLibrary/ScriptsLibrary.sln";

                    ComponentButton.Click += (object ss, RoutedEventArgs ee) => {
                        //slnが開いていない場合は新規slnを開く
                        if (!m_slnOpening)
                        {
                            Process p = new Process();
                            p.StartInfo.FileName = slnPath;
                            p.StartInfo.UseShellExecute = true;
                            p.EnableRaisingEvents = true;
                            //slnが閉じるとフラグが元に戻す
                            p.Exited += (object s, EventArgs e) => {
                                m_slnOpening = false;
                            };
                            p.Disposed += (object s, EventArgs e) => {
                                m_slnOpening = false;
                            };
                            m_slnOpening = true;
                            p.Start();
                            //VSが完全に開いたまで待たないと（約10秒、それでもミスする可能性がある）
                            //下のdevenv.exeが新規VSを開いてしまうので10秒間強制中断
                            Thread.Sleep(10000);
                        }

                        //devenv.exeを使って対象の.csファイルをsln内に開く
                        string devEnvPath = m_devenvPath + @"\devenv.exe";
                        string projPath = m_scriptLibrary.FullPath;
                        var command = $"/edit \"{path}\"";
                        var cmdsi = new ProcessStartInfo
                        {
                            WindowStyle = ProcessWindowStyle.Normal,
                            FileName = devEnvPath,
                            RedirectStandardInput = true,
                            UseShellExecute = false,
                            Arguments = command
                        };
                        Process cmd = Process.Start(cmdsi);
                    };
                    stackPanelTemp.Children.Add(ComponentButton);
                }

                Button RemoveComponentButton = new Button();
                RemoveComponentButton.Content = "Remove Script";
                RemoveComponentButton.Width = 180;
                string name = scriptNames[i];
                int index = i;
                RemoveComponentButton.Click += (object ss, RoutedEventArgs ee) =>
                {
                    var confirmDialog = new removeComponentConfirmDialog(name, gameObjectName);
                    confirmDialog.Owner = GetWindow(this);
                    if (confirmDialog.ShowDialog() == true)
                    {
                        if (confirmDialog.m_isConfirm)
                        {
                            m_loader.RemoveScriptFromGameObjectByIndex(gameObjectName, index);
                            LoadComponents(gameObjectName);
                        }
                    }
                };
                stackPanelTemp.Children.Add(RemoveComponentButton);

                Component_Panel.Children.Add(stackPanelTemp);
                Component_Panel.Children.Add(new Separator());
            }

        }


        //private void Add_BoxCollider(object sender, RoutedEventArgs e)
        //{
        //    GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;

        //    //BoxCollider boxCollider = new BoxCollider();

        //    string path = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\asset\\model\\cube.obj";
        //    NativeMethods.InvokeWithDllProtection(() => NativeMethods.AddBoxCollider(inspectorObject.Name, path));

        //}


        private void Model_Lighting_Checked(object sender, RoutedEventArgs e)
        {
            GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;
            if(inspectorObject == null) { return; }

            NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetModelVS(inspectorObject.Name, "asset/shader/vertexLightingVS.cso"));
            inspectorObject.HasLighting = true;
        }


        private void Model_Lighting_Unchecked(object sender, RoutedEventArgs e)
        {
            GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;
            if (inspectorObject == null) { return; }

            NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetModelVS(inspectorObject.Name, "asset/shader/unlitTextureVS.cso"));
            inspectorObject.HasLighting = false;
        }


        private void Model_Ray_Checked(object sender, RoutedEventArgs e)
        {
            GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;
            if (inspectorObject == null) { return; }

            NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectCanRayHit(inspectorObject.Name, true));
            inspectorObject.CanRayHit = true;
        }


        private void Model_Ray_Unchecked(object sender, RoutedEventArgs e)
        {
            GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;
            if (inspectorObject == null) { return; }

            NativeMethods.InvokeWithDllProtection(() => NativeMethods.SetObjectCanRayHit(inspectorObject.Name, false));
            inspectorObject.CanRayHit = false;
        }


        private void Hierarchy_RemoveObject(object sender, RoutedEventArgs e)
        {
            GameObject inspectorObject = HierarchyListBox.SelectedItem as GameObject;
            if (inspectorObject == null) { return; }

            if(inspectorObject.Name == "MainCamera")
            {
                CenterMessageBox.Show(new WindowWrapper(this), "Main Cameraは削除禁止です！", "Alert", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }

            //ゲーム側、描画側のオブジェクトを削除
            m_loader.RemoveGameObject(inspectorObject.Name);

            //エンジン側のオブジェクトを削除
            HierarchyListBox.Items.Remove(inspectorObject);

            //インスペクターを非表示にする
            HideInspector();
        }

        //========================
        //    PROJECT BROWSER
        //========================

        /// <summary>
        /// ブラウザをプロジェクトのディレクトリに移動
        /// </summary>
        private void ExplorerBrowser_ToDefaultPath(object sender, RoutedEventArgs e)
        {
            //ProjectBrowser.Navigate(ShellFileSystemFolder.FromFolderPath(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)));
            ProjectBrowser_Goto(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location));
        }

        /// <summary>
        /// 前のページに移動
        /// </summary>
        private void ExplorerBrowser_ToPreviousPage(object sender, RoutedEventArgs e)
        {
            m_pressedPageButton = true;

            //前のパスを取得
            string prevPath = m_traversedPath.Pop();
            //ProjectBrowser.Navigate(ShellFileSystemFolder.FromFolderPath(prevPath));
            ProjectBrowser_Goto(prevPath);

            //次のパスを追加
            m_nextPath.Push(m_currentPath);

            //現在のパスを更新
            m_currentPath = prevPath;

            //次のページが押せるように
            Button_NextPage.IsEnabled = true;

            //一番古いパスにたどった場合前のページを押せないように
            if(m_traversedPath.Count <= 0)
            {
                Button_PreviousPage.IsEnabled = false;
            }
        }

        /// <summary>
        /// 次のページに移動
        /// </summary>
        private void ExplorerBrowser_ToNextPage(object sender, RoutedEventArgs e)
        {
            m_pressedPageButton = true;

            //次のパスを取得
            string nextPath = m_nextPath.Pop();
            //ProjectBrowser.Navigate(ShellFileSystemFolder.FromFolderPath(nextPath));
            ProjectBrowser_Goto(nextPath);

            //前のパスを記録
            m_traversedPath.Push(m_currentPath);

            //現在のパスを更新
            m_currentPath = nextPath;

            //前のページが押せるように
            Button_PreviousPage.IsEnabled = true;

            //最新のパスにたどった場合次のページを押せないように
            if(m_nextPath.Count <= 0)
            {
                Button_NextPage.IsEnabled = false;
            }
        }

        /// <summary>
        /// 現在のパスの親ディレクトリに移動
        /// </summary>
        private void ExplorerBrowser_ToOuterDir(object sender, RoutedEventArgs e)
        {
            //親パスを取得し、移動させる
            DirectoryInfo parentInfo = new System.IO.DirectoryInfo(m_currentPath).Parent;
            if (parentInfo != null)
            {
                //ProjectBrowser.Navigate(ShellFileSystemFolder.FromFolderPath(parentInfo.FullName));
                ProjectBrowser_Goto(parentInfo.FullName);
            }
            //既に一番上のパスにいる場合は元のパスに戻す
            else
            {
                ProjectBrowser_Path.Text = m_currentPath;
            }
        }

        /// <summary>
        /// ENTERを押した時、TextBoxのパスに移動
        /// </summary>
        private void ProjectBrowser_Path_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.Key != Key.Enter)
            {
                return;
            }
            //有効なパスの場合
            if (System.IO.Directory.Exists(ProjectBrowser_Path.Text))
            {
                //ProjectBrowser.Navigate(ShellFileSystemFolder.FromFolderPath(ProjectBrowser_Path.Text));
                ProjectBrowser_Goto(ProjectBrowser_Path.Text);
            }
            //無効パスの場合は元のパスに戻す
            else
            {
                ProjectBrowser_Path.Text = m_currentPath;
            }
        }

        /// <summary>
        /// ブラウザ最初ロードする時
        /// </summary>
        private void ProjectBrowser_Load(object sender, EventArgs e)
        {
            m_projectBrowserLoading = true;
            //各種初期化
            string currentPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Button_PreviousPage.IsEnabled = false;
            Button_NextPage.IsEnabled = false;
            ProjectBrowser_Path.Text = currentPath;
            m_currentPath = currentPath;
        }

        /// <summary>
        /// 次のパスに移動した時
        /// </summary>
        private void ProjectBrowser_NavigationComplete(object sender, Microsoft.WindowsAPICodePack.Controls.NavigationCompleteEventArgs e)
        {
            //前のページ・次のページを押した場合
            if (m_pressedPageButton)
            {
                m_pressedPageButton = false;
                ProjectBrowser_Path.Text = e.NewLocation.ParsingName;
                return;
            }
            //最初ロードした場合
            if (m_projectBrowserLoading)
            {
                m_projectBrowserLoading = false;
                ProjectBrowser_Path.Text = e.NewLocation.ParsingName;
                return;
            }

            //前のパスを記録
            m_traversedPath.Push(m_currentPath);
            Button_PreviousPage.IsEnabled = true;

            //テキストを表示
            ProjectBrowser_Path.Text = e.NewLocation.ParsingName;

            //次のパスをクリア
            m_nextPath.Clear();
            Button_NextPage.IsEnabled = false;

            //現在のパスを更新
            m_currentPath = e.NewLocation.ParsingName;
        }

        public static void UpdateKeyboardState()
        {

        }

        private void ProjectBrowser_Loaded(object sender, RoutedEventArgs e)
        {
            m_projectBrowserLoading = false;
            //各種初期化
            string currentPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            Button_PreviousPage.IsEnabled = false;
            Button_NextPage.IsEnabled = false;
            ProjectBrowser_Path.Text = currentPath;
            m_currentPath = currentPath;

            //リストビューのアイテムを更新
            m_projectBrowserItems.Clear();
            foreach(string s in Directory.GetDirectories(currentPath))
            {
                //s.Replace(currentPath, "");
                m_projectBrowserItems.Add(s.Replace(currentPath + @"\", ""));
            }
            foreach(string obj in Directory.GetFiles(currentPath, "*.obj"))
            {
                //obj.Replace(currentPath, "");
                m_projectBrowserItems.Add(obj.Replace(currentPath + @"\", ""));
            }
            ProjectBrowser.ItemsSource = m_projectBrowserItems;
        }

        private void ProjectBrowser_Goto(string directory)
        {
            //前のパスを記録
            m_traversedPath.Push(m_currentPath);
            Button_PreviousPage.IsEnabled = true;

            //テキストを表示
            ProjectBrowser_Path.Text = directory;

            //次のパスをクリア
            m_nextPath.Clear();
            Button_NextPage.IsEnabled = false;

            //現在のパスを更新
            m_currentPath = directory;

            //リストビューのアイテムを更新
            m_projectBrowserItems.Clear();
            foreach (string s in Directory.GetDirectories(directory))
            {
                m_projectBrowserItems.Add(s.Replace(directory + @"\", ""));
            }
            foreach (string obj in Directory.GetFiles(directory, "*.obj"))
            {
                m_projectBrowserItems.Add(obj.Replace(directory + @"\", ""));
            }
        }

        private void ProjectBrowser_Item_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            //選んでいない場合は処理しない
            if (ProjectBrowser.SelectedItem == null) return;

            //.objを選んだ場合は直接ホストに入れる
            if (Path.GetExtension(m_currentPath + @"\" + ProjectBrowser.SelectedItem.ToString()) == ".obj")
            {
                Host_AddModel(m_currentPath + @"\" + ProjectBrowser.SelectedItem.ToString());
                return;
            }

            //フォルダの場合は入る
            ProjectBrowser_Goto(m_currentPath + @"\" + ProjectBrowser.SelectedItem.ToString());
        }

        private void ProjectBrowser_LostFocus(object sender, RoutedEventArgs e)
        {
            ProjectBrowser.SelectedItem = null;
        }

        private void ProjectBrowser_Item_Add(object sender, RoutedEventArgs e)
        {
            //.objを選んだ場合は直接ホストに入れる
            if (Path.GetExtension(m_currentPath + @"\" + ProjectBrowser.SelectedItem.ToString()) == ".obj")
            {
                Host_AddModel(m_currentPath + @"\" + ProjectBrowser.SelectedItem.ToString());
                return;
            }
        }


        private void ProjectBrowser_Item_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            FrameworkElement b = e.Source as FrameworkElement;
            ContextMenu contextMenu = new ContextMenu();
            MenuItem item = new MenuItem();
            if (Path.GetExtension(m_currentPath + @"\" + ProjectBrowser.SelectedItem.ToString()) != ".obj")
            {
                item.Header = "Enter Folder";
                item.Click += ProjectBrowser_Enter_Folder;
            }
            else
            {
                item.Header = "Add to game";
                item.Click += ProjectBrowser_Item_Add;
            }
            contextMenu.Items.Add(item);
            b.ContextMenu = contextMenu;
        }

        private void ProjectBrowser_Enter_Folder(object sender, RoutedEventArgs e)
        {
            //選んでいない場合は処理しない
            if (ProjectBrowser.SelectedItem == null) return;

            //フォルダの場合は入る
            ProjectBrowser_Goto(m_currentPath + @"\" + ProjectBrowser.SelectedItem.ToString());
        }
    }



}
