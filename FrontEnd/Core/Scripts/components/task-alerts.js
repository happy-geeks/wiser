export default {
    name: "task-alerts",
    props: ["subDomain"],
    data() {
        return {
            unreadMessage: false,
            taskCount: null,
            isOpened: false
        };
    },
    async created() {
        window.addEventListener("message", this.onReceiveMessage.bind(this));


    },
    computed: {
    },
    components: {
    },
    methods: {
        addAlert() {
            if (this.unreadMessage) {
                return;
            }

            this.unreadMessage = true;

            //play audio, this will only play if the user had interaction with the page. This is a browser setting.
            const audio = new Audio("/sounds/message.mp3");
            audio.play();
        },

        removeAlert() {
            this.unreadMessage = false;
        },

        updateTaskCount(count) {
            if (count === 0) {
                this.removeAlert();
            }

            this.taskCount = count;
        },

        onReceiveMessage(event) {
            const myOrigin = window.location.origin || window.location.protocol + "//" + window.location.host;
            if (event.origin !== myOrigin) {
                return;
            }

            if (!event.data.hasOwnProperty("action")) {
                return;
            }

            switch (event.data.action) {
            case "NewMessageReceived":
                this.addAlert();
                break;

            case "UpdateTaskCount":
                this.updateTaskCount(event.data.taskCount);
                break;
            }
        },

        toggleOpen() {
            this.isOpened = !this.isOpened;
            if (this.isOpened) {
                this.removeAlert();
            }
            const taskContainer = document.getElementById("task");
            const taskFrameElement = document.getElementById("taskFrame");
            const newContainerWidth = window.innerWidth - taskContainer.getBoundingClientRect().x;

            taskFrameElement.setAttribute("style", `width: ${newContainerWidth}px`);
        }
    },
    template: `<div :class="{ 'ico-item': true, alert: unreadMessage, open: isOpened }" id="task" :data-alert="taskCount" @click="toggleOpen()">
    <ins class="icon-clipboard"></ins>
    <div class="taskAlert"><ins class="icon-bell"></ins><span>Open recente meldingen</span></div>
    <iframe src="/Modules/TaskAlerts" id="taskFrame"></iframe>
</div>`
};