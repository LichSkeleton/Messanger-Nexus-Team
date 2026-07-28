namespace NexusTeam.Client.Views
{
    using System;
    using System.Windows;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Interaction logic for ImageViewerDialog.xaml.
    /// </summary>
    public partial class ImageViewerDialog : Window
    {
        private const double ZoomStep = 0.1;
        private const double MinZoom = 0.25;
        private const double MaxZoom = 5.0;
        private double currentZoom = 1.0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageViewerDialog"/> class.
        /// </summary>
        /// <param name="viewModel">The image viewer view model.</param>
        public ImageViewerDialog(ImageViewModel viewModel)
        {
            this.InitializeComponent();
            this.DataContext = viewModel;

            // Make window full screen and position it over the owner window
            this.Loaded += this.ImageViewerDialog_Loaded;

            // Reset zoom when image changes
            if (viewModel != null)
            {
                viewModel.PropertyChanged += (s, e) =>
                {
                    if ((e.PropertyName == nameof(ImageViewModel.ImageSource) ||
                         e.PropertyName == nameof(ImageViewModel.ImageName)) &&
                        this.currentZoom != 1.0)
                    {
                        this.ResetZoom();
                    }
                };
            }

            // Focus window to receive keyboard events
            this.Focusable = true;
            this.Loaded += (s, e) => this.Focus();

            // Cleanup event handlers when window closes
            this.Closed += this.ImageViewerDialog_Closed;

            // Subscribe to window size changes to update image dimensions
            this.SizeChanged += (s, e) => this.UpdateContainerMaxDimensions();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageViewerDialog"/> class with a BitmapImage.
        /// </summary>
        /// <param name="bitmapImage">The bitmap image to display.</param>
        public ImageViewerDialog(BitmapImage bitmapImage)
        {
            this.InitializeComponent();

            // Create a simple view model for the bitmap
            var viewModel = new ImageViewModel(bitmapImage, "Avatar");

            this.DataContext = viewModel;

            // Make window full screen and position it over the owner window
            this.Loaded += this.ImageViewerDialog_Loaded;

            // Focus window to receive keyboard events
            this.Focusable = true;
            this.Loaded += (s, e) => this.Focus();

            // Cleanup event handlers when window closes
            this.Closed += this.ImageViewerDialog_Closed;

            // Subscribe to window size changes to update image dimensions
            this.SizeChanged += (s, e) => this.UpdateContainerMaxDimensions();
        }

        private void ImageViewerDialog_Closed(object? sender, EventArgs e)
        {
            if (this.Owner != null)
            {
                this.Owner.LocationChanged -= this.Owner_LocationChanged;
                this.Owner.SizeChanged -= this.Owner_SizeChanged;
                this.Owner.StateChanged -= this.Owner_StateChanged;
            }
        }

        private void ImageViewerDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (this.Owner != null)
            {
                // Sync size and position with owner
                this.UpdatePositionAndSize();

                // Subscribe to owner events
                this.Owner.LocationChanged += this.Owner_LocationChanged;
                this.Owner.SizeChanged += this.Owner_SizeChanged;
                this.Owner.StateChanged += this.Owner_StateChanged;

                // Sync state
                if (this.Owner.WindowState == WindowState.Maximized)
                {
                    this.WindowState = WindowState.Maximized;
                }
            }
            else
            {
                // Fallback to screen size
                this.Width = SystemParameters.PrimaryScreenWidth;
                this.Height = SystemParameters.PrimaryScreenHeight;
                this.Left = 0;
                this.Top = 0;
            }

            // Set initial container max dimensions
            this.UpdateContainerMaxDimensions();
        }

        private void Owner_StateChanged(object? sender, EventArgs e)
        {
            if (this.Owner != null)
            {
                this.WindowState = this.Owner.WindowState;
            }
        }

        private void Owner_LocationChanged(object? sender, EventArgs e)
        {
            this.UpdatePositionAndSize();
        }

        private void Owner_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.UpdatePositionAndSize();
            this.UpdateContainerMaxDimensions();
        }

        private void UpdatePositionAndSize()
        {
            if (this.Owner != null && this.Owner.WindowState == WindowState.Normal)
            {
                this.Left = this.Owner.Left;
                this.Top = this.Owner.Top;
                this.Width = this.Owner.ActualWidth;
                this.Height = this.Owner.ActualHeight;
            }
        }

        private void UpdateContainerMaxDimensions()
        {
            // Set image max dimensions based on window size
            if (this.ImageView != null && this.ActualWidth > 0 && this.ActualHeight > 0)
            {
                // Calculate available space (window size minus margins and UI elements)
                // Margins: 20px on each side (40px total)
                // Title bar: ~60px
                // Footer: ~60px
                var horizontalMargin = 80.0; // 40px margins + 40px for navigation buttons
                var verticalMargin = 180.0; // 40px margins + 60px title + 60px footer + 20px padding

                var availableWidth = this.ActualWidth - horizontalMargin;
                var availableHeight = this.ActualHeight - verticalMargin;

                this.ImageView.MaxWidth = Math.Max(100, availableWidth);
                this.ImageView.MaxHeight = Math.Max(100, availableHeight);
            }
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Close when clicking anywhere (child elements will prevent with e.Handled = true)
            if (e.ChangedButton == MouseButton.Left)
            {
                this.Close();
            }
        }

        private void Image_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Prevent closing when clicking on the image
            e.Handled = true;
        }

        private void UI_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Prevent closing when clicking on UI elements (title bar, footer, etc)
            e.Handled = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            // Handle keyboard shortcuts
            if (e.Key == Key.Escape)
            {
                // Close on Escape key
                this.Close();
            }
            else if (e.Key == Key.Left)
            {
                // Navigate to previous image
                if (this.DataContext is ImageViewModel viewModel)
                {
                    viewModel.NavigatePreviousCommand.Execute(null);
                }
            }
            else if (e.Key == Key.Right)
            {
                // Navigate to next image
                if (this.DataContext is ImageViewModel viewModel)
                {
                    viewModel.NavigateNextCommand.Execute(null);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Image_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Zoom in/out with Ctrl + Mouse Wheel
            if (Keyboard.Modifiers == ModifierKeys.Control)
            {
                e.Handled = true;

                if (this.ImageView != null && this.ImageScaleTransform != null)
                {
                    var mousePosition = e.GetPosition(this.ImageView);
                    var zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
                    this.ZoomAtPoint(mousePosition, zoomFactor);
                }
            }
        }

        private void ZoomAtPoint(Point point, double zoomFactor)
        {
            if (this.ImageView == null || this.ImageScaleTransform == null)
            {
                return;
            }

            var newZoom = this.currentZoom * zoomFactor;
            newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, newZoom));

            // Calculate the zoom delta
            var deltaZoom = newZoom / this.currentZoom;

            // Get the current scroll position
            var scrollViewer = this.ImageScrollViewer;
            var scrollX = scrollViewer.HorizontalOffset;
            var scrollY = scrollViewer.VerticalOffset;

            // Calculate the position relative to the image
            var imageWidth = this.ImageView.ActualWidth * this.currentZoom;
            var imageHeight = this.ImageView.ActualHeight * this.currentZoom;

            // Calculate the new scroll position to keep the point under the cursor
            var newScrollX = ((scrollX + point.X) * deltaZoom) - point.X;
            var newScrollY = ((scrollY + point.Y) * deltaZoom) - point.Y;

            // Apply the zoom
            this.currentZoom = newZoom;
            this.ImageScaleTransform.ScaleX = this.currentZoom;
            this.ImageScaleTransform.ScaleY = this.currentZoom;

            // Update scroll position after a brief delay to allow the image to resize
            this.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Loaded, new Action(() =>
            {
                scrollViewer.ScrollToHorizontalOffset(Math.Max(0, newScrollX));
                scrollViewer.ScrollToVerticalOffset(Math.Max(0, newScrollY));
            }));
        }

        private void ZoomIn_Click(object sender, RoutedEventArgs e)
        {
            this.ZoomIn();
        }

        private void ZoomOut_Click(object sender, RoutedEventArgs e)
        {
            this.ZoomOut();
        }

        private void ResetZoom_Click(object sender, RoutedEventArgs e)
        {
            this.ResetZoom();
        }

        private void ZoomIn()
        {
            this.currentZoom = Math.Min(this.currentZoom + ZoomStep, MaxZoom);
            this.ApplyZoom();
        }

        private void ZoomOut()
        {
            this.currentZoom = Math.Max(this.currentZoom - ZoomStep, MinZoom);
            this.ApplyZoom();
        }

        private void ResetZoom()
        {
            this.currentZoom = 1.0;
            this.ApplyZoom();
        }

        private void ApplyZoom()
        {
            if (this.ImageScaleTransform != null)
            {
                this.ImageScaleTransform.ScaleX = this.currentZoom;
                this.ImageScaleTransform.ScaleY = this.currentZoom;
            }
        }
    }
}
