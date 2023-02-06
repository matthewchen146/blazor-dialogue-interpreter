function createDragger(parent, draggerSide) {

    const endSide = draggerSide === 'left' ? 'right' : (draggerSide === 'top' ? 'bottom' : 'unknown');
    const pointerAxis = draggerSide === 'left' ? 'clientX' : (draggerSide === 'top' ? 'clientY' : 'unknown');
    const dimension = draggerSide === 'left' ? 'width' : (draggerSide === 'top' ? 'height' : 'unknown');
    const minDimension = dimension === 'width' ? 'minWidth' : (dimension === 'height' ? 'minHeight' : 'unknown'); 

    const children = Array.from(parent.children).filter((child) => !child.classList.contains('editor-dragger'));

    let widthStyle = '';
    for (let i = 0; i < children.length - 1; i++)
    {
        widthStyle += 'auto ';
    }

    widthStyle += '1fr';

    parent.style.setProperty('--widths', widthStyle);

    children.forEach((container, index) => {

        if (index == 0) {
            


        } else {

            const dragger = document.createElement('div');
            dragger.classList.toggle('editor-dragger', true);
            dragger.draggable = false;

            container.appendChild(dragger);

            let dragging = false;
            
            let parentEnd = 0;

            let dragOffset = 0;

            let prevStart = 0;

            let currentEnd = 0;

            let currentComputedStyle;
            let prevComputedStyle;

            dragger.addEventListener('mousedown', (e) => {

                dragging = true;
                
                let draggerStart = dragger.getBoundingClientRect()[draggerSide];
                dragOffset = e[pointerAxis] - draggerStart;

                parentEnd = parent.getBoundingClientRect()[endSide];
                prevStart = children[index - 1].getBoundingClientRect()[draggerSide];
                currentEnd = container.getBoundingClientRect()[endSide];

                currentComputedStyle = window.getComputedStyle(container);
                prevComputedStyle = window.getComputedStyle(children[index - 1]);

            });

            window.addEventListener('mousemove', (e) => {

                if (dragging) {

                    let dragPos = Math.max(
                        Math.min(
                            e[pointerAxis] - dragOffset, 
                            currentEnd - parseFloat(currentComputedStyle[minDimension])
                        ), 
                        prevStart + parseFloat(prevComputedStyle[minDimension])
                    );

                    const doubleWidth = currentEnd - prevStart;

                    const newPrevWidth = dragPos - prevStart;

                    children[index - 1].style[dimension] = `${newPrevWidth}px`;

                    container.style[dimension] = index === children.length - 1 ? '100%' : `${doubleWidth - newPrevWidth}px`;
        
                }

            })

            window.addEventListener('mouseup', () => {

                if (dragging) {
                    dragging = false;

                    
                }

            });

        }
        
    });

}

// set up editor window draggers


const editorContainers = document.querySelectorAll('.editor *');

editorContainers.forEach((element) => {

    // const computedStyle = window.getComputedStyle(element);
    // element.style.minWidth = computedStyle.minWidth;
    // element.style.minHeight = computedStyle.minHeight;

});

const editorHBoxes = document.querySelectorAll('.editor-hbox');

editorHBoxes.forEach((hbox) => {

    createDragger(hbox, 'left');

});

const editorVBoxes = document.querySelectorAll('.editor-vbox');

editorVBoxes.forEach((vbox) => {

    createDragger(vbox, 'top');

});