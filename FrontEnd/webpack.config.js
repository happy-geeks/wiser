var path = require("path");

const NodePolyfillPlugin = require("node-polyfill-webpack-plugin");
const HtmlWebpackPlugin = require('html-webpack-plugin');
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
        WtsConfiguration: "../../Modules/Templates/Scripts/WtsConfiguration.js",
        Templates: "../../Modules/Templates/Scripts/Templates.js",
        DynamicContent: "../../Modules/Templates/Scripts/DynamicContent.js",
        Admin: "../../Modules/Admin/Scripts/Admin.js",
        Dashboard: "../../Modules/Dashboard/Scripts/Dashboard.js",
        Base: "../../Modules/Base/Scripts/Base.js",
        VersionControl: "../../Modules/VersionControl/Scripts/VersionControl.js",
        CommunicationIndex: "../../Modules/Communication/Scripts/Index.js",
        CommunicationSettings: "../../Modules/Communication/Scripts/Settings.js",
        FileManager: "../../Modules/FileManager/Scripts/FileManager.js",
        Configuration: "../../Modules/Configuration/Scripts/Configuration.js"
    },
    output: {
        path: path.join(__dirname, "wwwroot/scripts"),
        filename: "[name].[contenthash].min.js",
        chunkFilename: "[name].[contenthash].min.js",
        publicPath: "/scripts/",
        clean: true
    },
    plugins: [
        new NodePolyfillPlugin(),
        // Add JSON manifest for loading files in .NET with a dynamic hash in the name, so that users don't need to clear their browser cache after every Wiser update.
        new WebpackManifestPlugin({}),
        new HtmlWebpackPlugin()
    ],
    resolve: {
        alias: {
            vue$: "vue/dist/vue.esm-bundler.js",
            kendo: "@progress/kendo-ui/js/"
        }
    },
    optimization: {
        runtimeChunk: "single",
        splitChunks: {
            cacheGroups: {
                defaultVendors: {
                    chunks: "all",
                    test: /[\\/]node_modules[\\/]/,
                    name(module, chunks, cacheGroupKey) {
                        // Kendo and InnovaStudio have multiple modules, we want to create a chunk for each of those.
                        const vendorExcludes = ["@progress", "@innovastudio"];
                        const foundVendorName = vendorExcludes.find(x => module.resource.includes(x));

                        if (!foundVendorName) {
                            // If it's not one of the specified vendors, bundle them all in the same file.
                            return "vendors";
                        }

                        // Here we find the name of the module in the full resource path.
                        // First we get the index of where the vendor name starts.
                        const vendorIndex = module.resource.indexOf(foundVendorName);
                        // Then we get the part of the path that comes after the vendor name (and +1 for the slash).
                        let name = module.resource.substring(vendorIndex + foundVendorName.length + 1);

                        // Then we find the index of the next slash.
                        let slashIndex = name.indexOf("/");
                        if (slashIndex < 0) {
                            slashIndex = name.indexOf("\\");
                        }

                        // And finally we can take the name before the next slash, which will be the name of the module.
                        if (slashIndex > -1) {
                            name = name.substring(0, slashIndex);
                        }

                        return name;
                    }
                }
            },
        },
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