# Frontend Development Conventions

## Package Manager Rule
- **ALWAYS use `pnpm`** instead of `npm` or `yarn` for all frontend package management, installing dependencies, and running scripts in the `frontend/` directory.
- Examples:
  - Run typecheck: `pnpm check`
  - Run build: `pnpm build`
  - Run dev server: `pnpm dev`
  - Install packages: `pnpm add <package>` or `pnpm add -D <package>`
  - Install all dependencies: `pnpm install`
