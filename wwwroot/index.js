const log = document.querySelector('.log');

window.addLogInnerHTML = (text) => {
    if (log) {
        log.innerHTML += (log.innerHTML.length === 0 ? '' : '\n') + text;
        log.scrollTop = log.scrollHeight;
    }
}


async function parse(text) {
    
    const result = await window.callDotNetReferenceMethod('index', 'Parse', text);

    return result;

}

let scriptCheckTimeout;

const scriptEditor = document.querySelector('.script');

async function updateScript() {
    console.log('updating script');

    // send script textcontent to parser to parse
    const result = await parse(scriptEditor.textContent);

    const selection = document.getSelection();
    
    const selectionLength = selection.toString().length;

    selection.modify('extend', 'backward', 'paragraphboundary');

    


    console.log(selection);
    const range = selection.getRangeAt(0);
    selection.removeAllRanges();

    scriptEditor.innerHTML = result.innerHTML;

    selection.addRange(range);
    console.log('new', selection);

}

scriptEditor?.addEventListener('input', () => {

    clearTimeout(scriptCheckTimeout);
    scriptCheckTimeout = setTimeout(updateScript, 100);

});