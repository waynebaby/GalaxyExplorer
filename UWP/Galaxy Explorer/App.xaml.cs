﻿// Copyright Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
using Windows.ApplicationModel.Activation;
using Windows.System.Profile;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using UnityPlayer;

namespace GalaxyExplorer
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private AppCallbacks appCallbacks;
        public SplashScreen splashScreen;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            //SetupOrientation();
            appCallbacks = new AppCallbacks();
        }

        /// <summary>
        /// Invoked when application is launched through protocol.
        /// Read more - http://msdn.microsoft.com/library/windows/apps/br224742
        /// </summary>
        /// <param name="args"></param>
        protected override void OnActivated(IActivatedEventArgs args)
        {
            string appArgs = "";

            switch (args.Kind)
            {
                case ActivationKind.Protocol:
                    ProtocolActivatedEventArgs eventArgs = args as ProtocolActivatedEventArgs;
                    splashScreen = eventArgs.SplashScreen;
                    appArgs += string.Format("Uri={0}", eventArgs.Uri.AbsoluteUri);
                    break;
            }
            InitializeUnity(appArgs);
        }

        /// <summary>
        /// Invoked when application is launched via file
        /// Read more - http://msdn.microsoft.com/library/windows/apps/br224742
        /// </summary>
        /// <param name="args"></param>
        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            string appArgs = "";

            splashScreen = args.SplashScreen;
            appArgs += "File=";
            bool firstFileAdded = false;
            foreach (var file in args.Files)
            {
                if (firstFileAdded) appArgs += ";";
                appArgs += file.Path;
                firstFileAdded = true;
            }

            InitializeUnity(appArgs);
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used when the application is launched to open a specific file, to display
        /// search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            splashScreen = args.SplashScreen;
            InitializeUnity(args.Arguments);
        }

        private void InitializeUnity(string args)
        {
#if UNITY_WP_8_1 || UNITY_UWP
            ApplicationView.GetForCurrentView().SuppressSystemOverlays = true;
#endif

#if UNITY_WP_8_1
            StatusBar.GetForCurrentView().HideAsync();
#endif

            appCallbacks.SetAppArguments(args);
            Frame rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null && !appCallbacks.IsInitialized())
            {
                rootFrame = new Frame();
                Window.Current.Content = rootFrame;
#if !UNITY_HOLOGRAPHIC
                Window.Current.Activate();
#endif
                rootFrame.Navigate(typeof(MainPage));
            }

            Window.Current.Activate();

            // Integrate with Shell back button behavior on non-Holographic UWP
            // platforms such as Desktop and Mobile. Using DeviceFamily name
            // here instead of an API Contract check since the UWP API we will
            // subsequently call when the back button should be visible
            // (AppViewBackButtonVisibility) is present on all platforms and
            // simply no-ops on some.
            // Unity has the UnityEngine.VR.VRDevice.isPresent which would work
            // here but since this will wind up including a check for
            // SurfaceHub as well in the future which Unity does not know about,
            // this will consistently use DeviceFamily for these platform checks
            AppCallbacks.Instance.InvokeOnAppThread(() =>
                {
                    MyAppPlatformManager.MyAppPlatformManagerInitialized += RegisterForBackButtonChangeRequests;
                },
                waitUntilDone: false);
        }

        void SetupOrientation()
        {
#if UNITY_UWP
            Unity.UnityGenerated.SetupDisplay();
#endif					
        }
		
        private void RegisterForBackButtonChangeRequests()
        {
            if ((MyAppPlatformManager.Platform != MyAppPlatformManager.PlatformId.HoloLens) &&
                (MyAppPlatformManager.Platform != MyAppPlatformManager.PlatformId.ImmersiveHMD))
            {
                UnityEngine.Debug.LogFormat("RegisterForBackButtonChangeRequests on platform {0}", MyAppPlatformManager.Platform.ToString());
                ToolManager.BackButtonVisibilityChangeRequested += ToolManager_BackButtonVisibilityChangeRequested;
            }
        }

        /// <summary>
        /// Back navigation requested by the user via Shell affordance
        /// </summary>
        private void App_BackRequested(object sender, BackRequestedEventArgs e)
        {
            AppCallbacks.Instance.InvokeOnAppThread(() =>
                {
                    if (ToolManager.Instance)
                    {
                        var backButton = ToolManager.Instance.FindButtonByType(ButtonType.Back);
                        if (backButton)
                        {
                            backButton.ButtonAction();
                        }
                    }
                },
                waitUntilDone: false);

            // We will always handle back in the BackRequested callback.  In
            // the case of an empty backstack, the callback itself should be
            // removed from the BackRequested event's delegate list so this is
            // not called
            e.Handled = true;
        }

        /// <summary>
        /// Callback from GalaxyExplorer's ToolManager indicating whether or
        /// not the BackButton should be visible.  Tell the Shell the requested
        /// visual state for the BackButton and register for BackRequested
        /// callbacks if visible
        /// </summary>
        /// <param name="visible"></param>
        private void ToolManager_BackButtonVisibilityChangeRequested(bool visible)
        {
            AppCallbacks.Instance.InvokeOnUIThread(() =>
                {
                    if (visible)
                    {
                        SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                        SystemNavigationManager.GetForCurrentView().BackRequested += App_BackRequested;
                    }
                    else
                    {
                        SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Collapsed;
                        SystemNavigationManager.GetForCurrentView().BackRequested -= App_BackRequested;
                    }
                },
                waitUntilDone: false);
        }
    }
}
