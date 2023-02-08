const callBySelectorMap = new Map();

window.callBySelector = (selector, callbackString, args) => {
    let element = callBySelectorMap.get(selector);
    if (!element) {
        element = document.querySelector(selector);
        if (element) {
            callBySelectorMap.set(selector, element);
        } else {
            console.log('couldnt find element by selector', selector);
            return;
        }
    }
    const callback = new Function('element', 'args', callbackString);
    if (typeof(callback) == 'function') {
        return callback(element, args);
    }
};

const textArea = document.querySelector('.script'); 
if (!textArea) {
    console.error('could not find script editor');
}

window.scriptTextArea = textArea;

const scriptView = document.querySelector('.script-view');

window.setScriptViewInnerHTML = (text) => {
    if (scriptView) {
        scriptView.innerHTML = text;
    }
}

const log = document.querySelector('.log');

window.addLogInnerHTML = (text) => {
    if (log) {
        log.innerHTML += (log.innerHTML.length === 0 ? '' : '\n') + text;
        log.scrollTop = log.scrollHeight;
    }
}

const scriptContainer = document.querySelector('.script-container');

window.resizeView = function (setViewContent = true) {
    const scrollTop = scriptContainer.scrollTop;

    textArea.style.height = '';
    textArea.style.height = `${textArea.scrollHeight}px`;

    if (scriptView) {
        const rect = textArea.getBoundingClientRect();
        //scriptView.style.left = rect.x;
        //scriptView.style.top = rect.y;
        scriptView.style.width = `${rect.width}px`;
        scriptView.style.height = `${rect.height}px`;

        if (setViewContent) {
            scriptView.textContent = textArea.value;
        }
    }

    scriptContainer.scrollTop = scrollTop;
}

window.updateScript = function () {
    console.debug('calling update script');
    window.callDotNetReferenceMethod('index', 'UpdateScript', textArea.value);
}

let scriptCheckTimeout = setTimeout(updateScript, 500);

setTimeout(() => {
    resizeView();
    updateScript();
}, 1);

window.addEventListener('resize', resizeView.bind(this, false));

textArea.addEventListener('input', () => {
    clearTimeout(scriptCheckTimeout);
    scriptCheckTimeout = setTimeout(updateScript, 100);

    resizeView();
});