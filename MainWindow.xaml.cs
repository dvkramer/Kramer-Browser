using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace WebView2Browser
{
    public partial class MainWindow : Window
    {
        private List<BrowserTab> tabs = new List<BrowserTab>();
        private BrowserTab? currentTab;
        private const string HOME_URL = "https://www.google.com";

        public MainWindow()
        {
            InitializeComponent();
            CreateNewTab();
        }

        private async void CreateNewTab()
        {
            var tab = new BrowserTab();
            tabs.Add(tab);

            // Create tab button
            var tabButton = new Button
            {
                Content = "New Tab",
                Style = (Style)FindResource("TabButtonStyle"),
                Tag = tab
            };
            tabButton.Click += TabButton_Click;

            // Handle close button in template
            tabButton.Loaded += (s, e) =>
            {
                var template = tabButton.Template;
                var closeButton = template.FindName("CloseButton", tabButton) as Button;
                if (closeButton != null)
                {
                    closeButton.Click += (sender, args) => CloseTab(tab);
                }
            };

            tab.TabButton = tabButton;
            TabPanel.Children.Add(tabButton);

            // Create WebView2
            var webView = new WebView2();
            tab.WebView = webView;
            WebViewContainer.Children.Add(webView);

            try
            {
                await webView.EnsureCoreWebView2Async();
                
                // Set up event handlers
                webView.NavigationStarting += (s, e) => WebView_NavigationStarting(s, e, tab);
                webView.NavigationCompleted += (s, e) => WebView_NavigationCompleted(s, e, tab);
                webView.CoreWebView2.DocumentTitleChanged += (s, e) => WebView_DocumentTitleChanged(s, e, tab);

                // Navigate to home
                webView.CoreWebView2.Navigate(HOME_URL);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing WebView2: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }

            // Switch to new tab
            SwitchToTab(tab);
        }

        private void WebView_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e, BrowserTab tab)
        {
            if (tab.IsDisposed || tab != currentTab) return;
            
            try
            {
                UrlTextBox.Text = e.Uri;
                UpdateNavigationButtons();
            }
            catch (ObjectDisposedException)
            {
                // WebView was disposed, ignore
            }
        }

        private void WebView_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e, BrowserTab tab)
        {
            if (tab.IsDisposed) return;
            
            try
            {
                UpdateNavigationButtons();
            }
            catch (ObjectDisposedException)
            {
                // WebView was disposed, ignore
            }
        }

        private void WebView_DocumentTitleChanged(object? sender, object e, BrowserTab tab)
        {
            if (tab.IsDisposed) return;
            
            try
            {
                if (tab.WebView?.CoreWebView2 != null)
                {
                    var title = tab.WebView.CoreWebView2.DocumentTitle;
                    var displayTitle = string.IsNullOrEmpty(title) ? "New Tab" : 
                        (title.Length > 20 ? title.Substring(0, 20) + "..." : title);
                    
                    Dispatcher.Invoke(() =>
                    {
                        if (!tab.IsDisposed && tab.TabButton != null)
                        {
                            tab.TabButton.Content = displayTitle;
                        }
                    });
                }
            }
            catch (ObjectDisposedException)
            {
                // WebView was disposed, ignore
            }
        }

        private void SwitchToTab(BrowserTab tab)
        {
            if (tab.IsDisposed) return;
            
            try
            {
                // Hide all webviews
                foreach (var t in tabs.Where(t => !t.IsDisposed))
                {
                    if (t.WebView != null)
                    {
                        try
                        {
                            t.WebView.Visibility = Visibility.Hidden;
                        }
                        catch (ObjectDisposedException) { }
                    }
                    if (t.TabButton != null)
                        t.TabButton.Style = (Style)FindResource("TabButtonStyle");
                }

                // Show current tab
                currentTab = tab;
                if (tab.WebView != null && tab.TabButton != null)
                {
                    try
                    {
                        tab.WebView.Visibility = Visibility.Visible;
                        tab.TabButton.Style = (Style)FindResource("ActiveTabButtonStyle");

                        // Update UI
                        if (tab.WebView.CoreWebView2 != null)
                        {
                            UrlTextBox.Text = tab.WebView.CoreWebView2.Source;
                            UpdateNavigationButtons();
                        }
                    }
                    catch (ObjectDisposedException)
                    {
                        // Tab was disposed during switching, remove it
                        if (tabs.Contains(tab))
                            CloseTab(tab);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error switching tab: {ex.Message}");
            }
        }

        private void CloseTab(BrowserTab tab)
        {
            if (tabs.Count <= 1) return; // Don't close last tab

            try
            {
                // Mark tab as disposed to prevent further event handling
                tab.IsDisposed = true;
                
                // Switch to another tab first if this was current
                if (currentTab == tab)
                {
                    var nextTab = tabs.Where(t => t != tab).FirstOrDefault();
                    if (nextTab != null)
                        SwitchToTab(nextTab);
                }

                // Remove from list first
                tabs.Remove(tab);

                // Remove from UI
                TabPanel.Children.Remove(tab.TabButton);
                WebViewContainer.Children.Remove(tab.WebView);
                
                // Dispose WebView safely
                if (tab.WebView != null)
                {
                    try
                    {
                        tab.WebView.Dispose();
                    }
                    catch (ObjectDisposedException)
                    {
                        // Already disposed, ignore
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                System.Diagnostics.Debug.WriteLine($"Error closing tab: {ex.Message}");
            }
        }

        private void UpdateNavigationButtons()
        {
            try
            {
                if (currentTab?.WebView?.CoreWebView2 != null && !currentTab.IsDisposed)
                {
                    BackButton.IsEnabled = currentTab.WebView.CoreWebView2.CanGoBack;
                    ForwardButton.IsEnabled = currentTab.WebView.CoreWebView2.CanGoForward;
                }
                else
                {
                    BackButton.IsEnabled = false;
                    ForwardButton.IsEnabled = false;
                }
            }
            catch (ObjectDisposedException)
            {
                // WebView was disposed, disable buttons
                BackButton.IsEnabled = false;
                ForwardButton.IsEnabled = false;
            }
        }

        private void NavigateToUrl(string url)
        {
            try
            {
                if (currentTab?.WebView?.CoreWebView2 != null && !currentTab.IsDisposed)
                {
                    // Add protocol if missing
                    if (!url.StartsWith("http://") && !url.StartsWith("https://"))
                    {
                        // Check if it looks like a search query
                        if (!url.Contains(".") || url.Contains(" "))
                            url = $"https://www.google.com/search?q={Uri.EscapeDataString(url)}";
                        else
                            url = "https://" + url;
                    }
                    
                    currentTab.WebView.CoreWebView2.Navigate(url);
                }
            }
            catch (ObjectDisposedException)
            {
                // WebView was disposed, ignore
            }
        }

        // Event Handlers
        private void NewTab_Click(object sender, RoutedEventArgs e)
        {
            CreateNewTab();
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is BrowserTab tab)
            {
                SwitchToTab(tab);
            }
        }

        private void GoBack_Click(object sender, RoutedEventArgs e)
        {
            currentTab?.WebView?.GoBack();
        }

        private void GoForward_Click(object sender, RoutedEventArgs e)
        {
            currentTab?.WebView?.GoForward();
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            currentTab?.WebView?.Reload();
        }

        private void Home_Click(object sender, RoutedEventArgs e)
        {
            NavigateToUrl(HOME_URL);
        }

        private void Menu_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for menu functionality
            MessageBox.Show("Menu functionality coming soon!", "Menu", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                NavigateToUrl(UrlTextBox.Text);
                e.Handled = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            // Dispose all WebViews
            foreach (var tab in tabs)
            {
                tab.WebView?.Dispose();
            }
            base.OnClosed(e);
        }
    }

    // Helper class to manage tab data
    public class BrowserTab
    {
        public WebView2? WebView { get; set; }
        public Button? TabButton { get; set; }
        public bool IsDisposed { get; set; } = false;
    }
}