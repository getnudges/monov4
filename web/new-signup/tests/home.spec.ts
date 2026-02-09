import { expect, test } from "@playwright/test";

test("home page displays", async ({ page }) => {
  await page.goto("/");
  await expect(page.getByRole("heading", { name: "Home" })).toBeVisible();
});
