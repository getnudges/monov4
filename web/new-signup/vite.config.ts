import path from "node:path";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";
import relay from "vite-plugin-relay";
import commonjs from "vite-plugin-commonjs";
import fs from "node:fs";
import { i18nextVitePlugin } from "@i18next-selector/vite-plugin";

export default defineConfig(({ command }) => ({
  plugins: [
    react({
      babel: {
        plugins: ["relay"],
      },
    }),
    relay,
    commonjs(),
    i18nextVitePlugin({
      sourceDir: path.join(path.resolve(), "src", "locales"),
    }),
  ],
  build: {
    commonjsOptions: {
      transformMixedEsModules: true,
    },
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  server: {
    port: 5050,
    proxy: {
      "^/auth/.*": {
        target: "https://localhost:5555",
        rewrite(path) {
          return path.replace(/^\/auth/, "");
        },
        changeOrigin: true,
        secure: false, // Disable cert verification
        headers: {
          "X-Forwarded-Host": "localhost",
          "X-Forwarded-Port": "5050",
        },
      },
      "/graphql": {
        target: "https://localhost:5443",
        ws: true,
        rewriteWsOrigin: true,
        secure: false, // Disable cert verification
      },
    },
    https:
      command === "serve"
        ? {
            key: fs.readFileSync("./aspnetapp.key"),
            cert: fs.readFileSync("./aspnetapp.crt"),
          }
        : undefined,
  },
  define: {
    "process.env": {},
    global: "window",
  },
}));
