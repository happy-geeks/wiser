{
    "plugins": [
        [
            "transform-runtime",
            {
                "helpers": false,
                "polyfill": true,
                "regenerator": true,
                "moduleName": "babel-runtime"
            }
        ],
        ["babel-polyfill"]
    ],
        "presets":
    ["@babel/preset-env"]
}