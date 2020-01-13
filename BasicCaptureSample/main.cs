﻿using Microsoft.Graphics.Canvas;
using System;
using System.Numerics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Graphics.Capture;
using Windows.UI;
using Windows.UI.Composition;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace BasicCaptureSample
{
    public class MainView : IFrameworkView
    {
        public void Initialize(CoreApplicationView applicationView)
        {
            _view = applicationView;
        }


        public void SetWindow(CoreWindow window)
        {
            _window = window;
            
        }

        public void Load(string entryPoint) { }

        public void Run()
        {
            ApplicationView.PreferredLaunchWindowingMode = ApplicationViewWindowingMode.PreferredLaunchViewSize;
            ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(1280, 1024));

            ApplicationViewTitleBar formattableTitleBar = ApplicationView.GetForCurrentView().TitleBar;
            formattableTitleBar.ButtonForegroundColor = Windows.UI.Colors.Transparent;
            formattableTitleBar.ButtonBackgroundColor = Windows.UI.Colors.Transparent;
            CoreApplicationViewTitleBar coreTitleBar = CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;

            _compositor = new Compositor();
            _target = _compositor.CreateTargetForCurrentView();
            _root = _compositor.CreateSpriteVisual();
            _content = _compositor.CreateSpriteVisual();
            _brush = _compositor.CreateSurfaceBrush();

            _root.Brush = _compositor.CreateColorBrush(Colors.White);
            _root.RelativeSizeAdjustment = Vector2.One;
            _target.Root = _root;

            if (GraphicsCaptureSession.IsSupported())
            {
                //_content.AnchorPoint = new Vector2(0.5f, 0.5f);
                //_content.RelativeOffsetAdjustment = new Vector3(0.5f, 0.5f, 0);
                _content.AnchorPoint = new Vector2(0, 0);
                _content.RelativeOffsetAdjustment = new Vector3(0, 0, 0);
                _content.RelativeSizeAdjustment = Vector2.One;
                _content.Size = new Vector2(0, 0);
                _content.Brush = _brush;
                //_brush.HorizontalAlignmentRatio = 0.5f;
                //_brush.VerticalAlignmentRatio = 0.5f; 
                _brush.HorizontalAlignmentRatio = 0;
                _brush.VerticalAlignmentRatio = 0;
                _brush.Stretch = CompositionStretch.Uniform;
                //var shadow = _compositor.CreateDropShadow();
                //shadow.Mask = _brush;
                //_content.Shadow = shadow;
                _root.Children.InsertAtTop(_content);

                _device = new CanvasDevice();

                // We can't just call the picker here, because no one is pumping messages yet.
                // By asking the dispatcher for our UI thread to run this, we ensure that the
                // message pump is pumping messages by the time this runs.
                var ignored = _window.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                {
                    var ignoredTask = StartCaptureAsync();
                });

                _doubleTapHelper = new DoubleTapHelper(_window);
                _doubleTapHelper.DoubleTapped += OnDoubleTapped;
            }
            else
            {
                var ignored = _window.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () =>
                {
                    var dialog = new MessageDialog("Screen capture is not supported on this device for this release of Windows!");

                    await dialog.ShowAsync();
                });
            }

            _window.Activate();
            _window.Dispatcher.ProcessEvents(CoreProcessEventsOption.ProcessUntilQuit);
        }

        private void OnDoubleTapped(object sender, EventArgs e)
        {
            StopCapture();
            var ignored = StartCaptureAsync();
        }

        public void Uninitialize()
        {
            StopCapture();
            _compositor.Dispose();
        }

        private async Task StartCaptureAsync()
        {
            var picker = new GraphicsCapturePicker();
            var item = await picker.PickSingleItemAsync();

            if (item != null)
            {
                _capture = new Capture(_device, item);

                var surface = _capture.CreateSurface(_compositor);
                _brush.Surface = surface;

                _capture.StartCapture();
            }
        }

        private void StopCapture()
        {
            _capture?.Dispose();
            _brush.Surface = null;
        }

        private CoreWindow _window;
        private CoreApplicationView _view;

        private Compositor _compositor;
        private CompositionTarget _target;
        private SpriteVisual _root;
        private SpriteVisual _content;
        private CompositionSurfaceBrush _brush;

        private CanvasDevice _device;
        private Capture _capture;
        private DoubleTapHelper _doubleTapHelper;
    }

    public class MainViewFactory : IFrameworkViewSource
    {
        public IFrameworkView CreateView()
        {
            return new MainView();
        }

        public static void Main(string[] args)
        {
            CoreApplication.Run(new MainViewFactory());
        }
    }
}
