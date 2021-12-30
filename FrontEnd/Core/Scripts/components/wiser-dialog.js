import { defineAsyncComponent } from "vue";

export default {
    name: "wiser-dialog",
    props: {
        title: { type: String, default: "" }, 
        visible: { type: Boolean, default: false }, 
        modal: { type: Boolean, default: false }, 
        width: { type: String, default: "500px" },
        actions: { type: Array, default: () => [{ text: "Ok", primary: true }] }
    },
    data() {
        return {
            mutableVisible: this.visible
        };
    },
    computed: {
    },
    components: {
        "WiserDialogAction": defineAsyncComponent(() => import(/* webpackChunkName: "wiser-dialog-action" */"./wiser-dialog-action"))
    },
    methods: {
        open() {
            this.mutableVisible = true;
        },

        close() {
            this.mutableVisible = false;
        }
    },
    template: `<div v-if="mutableVisible" class="w-dialog-container">
    <div v-if="modal" class="w-overlay"></div>
    <div class="w-dialog" :style="{ width: width }">
        <h1>{{ title }}</h1>
        <slot></slot>
        <wiser-dialog-action v-for="dialogAction in actions" :key="dialogAction.text" v-bind="dialogAction" :closeDialog="close"></wiser-dialog-action>
    </div>
</div>
`
};