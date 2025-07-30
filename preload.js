const { contextBridge, ipcRenderer } = require('electron');

// Expose protected methods that allow the renderer process to use
// the ipcRenderer without exposing the entire object
contextBridge.exposeInMainWorld('electronAPI', {
  // Menu actions
  onMenuNewTab: (callback) => ipcRenderer.on('menu-new-tab', callback),
  onMenuCloseTab: (callback) => ipcRenderer.on('menu-close-tab', callback),
  onMenuGoBack: (callback) => ipcRenderer.on('menu-go-back', callback),
  onMenuGoForward: (callback) => ipcRenderer.on('menu-go-forward', callback),
  onMenuRefresh: (callback) => ipcRenderer.on('menu-refresh', callback),
  onMenuGoHome: (callback) => ipcRenderer.on('menu-go-home', callback),
  onMenuAbout: (callback) => ipcRenderer.on('menu-about', callback),

  // App info
  getVersion: () => ipcRenderer.invoke('get-version'),
  showAbout: () => ipcRenderer.invoke('show-about'),

  // Remove listeners
  removeAllListeners: (channel) => ipcRenderer.removeAllListeners(channel)
});