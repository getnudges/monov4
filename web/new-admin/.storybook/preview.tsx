import type { Preview } from "@storybook/react";
import React from "react";
import { Router } from "wouter";
import { ThemeProvider } from "../src/components/theme-provider";
import "../src/index.css";

const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i,
      },
    },
    backgrounds: {
      default: "dark",
      values: [
        {
          name: "dark",
          value: "#0a0a0a",
        },
        {
          name: "light",
          value: "#ffffff",
        },
      ],
    },
  },
  decorators: [
    (Story) => (
      <ThemeProvider defaultTheme="dark" storageKey="storybook-ui-theme">
        <Router>
          <div className="p-8">
            <Story />
          </div>
        </Router>
      </ThemeProvider>
    ),
  ],
};

export default preview;
