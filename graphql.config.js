module.exports = {
  projects: {
    adminUi: {
      schema: [
        "web/new-admin/schema.graphql",
        "./relay-compiler-directives-v10.0.1.graphql",
      ],
      documents: ["web/new-admin/src/**/*.{graphql,js,ts,jsx,tsx}"],
    },
    signupSite: {
      schema: [
        "web/new-signup/schema.graphql",
        "./relay-compiler-directives-v10.0.1.graphql",
      ],
      documents: ["web/new-signup/src/**/*.{graphql,js,ts,jsx,tsx}"],
    },
  },
};
