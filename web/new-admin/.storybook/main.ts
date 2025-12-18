import type { StorybookConfig } from "@storybook/react-vite";
import { mergeConfig } from "vite";

const config: StorybookConfig = {
  stories: ["../src/**/*.mdx", "../src/**/*.stories.@(js|jsx|mjs|ts|tsx)"],
  addons: [
    "@storybook/addon-links",
    "@storybook/addon-essentials",
    "@storybook/addon-interactions",
  ],
  framework: {
    name: "@storybook/react-vite",
    options: {},
  },
  docs: {},
  viteFinal: async (config) => {
    return mergeConfig(config, {
      // Ensure Storybook uses the same aliases as the app
      resolve: {
        alias: {
          "@": "/src",
        },
      },
      // Handle Relay babel plugin
      define: {
        "process.env": {},
        global: "window",
      },
    });
  },
};

export default config;
