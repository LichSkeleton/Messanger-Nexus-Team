namespace NexusTeam.Client.Services
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Extensions.DependencyInjection;
    using NexusTeam.Client.ViewModels;

    /// <summary>
    /// Service for navigation between views.
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly MainWindowViewModel mainWindowViewModel;
        private readonly Stack<ViewModelBase> navigationHistory = new Stack<ViewModelBase>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationService"/> class.
        /// </summary>
        /// <param name="serviceProvider">Service provider.</param>
        /// <param name="mainWindowViewModel">Main window view model.</param>
        public NavigationService(IServiceProvider serviceProvider, MainWindowViewModel mainWindowViewModel)
        {
            this.serviceProvider = serviceProvider;
            this.mainWindowViewModel = mainWindowViewModel;
        }

        /// <inheritdoc/>
        public event EventHandler<ViewModelBase>? CurrentViewModelChanged;

        /// <inheritdoc/>
        public ViewModelBase? CurrentViewModel => this.mainWindowViewModel.CurrentViewModel;

        /// <inheritdoc/>
        public void NavigateTo<TViewModel>()
            where TViewModel : ViewModelBase
        {
            // Call OnNavigatedFrom on the current view model
            if (this.mainWindowViewModel.CurrentViewModel != null)
            {
                this.mainWindowViewModel.CurrentViewModel.OnNavigatedFrom();
                this.navigationHistory.Push(this.mainWindowViewModel.CurrentViewModel);
            }

            // Create new view model
            var viewModel = this.serviceProvider.GetRequiredService<TViewModel>();
            this.mainWindowViewModel.SetCurrentViewModel(viewModel);

            // Call OnNavigatedTo on the new view model
            viewModel.OnNavigatedTo();

            // Notify subscribers
            this.CurrentViewModelChanged?.Invoke(this, viewModel);
        }

        /// <inheritdoc/>
        public void NavigateBack()
        {
            if (this.navigationHistory.Count > 0)
            {
                // Call OnNavigatedFrom on the current view model
                if (this.mainWindowViewModel.CurrentViewModel != null)
                {
                    this.mainWindowViewModel.CurrentViewModel.OnNavigatedFrom();
                }

                var previousViewModel = this.navigationHistory.Pop();
                this.mainWindowViewModel.SetCurrentViewModel(previousViewModel);

                // Call OnNavigatedTo on the restored view model
                previousViewModel.OnNavigatedTo();

                // Notify subscribers
                this.CurrentViewModelChanged?.Invoke(this, previousViewModel);
            }
        }

        /// <inheritdoc/>
        public void GoBack()
        {
            this.NavigateBack();
        }

        /// <inheritdoc/>
        public void ClearHistory()
        {
            this.navigationHistory.Clear();
        }
    }
}
