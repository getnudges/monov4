import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
  testDir: "./tests",
  timeout: 30_000,
  retries: process.env.CI ? 2 : 0,
  reporter: process.env.CI
    ? [["html", { open: "never" }], ["list"]]
    : "list",
  use: {
    baseURL: "https://localhost:5050",
    ignoreHTTPSErrors: true,
    trace: "on-first-retry",
  },
  webServer: {
    command: "npm run dev -- --host",
    url: "https://localhost:5050",
    reuseExistingServer: !process.env.CI,
    timeout: 120_000,
  },
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },
  ],
});
