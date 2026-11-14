"use strict";

window.pokohex = {
    dirty: false,
    setDirty(value) {
        this.dirty = value;
    },
    getPaths(id) {
        return window.pokohexDrop.getPaths(document.getElementById(id));
    },
    mapPoint(element, clientX, clientY) {
        const point = element.createSVGPoint();
        point.x = clientX;
        point.y = clientY;
        const local = point.matrixTransform(element.getScreenCTM().inverse());
        return [Math.floor(local.x / 16), Math.floor(local.y / 16)];
    },
    async download(name, stream, type = "application/zip") {
        const data = await stream.arrayBuffer();
        const url = URL.createObjectURL(new Blob([data], { type }));
        const link = document.createElement("a");
        link.href = url;
        link.download = name;
        document.body.appendChild(link);
        link.click();
        link.remove();
        setTimeout(() => URL.revokeObjectURL(url), 60000);
    }
};

window.addEventListener("beforeunload", event => {
    if (window.pokohex.dirty) {
        event.preventDefault();
        event.returnValue = "";
    }
});
