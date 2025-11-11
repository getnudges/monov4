import path from "path";
import react from "@vitejs/plugin-react";
import { defineConfig } from "vite";
import relay from "vite-plugin-relay";
import commonjs from "vite-plugin-commonjs";

export default defineConfig({
  plugins: [
    react({
      babel: {
        plugins: ["relay"],
      },
    }),
    relay,
    commonjs(),
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
    port: 6060,
    proxy: {
      "^/auth/.*": {
        target: "https://localhost:5555",
        rewrite(path) {
          return path.replace(/^\/auth/, "");
        },
        changeOrigin: true,
      },
      "/graphql": {
        target: "https://localhost:5900",
        ws: true,
        rewriteWsOrigin: true,
      },
    },
  },
  define: {
    "process.env": {},
    global: "window",
  },
});
