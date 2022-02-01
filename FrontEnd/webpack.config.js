var path = require("path");

const NodePolyfillPlugin = require("node-polyfill-webpack-plugin")

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
        DynamicContent: "../../Modules/DynamicContent/Scripts/DynamicContent.js",
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
        Templates: "../../Modules/Templates/Scripts/Templates.js",
        Admin: "../../Modules/Admin/Scripts/Admin.js"
    },
    output: {
        path: path.join(__dirname, "wwwroot/scripts"),
        filename: "[name].min.js",
        chunkFilename: "[name].min.js",
        publicPath: "/scripts/"
    },
    plugins: [
        new NodePolyfillPlugin()
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
                use: [
                    {
                        loader: "file-loader",
                        options: {
                            name: "[name].[ext]",
                            outputPath: "fonts/"
                        }
                    }
                ]
            }
        ]
    }
};