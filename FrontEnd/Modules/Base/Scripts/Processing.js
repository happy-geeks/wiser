export class Processing {
    static currentProcesses = {};
    static onIdleFns = [];

    static addProcess(processId, isBlocking = true) {
        if (currentProcesses[processId]) {
            console.warn("Cannot add process, id already exists");
            return;
        }
        
        if (!busy()) {
            // todo: send busy event
            //core.debug.log("Event: processing.Busy");
            document.dispatchEvent(new CustomEvent("processing.Busy"));
        }
        currentProcesses[processId] = new JProcess(processId, isBlocking);
    }

    static removeProcess(processId) {
        if (!currentProcesses[processId]) {
            console.warn("Cannot remove process, id does not exist");
            return;
        }

        delete currentProcesses[processId];

        if (Object.keys(currentProcesses).length < 1) {
            //core.debug.log("Event: processing.Idle");
            document.dispatchEvent(new CustomEvent("processing.Idle"));
            runOnIdleFns();
        }
    }

    static busy(showActiveProcesses = false) {
        if (Object.keys(currentProcesses).length > 0) {
            if (showActiveProcesses) showProcesses();
            return true;
        } else {
            return false;
        }
    }

    static showProcesses() {
        console.log("Active processes: " + Object.keys(this.currentProcesses).length + ":", currentProcesses);
    }

    static clearProcesses() {
        currentProcesses = {};
    }

    static onDone(fn) {
        onIdleFns.push(fn);

        if (!busy()) {
            runOnIdleFns();
        }
    }

    static runOnIdleFns() {
        for (let i = 0; i < this.onIdleFns.length; i++) {
            onIdleFns[i]();
        }

        onIdleFns = [];
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

window.processing = Processing;