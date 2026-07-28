namespace NexusTeam.Client.ViewModels
{
    using CommunityToolkit.Mvvm.ComponentModel;

    /// <summary>
    /// Base class for all view models in the application.
    /// </summary>
    public abstract class ViewModelBase : ObservableObject
    {
        private bool isBusy;
        private string title = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the view model is busy.
        /// </summary>
        public bool IsBusy
        {
            get => this.isBusy;
            set => this.SetProperty(ref this.isBusy, value);
        }

        /// <summary>
        /// Gets or sets the title of the view.
        /// </summary>
        public string Title
        {
            get => this.title;
            set => this.SetProperty(ref this.title, value);
        }

        /// <summary>
        /// Called when the view model is navigated to.
        /// </summary>
        public virtual void OnNavigatedTo()
        {
        }

        /// <summary>
        /// Called when the view model is navigated away from.
        /// </summary>
        public virtual void OnNavigatedFrom()
        {
        }
    }
}
