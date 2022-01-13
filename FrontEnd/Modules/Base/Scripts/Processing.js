export class Processing {
    constructor() {
        this.currentProcesses = {};
        this.onIdleFns = [];
    }

    addProcess(processId, isBlocking = true) {
        if (this.currentProcesses[processId]) {
            console.warn("Cannot add process, id already exists");
            return;
        }
        
        if (!this.busy()) {
            document.dispatchEvent(new CustomEvent("processing.Busy"));
        }
        this.currentProcesses[processId] = new JProcess(processId, isBlocking);
    }

    removeProcess(processId) {
        if (!this.currentProcesses[processId]) {
            console.warn("Cannot remove process, id does not exist");
            return;
        }

        delete this.currentProcesses[processId];

        if (Object.keys(this.currentProcesses).length < 1) {
            document.dispatchEvent(new CustomEvent("processing.Idle"));
            this.runOnIdleFns();
        }
    }

    busy(showActiveProcesses = false) {
        if (Object.keys(this.currentProcesses).length > 0) {
            if (showActiveProcesses) this.showProcesses();
            return true;
        } else {
            return false;
        }
    }

    showProcesses() {
        console.log("Active processes: " + Object.keys(this.currentProcesses).length + ":", currentProcesses);
    }

    clearProcesses() {
        this.currentProcesses = {};
    }

    onDone(fn) {
        this.onIdleFns.push(fn);

        if (!this.busy()) {
            this.runOnIdleFns();
        }
    }

    runOnIdleFns() {
        for (let i = 0; i < this.onIdleFns.length; i++) {
            this.onIdleFns[i]();
        }

        this.onIdleFns = [];
    }
}

class JProcess {
    constructor(processId, isBlocking) {
        this.processId = processId;
        this.isBlocking = isBlocking;
        this.created = Date.now();
        this.createdDateTime = new Date().toLocaleString();
    }
}

if (window.processing == undefined) {
    window.processing = new Processing();
}