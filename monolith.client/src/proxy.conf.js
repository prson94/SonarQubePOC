const PROXY_CONFIG = [
  {
    context: [
      "/api",
    ],
    target: "https://localhost:7048",
    "changeOrigin": true,
    "secure": false,
    "logLevel": "debug"
  }
];

module.exports = PROXY_CONFIG;
