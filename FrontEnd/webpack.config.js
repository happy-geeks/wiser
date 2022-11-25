var path = require("path");

const NodePolyfillPlugin = require("node-polyfill-webpack-plugin");
const {WebpackManifestPlugin} = require("webpack-manifest-plugin");

module.exports = {
    context: path.join(__dirname, "Core/Scripts"),
    entry: {
        main: "./main.js",
        Utils: "../../Modules/Base/Scripts/Utils.js",
        Processing: "../../Modules/Base/Scripts/Processing.js",
        DynamicItems: "../../Modules/DynamicItems/Scripts/DynamicItems.js",
        Fields: "../../Modules/DynamicItems/Scripts/Fields.js",
        Dialogs: "../../Modules/DynamicItems/Scripts/Dialogs.js",
        Windows: "../../Modules/DynamicItems/Scripts/Windows.js",
        Grids: "../../Modules/DynamicItems/Scripts/Grids.js",
        DragAndDrop: "../../Modules/DynamicItems/Scripts/DragAndDrop.js",
        Search: "../../Modules/Search/Scripts/Search.js",
        Import: "../../Modules/ImportExport/Scripts/Import.js",
        Export: "../../Modules/ImportExport/Scripts/Export.js",
        ImportExport: "../../Modules/ImportExport/Scripts/ImportExport.js",
        RemoveItems: "../../Modules/ImportExport/Scripts/RemoveItems.js",
        RemoveConnections: "../../Modules/ImportExport/Scripts/RemoveConnections.js",
        TaskAlerts: "../../Modules/TaskAlerts/Scripts/TaskAlerts.js",
        TaskHistory: "../../Modules/TaskAlerts/Scripts/TaskHistory.js",
        DataSelector: "../../Modules/DataSelector/Scripts/DataSelector.js",
        DataSelectorDataLoad: "../../Modules/DataSelector/Scripts/DataLoad.js",
        DataSelectorConnection: "../../Modules/DataSelector/Scripts/Connection.js",
        ContentBuilder: "../../Modules/ContentBuilder/Scripts/main.js",
        ContentBox: "../../Modules/ContentBox/Scripts/main.js",
        Preview: "../../Modules/Templates/Scripts/Preview.js",
        Templates: "../../Modules/Templates/Scripts/Templates.js",
        DynamicContent: "../../Modules/Templates/Scripts/DynamicContent.js",
        Admin: "../../Modules/Admin/Scripts/Admin.js",
        Dashboard: "../../Modules/Dashboard/Scripts/Dashboard.js",
        Base: "../../Modules/Base/Scripts/Base.js",
        VersionControl: "../../Modules/VersionControl/Scripts/VersionControl.js",
        CommunicationIndex: "../../Modules/Communication/Scripts/Index.js",
        CommunicationSettings: "../../Modules/Communication/Scripts/Settings.js"
    },
    output: {
        path: path.join(__dirname, "wwwroot/scripts"),
        filename: "[name].[contenthash].min.js",
        chunkFilename: "[name].[contenthash].min.js",
        publicPath: "/scripts/"
    },
    plugins: [
        new NodePolyfillPlugin(),
        // Add JSON manifest for loading files in .NET with a dynamic hash in the name, so that users don't need to clear their browser cache after every Wiser update.
        new WebpackManifestPlugin({}),
    ],
    resolve: {
        alias: {
            vue$: "vue/dist/vue.esm-bundler.js",
            kendo: "@progress/kendo-ui/js/"
        }
    },
    module: {
        rules: [
            {
                test: /\.[s]?[ac]ss$/i,
                use: ["style-loader", "css-loader", "sass-loader"]
            },
            {
                test: /\.(woff(2)?|ttf|eot|svg|png|jpg)(\?v=\d+\.\d+\.\d+)?$/,
                type: "asset/resource"
            }
        ]
    }
};