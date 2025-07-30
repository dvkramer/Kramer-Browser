const tabsContainer = document.getElementById('tabs');
const viewsContainer = document.getElementById('views');
const urlInput = document.getElementById('url');
const backButton = document.getElementById('back');
const forwardButton = document.getElementById('forward');
const refreshButton = document.getElementById('refresh');
const homeButton = document.getElementById('home');

let tabs = [];
let activeTab = null;

const HOME_URL = 'https://www.google.com';

function createNewTab(url = HOME_URL) {
    const tabId = `tab-${Date.now()}`;

    // Create tab element
    const tab = document.createElement('div');
    tab.className = 'tab';
    tab.dataset.tabId = tabId;

    const title = document.createElement('span');
    title.className = 'tab-title';
    title.textContent = 'New Tab';
    tab.appendChild(title);

    const closeButton = document.createElement('button');
    closeButton.className = 'close-tab';
    closeButton.textContent = '×';
    tab.appendChild(closeButton);

    tabsContainer.appendChild(tab);

    // Create webview
    const webview = document.createElement('webview');
    webview.className = 'hidden';
    webview.src = url;
    viewsContainer.appendChild(webview);

    const newTab = {
        id: tabId,
        element: tab,
        webview: webview,
        title: title
    };

    tabs.push(newTab);
    setActiveTab(newTab);

    // Event listeners
    tab.addEventListener('click', () => setActiveTab(newTab));
    closeButton.addEventListener('click', (e) => {
        e.stopPropagation();
        closeTab(newTab);
    });

    webview.addEventListener('did-start-loading', () => {
        // show loading indicator
    });

    webview.addEventListener('did-stop-loading', () => {
        // hide loading indicator
        if (activeTab && activeTab.id === tabId) {
            updateNavButtons();
        }
    });

    webview.addEventListener('page-title-updated', (e) => {
        title.textContent = e.title;
    });

    webview.addEventListener('did-navigate', (e) => {
        if (activeTab && activeTab.id === tabId) {
            urlInput.value = e.url;
        }
    });

    webview.addEventListener('new-window', (e) => {
        createNewTab(e.url);
    });
}

function setActiveTab(tab) {
    if (activeTab) {
        activeTab.element.classList.remove('active');
        activeTab.webview.classList.add('hidden');
    }

    activeTab = tab;
    activeTab.element.classList.add('active');
    activeTab.webview.classList.remove('hidden');
    urlInput.value = activeTab.webview.src;
    updateNavButtons();
}

function closeTab(tab) {
    if (tabs.length <= 1) {
        return; // Don't close the last tab
    }

    const index = tabs.findIndex(t => t.id === tab.id);
    if (index > -1) {
        tabs.splice(index, 1);
        tabsContainer.removeChild(tab.element);
        viewsContainer.removeChild(tab.webview);

        if (activeTab && activeTab.id === tab.id) {
            setActiveTab(tabs[0]);
        }
    }
}

function updateNavButtons() {
    if (activeTab) {
        backButton.disabled = !activeTab.webview.canGoBack();
        forwardButton.disabled = !activeTab.webview.canGoForward();
    }
}

// Navigation controls
backButton.addEventListener('click', () => {
    if (activeTab) {
        activeTab.webview.goBack();
    }
});

forwardButton.addEventListener('click', () => {
    if (activeTab) {
        activeTab.webview.goForward();
    }
});

refreshButton.addEventListener('click', () => {
    if (activeTab) {
        activeTab.webview.reload();
    }
});

homeButton.addEventListener('click', () => {
    if (activeTab) {
        activeTab.webview.loadURL(HOME_URL);
    }
});

urlInput.addEventListener('keydown', (e) => {
    if (e.key === 'Enter') {
        let url = urlInput.value;
        if (!url.startsWith('http://') && !url.startsWith('https://')) {
            if (!url.includes('.') || url.includes(' ')) {
                url = `https://www.google.com/search?q=${encodeURIComponent(url)}`;
            } else {
                url = 'https://' + url;
            }
        }
        activeTab.webview.loadURL(url);
    }
});

// New Tab Button
const newTabButton = document.createElement('button');
newTabButton.id = 'new-tab-button';
newTabButton.textContent = '+';
tabsContainer.appendChild(newTabButton);
newTabButton.addEventListener('click', () => createNewTab());

// Initial tab
createNewTab();
