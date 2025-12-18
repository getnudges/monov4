# Storybook Deployment

This Storybook is automatically deployed to GitHub Pages alongside the mkdocs documentation.

## Deployment Setup

### GitHub Actions Workflow

The workflow at `.github/workflows/docs.deploy.yml` handles deployment:

1. **Triggers on:**
   - Push to `main` branch
   - Changes to `mkdocs/**` OR `web/new-admin/**`
   - Manual workflow dispatch

2. **Build Process:**
   - Sets up Node.js 18
   - Installs npm dependencies (with caching)
   - Builds Storybook → `mkdocs/site/admin-storybook/`
   - Builds mkdocs (includes Storybook subdirectory)
   - Deploys entire `mkdocs/site/` to GitHub Pages

### Hosted URL

Once deployed, Storybook will be available at:

**https://docs.nudges.dev/admin-storybook/**

Or if using GitHub's default Pages URL:

**https://[username].github.io/[repo]/admin-storybook/**

## Local Testing

Before pushing, test the production build locally:

```bash
# Build Storybook
cd web/new-admin
npm run build-storybook

# Verify output
ls -la ../../mkdocs/site/admin-storybook/

# Serve locally (if you have a static server)
npx serve ../../mkdocs/site/admin-storybook
```

Or serve the entire mkdocs site including Storybook:

```bash
# From mkdocs directory
cd mkdocs
mkdocs serve
# Storybook available at http://localhost:8000/admin-storybook/
```

## Manual Deployment

If needed, you can trigger deployment manually:

1. Go to GitHub Actions tab
2. Select "Deploy Documentation to GitHub Pages"
3. Click "Run workflow"
4. Select branch `main`
5. Click "Run workflow"

## Workflow Details

### Node.js Caching

The workflow uses npm cache to speed up builds:

```yaml
cache: 'npm'
cache-dependency-path: web/new-admin/package-lock.json
```

This caches `node_modules` based on `package-lock.json` hash.

### Build Order

1. **Storybook first** - Builds to `mkdocs/site/admin-storybook/`
2. **mkdocs second** - Builds main docs, preserving Storybook subdirectory
3. **Upload artifact** - Entire `mkdocs/site/` directory
4. **Deploy** - GitHub Pages deployment

## Troubleshooting

### Storybook Not Updating

1. Check GitHub Actions run succeeded
2. Verify Storybook build step completed
3. Check for build errors in Actions logs
4. Clear browser cache (Storybook assets are cached)

### Build Failures

Common issues:

**npm install fails:**
- Check `package-lock.json` is committed
- Verify Node 18+ compatibility

**Storybook build fails:**
- Check for TypeScript errors: `npm run relay`
- Verify all imports resolve
- Check story syntax

**mkdocs overwriting Storybook:**
- Ensure Storybook builds BEFORE mkdocs
- mkdocs should preserve existing files in `site/`

## Updating Storybook

When you update stories or components:

1. Make changes to `web/new-admin/src/stories/**`
2. Test locally: `npm run storybook`
3. Commit and push to `main`
4. GitHub Actions automatically rebuilds and deploys
5. Storybook updates at `docs.nudges.dev/admin-storybook/`

## Adding to mkdocs Navigation (Optional)

To link Storybook from mkdocs navigation, edit `mkdocs/mkdocs.yml`:

```yaml
nav:
  - Home: index.md
  - Getting Started: getting-started.md
  # ... other items
  - Web Applications:
    - Admin UI: web/admin.md  # Create this page
  - Component Library: /admin-storybook/  # External link to Storybook
```

Or add a card/link in your index page:

```markdown
## Resources

- [Component Library (Storybook)](admin-storybook/) - Interactive component showcase
```

## Performance

**Build time:**
- Storybook: ~10-15 seconds
- mkdocs: ~2-5 seconds
- Total: ~15-20 seconds

**Caching benefits:**
- First build: ~30 seconds (npm install)
- Cached builds: ~15 seconds (skip install)

## Future Enhancements

- [ ] Add visual regression testing (Chromatic)
- [ ] Separate workflow for Storybook-only updates
- [ ] Version Storybook deployments (keep old versions)
- [ ] Add Storybook to PR previews
